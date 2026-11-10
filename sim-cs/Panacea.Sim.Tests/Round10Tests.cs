using Xunit;

namespace Panacea.Sim.Tests;

/// <summary>
/// Round-10 balance (packages/sim/test/round10.test.ts): a label's promise, suspicion that always has
/// a reason, inquests that move it by what their letter prints, weaning off an old recipe, the good
/// side of being better known, and letters grounded in what happened.
/// </summary>
public class Round10Tests
{
    private static readonly ContentPack C = Fixtures.Content;

    private static FormulaComponent[] Recipe(double alcohol, double laudanum = 0)
    {
        var parts = new List<FormulaComponent> { new("grain_alcohol", alcohol) };
        if (laudanum > 0) parts.Add(new("laudanum", laudanum));
        parts.Add(new("valerian", 1));
        parts.Add(new("sugar_syrup", 1));
        parts.Add(new("water", 6 - alcohol - laudanum));
        return parts.ToArray();
    }

    private static readonly FormulaComponent[] Mild = Recipe(2);
    private static readonly FormulaComponent[] HalfOz = Recipe(2, 0.5);
    private static readonly string[] Bold = ["purifies_the_blood", "banishes_rheumatism", "cures_consumption", "soothes_nerves"];

    private static GameState Start(string seed) => Game.NewGame(C, new NewGameOptions { Seed = seed });

    /// <summary>A shop with one remedy of this recipe and label on the shelf.</summary>
    private static GameState Shop(FormulaComponent[] components, string[] claims, string seed)
    {
        var s = Start(seed);
        s.Player.CashCents = 1_000_000;
        s.Month.StartCashCents = s.Player.CashCents;
        var (next, r) = Game.ApplyAction(s, new CreateFormula("T", components, "simple_mix", 60, claims), C);
        Assert.True(r.Ok, r.Error);
        s = next;
        s.Lab.Stock.Add(new Lot
        {
            Id = "lot_t",
            FormulaId = s.Lab.Formulas[0].Id,
            Components = components.ToList(),
            ProcessId = "simple_mix",
            Bottles = 200,
            Potency = 1,
            BadBatch = false,
        });
        return s;
    }

    private static double HeatBy(GameState s, string cause) =>
        s.Month.Deltas.Where(d => d.Metric == Metrics.Heat && d.Cause == cause).Sum(d => d.Amount);

    // --- a label's promise ----------------------------------------------------------------------

    [Fact]
    public void AnOrdinaryClaimTheBottleMakesGoodCostsNothingOneItDoesNotAtMostItsPromise()
    {
        double promise = C.Config.Market.PromiseShare * C.ById.Claims["soothes_nerves"].Appeal;
        Assert.Equal(0, Formulas.PromiseShortfall(["soothes_nerves"], "nerves", promise + 1, 12, C));
        Assert.Equal(promise / 2, Formulas.PromiseShortfall(["soothes_nerves"], "nerves", promise / 2, 12, C), 9);
        Assert.Equal(promise, Formulas.PromiseShortfall(["soothes_nerves"], "nerves", -5, 12, C), 9);
        Assert.Equal(0, Formulas.PromiseShortfall(["soothes_nerves"], "rheumatism", -5, 12, C));
    }

    [Fact]
    public void AClaimToCureIsNeverMadeGoodAndIsGivenAFewMonths()
    {
        var m = C.Config.Market;
        double full = m.CureShare * C.ById.Claims["cures_consumption"].Appeal;
        Assert.True(C.ById.Claims["cures_consumption"].Cures);
        Assert.False(C.ById.Claims["soothes_nerves"].Cures);
        Assert.Equal(0, Formulas.PromiseShortfall(["cures_consumption"], "consumption", 99, 0, C));
        Assert.Equal(full, Formulas.PromiseShortfall(["cures_consumption"], "consumption", 99, m.CurePatience, C));
        Assert.Equal(full / 2, Formulas.PromiseShortfall(["cures_consumption"], "consumption", 99, m.CurePatience / 2, C), 9);
        Assert.Equal(0, Formulas.PromiseShortfall(["cures_consumption"], "dyspepsia", 99, 24, C));
    }

