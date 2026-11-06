namespace Panacea.Sim;

/// <summary>The body model (systems/body.ts).</summary>
public static class BodySystem
{
    private readonly record struct MonthOutcome(bool Died, bool Left, bool Complaint);

    public static readonly Phase BodyPhase = new("body", ctx =>
    {
        var state = ctx.State;
        var content = ctx.Content;
        var rng = ctx.Rng;
        var scratch = ctx.Scratch;
        var analyses = new Dictionary<string, FormulaAnalysis>(StringComparer.Ordinal);
        FormulaAnalysis AnalysisOf(Bought b)
        {
            if (!analyses.TryGetValue(b.LotId, out var a))
            {
                a = Formulas.Analyze(b.Components, b.ProcessId, content);
                analyses[b.LotId] = a;
            }
            return a;
        }

        double wordOfMouth = 0;
        int complaints = 0;
        var departed = new HashSet<string>(StringComparer.Ordinal);

        foreach (var c in state.Customers)
        {
            var outcome = MonthForCustomer(state, content, rng, c,
                scratch.Purchases.TryGetValue(c.Id, out var p) ? p : [], AnalysisOf);
            if (outcome.Complaint) complaints += 1;
            if (outcome.Died || outcome.Left) departed.Add(c.Id);
            if (!outcome.Died) wordOfMouth += c.Satisfaction;
        }

        // Anonymous walk-ins: one bottle, no tolerance. A strong enough bottle still kills.
        foreach (var w in scratch.WalkIns)
        {
            var a = AnalysisOf(w.Bought);
            if (!content.ById.Segments.TryGetValue(w.SegmentId, out var seg)) continue;
            string consumer = content.ById.Ailments.TryGetValue(w.AilmentId, out var ail) ? ail.Consumer : Consumers.Self;
            var dose = Pharmacology.DoseContextFor(consumer, seg.Id, w.Bought.Potency, content);
            double danger = Pharmacology.AcuteDanger(a, Pharmacology.ZeroTolerance(), dose, content);
            if (rng.Chance(Pharmacology.OverdoseChance(danger, seg.BaseHealth)))
            {
                Scrutiny.RecordDeath(state, content, rng, new DeathInput
                {
                    CustomerId = null,
                    Name = "",
                    Segment = seg.Id,
                    Consumer = consumer,
                    Anonymous = true,
                    Ailment = w.AilmentId,
                    Cause = DeathCauses.Overdose,
                    ProductImplicated = true,
                    FormulaId = w.Bought.FormulaId,
                    BadBatch = w.Bought.BadBatch,
                    Detectability = a.Properties.Detectability,
                });
            }
        }

        state.Customers = state.Customers.Where(c => !departed.Contains(c.Id)).ToList();
        state.Stats.Complaints += complaints;
        state.Month.Complaints += complaints;
        if (complaints > 0)
        {
            Ledger.Adjust(state, Metrics.Heat, complaints * content.Config.Scrutiny.HeatPerComplaint, Causes.Complaints);
            Ledger.Adjust(state, Metrics.Reputation, -0.15 * complaints, Causes.Complaints);
        }
        double wom = Math.Max(-2, Math.Min(2, 0.08 * wordOfMouth));
        if (wom != 0) Ledger.Adjust(state, Metrics.Reputation, wom, Causes.WordOfMouth);
        double drift = (content.Config.ReputationBaseline - state.Player.Reputation) * content.Config.ReputationDrift;
        if (Math.Abs(drift) > 1e-9) Ledger.Adjust(state, Metrics.Reputation, drift, Causes.ReputationDrift);
    });

