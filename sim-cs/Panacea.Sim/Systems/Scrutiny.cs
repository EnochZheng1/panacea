namespace Panacea.Sim;

public sealed class DeathInput
{
    public string? CustomerId { get; init; }
    public required string Name { get; init; }
    public required string Segment { get; init; }
    public required string Consumer { get; init; }
    public bool Anonymous { get; init; }
    public required string Cause { get; init; }
    public bool ProductImplicated { get; init; }
    public string? FormulaId { get; init; }
    public bool BadBatch { get; init; }
    /// <summary>Detectability of the bottle actually taken, if known.</summary>
    public double? Detectability { get; init; }
}

/// <summary>Scrutiny: heat, deaths and coroner's inquests (systems/scrutiny.ts).</summary>
public static class Scrutiny
{
    public const string InquestEventId = "inquest";

    /// <summary>Records a death in the human ledger, raises heat, and may convene an inquest.</summary>
    public static DeathRecord RecordDeath(GameState state, ContentPack content, Rng rng, DeathInput input)
    {
        var cfg = content.Config.Scrutiny;
        var formula = !string.IsNullOrEmpty(input.FormulaId)
            ? state.Lab.Formulas.Find(f => f.Id == input.FormulaId)
            : null;
        var record = new DeathRecord
        {
            Id = $"death_{state.Meta.NextId++}",
            Turn = state.Meta.Turn,
            Date = state.Meta.Date,
            CustomerId = input.CustomerId,
            Name = input.Name,
            Segment = input.Segment,
            Consumer = input.Consumer,
            Anonymous = input.Anonymous,
            Cause = input.Cause,
            ProductImplicated = input.ProductImplicated,
            FormulaId = input.FormulaId,
            BadBatch = input.BadBatch,
            Detectability = input.Detectability ??
                (formula is not null
                    ? Formulas.Analyze(formula.Components, formula.ProcessId, content).Properties.Detectability
                    : 0),
            Boldness = formula is not null ? Formulas.ClaimBoldness(formula.Claims, content) : 0,
            Inquest = InquestStates.None,
            Verdict = null,
        };
        state.Deaths.Add(record);
        state.Month.News.Add(new DeathNews(record.Id));

        double @base = input.Anonymous
            ? cfg.HeatPerAnonymousDeath
            : input.Consumer == Consumers.Infant
                ? cfg.HeatPerInfantDeath
                : cfg.HeatPerDeath;
        Ledger.Adjust(state, Metrics.Heat, input.ProductImplicated ? @base : @base / 2, Causes.Death, record.Id);
        if (!input.Anonymous) Ledger.Adjust(state, Metrics.Reputation, -1, Causes.Death, record.Id);

        if (!string.IsNullOrEmpty(input.FormulaId))
        {
            double p =
                cfg.InquestBaseChance +
                state.Scrutiny.Heat / 150 +
                (input.Consumer == Consumers.Infant ? 0.25 : 0) +
                (input.ProductImplicated ? 0.15 : 0);
            if (input.Anonymous) p *= 0.6;
            if (rng.Chance(Math.Min(0.95, p)))
            {
                record.Inquest = InquestStates.Pending;
                state.Events.Pending.Add(new PendingEvent
                {
                    InstanceId = $"event_{state.Meta.NextId++}",
                    EventId = InquestEventId,
                    DrawnTurn = state.Meta.Turn,
                    DeathId = record.Id,
                });
            }
        }
        return record;
    }

    /// <summary>
    /// What the coroner currently charges. Rises with every bribe and with the heat, and is asked
    /// in round sums: a coroner names his price to the nearest five dollars.
    /// </summary>
    public static long BribeCostCents(GameState state, ContentPack content)
    {
        long @base = content.Config.Scrutiny.BribeBaseCents;
        double raw = @base * (1 + 0.5 * state.Scrutiny.BribesPaid) * (1 + state.Scrutiny.Heat / 50);
        return Math.Max(500, (long)JsMath.Round(raw / 500) * 500);
    }

    /// <summary>
    /// Chance that a bribe offered now comes out: the coroner's clerk talks. Every fee already paid
    /// is one more that someone knows about, and a notorious name is worth more to a talker.
    /// </summary>
    public static double BribeExposureChance(GameState state, ContentPack content)
    {
        var cfg = content.Config.Scrutiny;
        return Math.Min(cfg.BribeExposureMax,
            state.Scrutiny.BribesPaid * cfg.BribeExposurePerBribe + state.Player.Notoriety * cfg.BribeExposurePerNotoriety);
    }