    /// <summary>One regular of this complaint takes two bottles a month of this label for n months.</summary>
    private static (Customer C, int Disappointed) RegularOn(string[] claims, string ailment, int n, double hooked = 0)
    {
        var s = Shop(Mild, claims, "promise");
        var c = s.Customers.First(x => x.Consumer == Consumers.Self);
        c.Ailment = ailment;
        c.AilmentEndsTurn = null;
        s.Customers = [c];
        var bought = new Bought(s.Lab.Formulas[0].Id, "lot_t", Mild, "simple_mix", 1, false);
        int disappointed = 0;
        for (int m = 0; m < n; m++)
        {
            s.Meta.Turn += 1;
            s.Month.Disappointed = 0;
            // a regular hooked on an old recipe (the dependency is set again each month)
            if (hooked > 0) c.Body.Dependency = hooked / (1 - C.Config.Body.WeaningDecay);
            var ctx = new TurnContext { State = s, Content = C, Rng = Rng.FromSeed($"p{m}") };
            ctx.Scratch.Purchases[c.Id] = [bought, bought];
            BodySystem.BodyPhase.Run(ctx);
            c.Body.Health = 100;
            disappointed += s.Month.Disappointed;
            if (!s.Customers.Contains(c)) s.Customers = [c];
        }
        return (c, disappointed);
    }

    [Fact]
    public void AConsumptiveSoldACureIsLessPleasedWithTheSameBottleAndIsCountedDisappointed()
    {
        var plain = RegularOn(["soothes_nerves"], "consumption", 8);
        var cure = RegularOn(["cures_consumption"], "consumption", 8);
        Assert.True(cure.C.Satisfaction < plain.C.Satisfaction - 0.1, $"{cure.C.Satisfaction} vs {plain.C.Satisfaction}");
        Assert.Equal(0, plain.Disappointed);
        Assert.True(cure.Disappointed > 5, $"{cure.Disappointed} disappointed");
    }

    [Fact]
    public void ARegularWhoCravesTheBottleReadsTheLabelLess()
    {
        double Gap(double hooked) =>
            RegularOn(["soothes_nerves"], "consumption", 8, hooked).C.Satisfaction -
            RegularOn(["cures_consumption"], "consumption", 8, hooked).C.Satisfaction;
        Assert.True(Gap(0.6) > 0);
        Assert.True(Gap(0.6) < Gap(0) * 0.7, $"{Gap(0.6)} vs {Gap(0)}");
    }

    [Fact]
    public void ClaimsToCureADeadlyDiseaseDrawHeatOfTheirOwnACureForRheumatismDoesNot()
    {
        Assert.Equal(1, Formulas.CureClaims(Bold, C));
        Assert.Equal(2, Formulas.CureClaims(["cures_everything", "cures_consumption"], C));
        Assert.Equal(0, Formulas.CureClaims(["banishes_rheumatism"], C));
        var s = Shop(Mild, Bold, "cures");
        Market.AdvertisingPhase.Run(new TurnContext { State = s, Content = C, Rng = Rng.FromSeed("c") });
        Assert.Equal(C.Config.Advertising.CuresHeat, HeatBy(s, Causes.CureClaims), 9);
        var r = Shop(Mild, ["banishes_rheumatism"], "rheum");
        Market.AdvertisingPhase.Run(new TurnContext { State = r, Content = C, Rng = Rng.FromSeed("c") });
        Assert.Equal(0, HeatBy(r, Causes.CureClaims));
    }

    // --- suspicion always has a reason ------------------------------------------------------------

    private static DeathRecord Death(GameState s, string ailment, bool productImplicated) =>
        Scrutiny.RecordDeath(s, C, Rng.FromSeed("d"), new DeathInput
        {
            CustomerId = "cust_x",
            Name = "Mr. Lefferts",
            Segment = "clerk",
            Consumer = Consumers.Self,
            Anonymous = false,
            Ailment = ailment,
            Cause = DeathCauses.Natural,
            ProductImplicated = productImplicated,
            FormulaId = s.Lab.Formulas[0].Id,
            BadBatch = false,
        });

    [Fact]
    public void ADeathTheBottleHadNoPartInDoesNotMoveTheSuspicion()
    {
        var s = Shop(Mild, ["soothes_nerves"], "fever");
        var d = Death(s, "nerves", false);
        Assert.Equal(0, s.Scrutiny.Heat);
        Assert.False(d.PromisedCure);
        Assert.Equal(0, WardSystem.DeathWeight(d, C));
    }

    [Fact]
    public void AConsumptiveWhoDiesUnderALabelThatCuredConsumptionIsABrokenPromise()
    {
        var s = Shop(Mild, Bold, "promise");
        var d = Death(s, "consumption", false);
        Assert.True(d.PromisedCure);
        Assert.Equal(C.Config.Scrutiny.HeatPerBrokenPromise, HeatBy(s, Causes.BrokenPromise));
        Assert.Equal(0, HeatBy(s, Causes.Death));
        Assert.Equal(C.Config.Ward.PerBrokenPromise, WardSystem.DeathWeight(d, C));
        Assert.False(Formulas.PromisesCure(Bold, "rheumatism", C));
        Assert.True(Formulas.PromisesCure(["cures_everything"], "teething_infant", C));
    }