    private static MonthOutcome MonthForCustomer(GameState state, ContentPack content, Rng rng, Customer c,
        IReadOnlyList<Bought> bought, Func<Bought, FormulaAnalysis> analysisOf)
    {
        var cfg = content.Config.Body;
        var body = c.Body;
        content.ById.Segments.TryGetValue(c.Segment, out var seg);
        content.ById.Ailments.TryGetValue(c.Ailment, out var ailment);
        var habitIntake = Pharmacology.ZeroTolerance();
        double loadIntake = 0;
        double acuteHarm = 0;
        double reliefSum = 0;
        string? lastFormula = null;
        bool lastBadBatch = false;
        double? lastDetectability = null;

        MonthOutcome Die(string cause, bool productImplicated)
        {
            Scrutiny.RecordDeath(state, content, rng, new DeathInput
            {
                CustomerId = c.Id,
                Name = c.Name,
                Segment = c.Segment,
                Consumer = c.Consumer,
                Anonymous = false,
                Ailment = c.Ailment,
                Cause = cause,
                ProductImplicated = productImplicated,
                FormulaId = lastFormula ?? RecentFormula(state, c),
                BadBatch = lastBadBatch,
                Detectability = lastDetectability,
            });
            if (c.Consumer == Consumers.Infant)
            {
                state.Stats.CustomersLost += 1;
                state.Month.News.Add(new CustomerLeftNews(c.Id, c.Name, LeaveReasons.Bereaved));
            }
            return new MonthOutcome(c.Consumer == Consumers.Self, c.Consumer == Consumers.Infant, false);
        }

        // 1. Each bottle taken this month.
        foreach (var b in bought)
        {
            var a = analysisOf(b);
            lastFormula = b.FormulaId;
            lastDetectability = a.Properties.Detectability;
            lastBadBatch = lastBadBatch || b.BadBatch;
            var dose = Pharmacology.DoseContextFor(c.Consumer, c.Segment, b.Potency, content);
            reliefSum += Pharmacology.ReliefScore(a, body.Tolerance, c.Profile);
            double danger = Pharmacology.AcuteDanger(a, body.Tolerance, dose, content);
            if (rng.Chance(Pharmacology.OverdoseChance(danger, body.Health))) return Die(DeathCauses.Overdose, true);
            double harm = Math.Max(0, danger - 0.3 * Pharmacology.DangerThreshold(body.Health)) * cfg.AcuteHarm;
            body.Health -= harm;
            acuteHarm += harm;
            foreach (var cls in DrugClasses.All)
                habitIntake[cls] += a.ByClass[cls].Habit * b.Potency * dose.DoseScale;
            loadIntake += a.Properties.Accumulation * b.Potency * dose.DoseScale * dose.Vulnerability;
        }

        // 2. Tolerance and dependency.
        foreach (var cls in DrugClasses.All)
        {
            body.Tolerance[cls] = body.Tolerance[cls] * (1 - cfg.ToleranceDecay) + cfg.ToleranceGain * habitIntake[cls];
        }
        double habitTotal = 0;
        foreach (var cls in DrugClasses.All) habitTotal = habitTotal + habitIntake[cls];
        double dependencyDecay = cfg.DependencyDecay * (habitTotal > 0 ? 0.3 : 1);
        body.Dependency = Clamp01(
            body.Dependency +
            cfg.DependencyGain * habitTotal * (1 - body.Dependency) -
            dependencyDecay * body.Dependency);
        bool withdrawal = body.Dependency > cfg.WithdrawalThreshold && habitTotal < body.Dependency * 2;
        c.InWithdrawal = withdrawal;
        if (withdrawal) body.Health -= cfg.WithdrawalHarm;

        // 3. Accumulated toxins, the illness itself, and recovery.
        body.Load = body.Load * (1 - cfg.LoadDecay) + loadIntake;
        double chronicHarm = Math.Max(0, body.Load - cfg.LoadSafe) * cfg.LoadHarm;
        body.Health -= chronicHarm;
        double drift = ailment?.HealthDriftPerMonth ?? 0;
        body.Health += drift;
        if (acuteHarm == 0 && chronicHarm == 0 && !withdrawal && body.Health < body.BaseHealth)
        {
            body.Health += (body.BaseHealth - body.Health) * cfg.Recovery;
        }
        body.Health = Math.Min(100, body.Health);
        double productHarm = acuteHarm + chronicHarm + (withdrawal ? cfg.WithdrawalHarm : 0);

        // 4. Death from wasting, or from ordinary causes made likelier by poor health.
        if (body.Health <= 0)
        {
            string cause = chronicHarm > 0 && chronicHarm >= -drift
                ? DeathCauses.Poisoning
                : acuteHarm > 0
                    ? DeathCauses.Overdose
                    : DeathCauses.Disease;
            return Die(cause, productHarm > 0);
        }
        double weak = body.Health < 40 ? 1 + (40 - body.Health) / 10 : 1;
        double hazard =
            (seg?.NaturalHazard ?? 0.002) *
            (ailment?.MortalityMultiplier ?? 1) *
            (c.Consumer == Consumers.Infant ? 2 : 1) *
            weak;
        if (rng.Chance(hazard)) return Die(DeathCauses.Natural, body.Load > cfg.LoadSafe || productHarm > 0);

        // 5. Satisfaction.
        if (bought.Count > 0)
        {
            double felt = (reliefSum / bought.Count) * (1 + 0.5 * (bought.Count - 1));
            double relief = felt - (ailment?.Severity ?? 3);
            c.Satisfaction = 0.6 * c.Satisfaction + 0.4 * DMath.Tanh(relief / 3);
        }
        else
        {
            c.Satisfaction *= 0.8;
        }
        if (withdrawal) c.Satisfaction -= 0.4;
        c.Satisfaction = Math.Max(-1, Math.Min(1, c.Satisfaction));
        // A regular ruined by the habit has a family, and the family complains.
        bool complaint = withdrawal || (bought.Count > 0 && c.Satisfaction < -0.3) || Market.IsRuined(c, content);

        // 6. Does the customer keep coming?
        string? reason = null;
        if (c.AilmentEndsTurn is int ends && state.Meta.Turn >= ends && body.Dependency < 0.5)
        {
            reason = LeaveReasons.Recovered;
        }
        else if (body.Dependency < cfg.WithdrawalThreshold && c.Satisfaction < -0.4 && rng.Chance(cfg.LapseChance))
        {
            reason = LeaveReasons.Dissatisfied;
        }
        else if (body.Dependency < cfg.WithdrawalThreshold &&
                 (c.LastPurchaseTurn is int last ? state.Meta.Turn - last : state.Meta.Turn - c.JoinedTurn) >= 3)
        {
            reason = LeaveReasons.Drifted;
        }
        else if (WardSystem.FrightenedAway(state, c, content, rng))
        {
            // a regular who is not hooked hears what the ward says of the bottle, and goes elsewhere
            reason = LeaveReasons.Afraid;
        }
        if (reason is not null)
        {
            state.Stats.CustomersLost += 1;
            state.Month.News.Add(new CustomerLeftNews(c.Id, c.Name, reason));
            return new MonthOutcome(false, true, complaint);
        }
        return new MonthOutcome(false, false, complaint);
    }

    /// <summary>The formula a customer took recently, to attribute a death in a month without purchases.</summary>
    private static string? RecentFormula(GameState state, Customer c)
    {
        if (c.LastPurchaseTurn is not int last || state.Meta.Turn - last > 6) return null;
        return c.LastFormulaId;
    }

    private static double Clamp01(double x) => Math.Max(0, Math.Min(1, x));
}