    /// <summary>Probability the jury blames your medicine. Approach: "testify" or "silent".</summary>
    public static double ImplicationChance(GameState state, DeathRecord record, string approach)
    {
        double heat = state.Scrutiny.Heat;
        bool testify = approach == "testify";
        if (!record.ProductImplicated)
        {
            return Math.Min(0.3, Math.Max(0, 0.05 + heat / 300 + state.Scrutiny.TimesImplicated * 0.01 - (testify ? 0.04 : 0)));
        }
        double p =
            0.25 +
            record.Detectability / 25 +
            heat / 200 -
            state.Player.Reputation / 400 +
            record.Boldness * 0.02 +
            (record.BadBatch ? 0.2 : 0) +
            // the coroner remembers the label: every verdict against it makes the next one likelier
            state.Scrutiny.TimesImplicated * 0.03 -
            (testify ? 0.15 : 0);
        return Math.Min(0.95, Math.Max(0.05, p));
    }

    /// <summary>Resolves an inquest. Returns an error message if the approach is not possible.</summary>
    public static string? ResolveInquest(GameState state, ContentPack content, Rng rng, string? deathId, string approach)
    {
        var record = state.Deaths.Find(d => d.Id == deathId);
        if (record is null || record.Inquest != InquestStates.Pending) return "no inquest is pending for that death";
        var cfg = content.Config.Scrutiny;
        string outcome;

        void Implicate(double heat, double reputation, double notoriety)
        {
            Ledger.Adjust(state, Metrics.Reputation, -reputation, Causes.Inquest, record.Id);
            Ledger.Adjust(state, Metrics.Heat, heat, Causes.Inquest, record.Id);
            Ledger.Adjust(state, Metrics.Notoriety, notoriety, Causes.Inquest, record.Id);
            state.Scrutiny.TimesImplicated += 1;
            state.Player.Flags["implicated_at_inquest"] = true;
            foreach (var c in state.Customers) c.Satisfaction = Math.Max(-1, c.Satisfaction - 0.3);
        }

        if (approach == "bribe")
        {
            long cost = BribeCostCents(state, content);
            if (cost > state.Player.CashCents) return "not enough cash for the coroner";
            double exposure = BribeExposureChance(state, content);
            Ledger.Adjust(state, Metrics.Cash, -cost, Causes.Bribe, record.Id);
            state.Scrutiny.BribesPaid += 1;
            if (rng.Chance(exposure))
            {
                // the money is gone, and the jury hears where it went: worse than any verdict
                Implicate(30, 15, 10);
                state.Month.News.Add(new BribeExposedNews(record.Id));
                outcome = InquestStates.Implicated;
            }
            else
            {
                Ledger.Adjust(state, Metrics.Notoriety, 2, Causes.Inquest, record.Id);
                Ledger.Adjust(state, Metrics.Heat, -2, Causes.Inquest, record.Id);
                outcome = InquestStates.Bribed;
            }
        }
        else if (rng.Chance(ImplicationChance(state, record, approach)))
        {
            // a proprietor who would not come and speak for his bottle looks the guiltier for it
            Implicate(approach == "silent" ? 28 : 20, 12, 8);
            outcome = InquestStates.Implicated;
        }
        else
        {
            Ledger.Adjust(state, Metrics.Heat, -3, Causes.Inquest, record.Id);
            if (approach == "testify") Ledger.Adjust(state, Metrics.Reputation, 1, Causes.Inquest, record.Id);
            outcome = InquestStates.Cleared;
        }

        // A chemist's analysis at an honest inquest reveals a mis-measured batch.
        if (record.BadBatch && approach != "bribe") state.Player.Flags["suffered_bad_batch"] = true;

        record.Inquest = outcome;
        record.Verdict = outcome == InquestStates.Implicated ? "proprietary_poisoning" : rng.Pick(cfg.BenignVerdicts);
        state.Scrutiny.InquestsHeld += 1;
        state.Player.Flags["survived_inquest"] = true;
        state.Month.News.Add(new InquestNews(record.Id, outcome, record.Verdict));
        return null;
    }

    /// <summary>Turn phase: talk dies down over time.</summary>
    public static readonly Phase HeatDecayPhase = new("heat_decay", ctx =>
    {
        var cfg = ctx.Content.Config.Scrutiny;
        var s = ctx.State.Scrutiny;
        double decay = Math.Min(s.Heat, cfg.HeatDecayFlat + s.Heat * cfg.HeatDecayRate);
        if (decay > 0) Ledger.Adjust(ctx.State, Metrics.Heat, -decay, Causes.HeatDecay);
    });
}