    [Fact]
    public void EveryChangeToTheSuspicionOverAYearOfBoldClaimsAndLaudanumHasAPlainCause()
    {
        var s = Shop(HalfOz, Bold, "causes");
        s.Unlocks.Unlocked["newspaper_ads"] = 0;
        var (next, r) = Game.ApplyAction(s, new SetAdSpend(600), C);
        Assert.True(r.Ok, r.Error);
        s = next;
        for (int m = 0; m < 12 && s.Meta.Status == GameStatus.Running; m++)
        {
            foreach (var p in s.Events.Pending.ToList())
            {
                if (p.EventId != "inquest") continue;
                (s, r) = Game.ApplyAction(s, new ResolveEvent(p.InstanceId, "stay_silent"), C);
                Assert.True(r.Ok, r.Error);
            }
            s = Game.EndTurn(s, C).State;
        }
        var heat = s.Ledger.SelectMany(e => e.Deltas).Where(d => d.Metric == Metrics.Heat).ToList();
        Assert.NotEmpty(heat);
        foreach (var d in heat)
        {
            Assert.Contains(d.Cause, Causes.All);
            Assert.True(C.Strings.ContainsKey($"cause.{d.Cause}"), d.Cause);
        }
    }

    [Fact]
    public void AnInquestsVerdictMovesTheSuspicionByExactlyWhatTheCoronersLetterPrints()
    {
        var cfg = C.Config.Scrutiny;
        // the numbers LettersDesk prints under the replies
        Assert.Equal([24.0, 12, 8], new[] { cfg.NamedHeat, cfg.NamedReputation, cfg.NamedNotoriety });
        Assert.Equal([3.0, 1], new[] { cfg.ClearedHeat, cfg.TestifiedReputation });
        Assert.Equal([30.0, 15, 10], new[] { cfg.ExposedHeat, cfg.ExposedReputation, cfg.ExposedNotoriety });
        Assert.Equal(2, cfg.BribedNotoriety);
        int named = 0, cleared = 0;
        for (int i = 0; i < 40; i++)
        {
            var s = Shop(HalfOz, [], $"inq{i}");
            s.Scrutiny.Heat = 40;
            s.Month.StartHeat = 40;
            s.Player.Reputation = 30;
            s.Month.StartReputation = 30;
            var d = Death(s, "nerves", true);
            d.Inquest = InquestStates.Pending;
            double heat = s.Scrutiny.Heat, rep = s.Player.Reputation;
            Assert.Null(Scrutiny.ResolveInquest(s, C, Rng.FromSeed($"i{i}"), d.Id, "testify"));
            if (d.Inquest == InquestStates.Implicated)
            {
                named += 1;
                Assert.Equal(cfg.NamedHeat, s.Scrutiny.Heat - heat, 9);
                Assert.Equal(cfg.NamedReputation, rep - s.Player.Reputation, 9);
            }
            else
            {
                cleared += 1;
                Assert.Equal(cfg.ClearedHeat, heat - s.Scrutiny.Heat, 9);
                Assert.Equal(cfg.TestifiedReputation, s.Player.Reputation - rep, 9);
            }
        }
        Assert.True(named > 0);
        Assert.True(cleared > 0);
    }

    // --- weaning off an old recipe ----------------------------------------------------------------

    /// <summary>A craving regular takes three bottles a month of this recipe; dependency after each month.</summary>
    private static (List<double> Deps, Customer C) Months(FormulaComponent[] components, int n)
    {
        var s = Start("wean");
        var c = s.Customers.First(x => x.Consumer == Consumers.Self);
        s.Customers = [c];
        c.Body.Dependency = 0.8;
        var bought = new Bought("f", "lot", components, "simple_mix", 1, false);
        var deps = new List<double>();
        for (int m = 0; m < n; m++)
        {
            var ctx = new TurnContext { State = s, Content = C, Rng = Rng.FromSeed($"w{m}") };
            ctx.Scratch.Purchases[c.Id] = [bought, bought, bought];
            BodySystem.BodyPhase.Run(ctx);
            c.Body.Health = 100;
            if (!s.Customers.Contains(c)) s.Customers = [c];
            deps.Add(c.Body.Dependency);
        }
        return (deps, c);
    }

    [Fact]
    public void OnceTheNarcoticIsOutOfTheBottleTheCravingFadesWithinASeason()
    {
        var b = C.Config.Body;
        var (deps, _) = Months(Mild, 4);
        Assert.True(deps[0] > 0.8 * (1 - b.WeaningDecay));
        Assert.True(deps[0] < 0.8 * (1 - b.WeaningDecay) + 0.05);
        Assert.True(deps[2] < b.WithdrawalThreshold);
        Assert.True(deps[3] <= b.MildDependencyCap);
    }

    [Fact]
    public void AWeaningRegularIsMarkedSoOneStillOnLaudanumIsNotAndStillCraves()
    {
        var (_, weaned) = Months(Mild, 1);
        Assert.True(weaned.Weaning);
        var (deps, fed) = Months(HalfOz, 4);
        Assert.False(fed.Weaning);
        Assert.True(deps[3] > C.Config.Body.WithdrawalThreshold);
    }

    // --- being better known ------------------------------------------------------------------------

    [Fact]
    public void BeingBetterKnownBringsMoreStrangersInOffTheStreet()
    {
        var w = C.Config.WalkIns;
        int Callers(double notoriety)
        {
            int most = 0;
            for (int i = 0; i < 40; i++)
            {
                var s = Shop(Mild, [], $"known{i}");
                s.Player.Reputation = 0;
                s.Player.Notoriety = notoriety;
                Market.SalesPhase.Run(new TurnContext { State = s, Content = C, Rng = Rng.FromSeed($"k{i}") });
                most = Math.Max(most, s.Month.Callers.TryGetValue("ward", out var n) ? n : 0);
            }
            return most;
        }
        Assert.True(w.PerNotoriety > 0);
        Assert.True(Callers(0) <= w.Base);
        Assert.True(Callers(w.NotorietyCap) > w.Base);
        // a name the papers print for poisoning brings no more of the curious than a name well known
        Assert.Equal(Callers(w.NotorietyCap), Callers(10 * w.NotorietyCap));
    }

    // --- letters grounded in what happened ---------------------------------------------------------

    private static void Died(GameState s, int monthsAgo, double owedCents, bool anonymous = false) =>
        s.Deaths.Add(new DeathRecord
        {
            Id = $"death_{s.Deaths.Count}",
            Turn = s.Meta.Turn - monthsAgo,
            Date = s.Meta.Date,
            CustomerId = anonymous ? null : "cust_x",
            Name = anonymous ? "" : "Mr. Dunne",
            Segment = "clerk",
            Consumer = Consumers.Self,
            Anonymous = anonymous,
            Ailment = "nerves",
            Cause = DeathCauses.Natural,
            ReportedCause = "natural",
            ProductImplicated = false,
            FormulaId = null,
            BadBatch = false,
            Detectability = 0,
            Boldness = 0,
            PromisedCure = false,
            OwedCents = owedCents,
            Inquest = InquestStates.None,
            Verdict = null,
        });

    [Fact]
    public void TheWidowWritesOnlyAfterARegularDiedOwingOnTheBottleThisHalfYear()
    {
        Assert.Contains(C.ById.Events["widow_account"].Condition is AllRequirement all ? all.Of : [], x => x is RegularDiedRequirement);
        var req = new RegularDiedRequirement(6, true);
        var s = Start("widow");
        s.Meta.Turn = 10;
        Assert.False(Requirements.Evaluate(req, s, C).Met);
        Died(s, 2, 0);
        Assert.False(Requirements.Evaluate(req, s, C).Met);
        Assert.True(Requirements.Evaluate(req with { Owing = false }, s, C).Met);
        Died(s, 2, 500, true);
        Assert.False(Requirements.Evaluate(req, s, C).Met);
        Died(s, 8, 500);
        Assert.False(Requirements.Evaluate(req, s, C).Met);
        Died(s, 3, 460);
        Assert.True(Requirements.Evaluate(req, s, C).Met);
    }

    [Fact]
    public void EveryVentureSaysTheMonthlySumsItsTermsCharge()
    {
        var words = new Dictionary<long, string>
        {
            [100] = "a dollar", [200] = "two dollars", [300] = "three dollars", [400] = "four dollars", [500] = "five dollars",
        };
        foreach (var inv in C.Investments)
        {
            var text = C.Text($"investment.{inv.Id}.description");
            foreach (var (cents, w) in words)
                if (text.Contains($"{w} more a month") || text.Contains($"{w} a month"))
                    Assert.True(inv.UpkeepCents == cents, $"{inv.Id}: says {w}, charges {inv.UpkeepCents}c");
        }
        Assert.Contains("three dollars", C.Text("investment.shop_next_door.description"));
        Assert.Contains("three dollars", C.Text("investment.fourth_still.description"));
    }

    [Fact]
    public void OnlyThePumpWaterIsFree()
    {
        foreach (var ing in C.Ingredients)
            if (ing.Id != "water") Assert.True(Math.Round(ing.CostPerUnitCents) > 0, ing.Id);
    }
}
