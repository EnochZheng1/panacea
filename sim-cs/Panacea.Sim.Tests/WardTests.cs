using Xunit;

namespace Panacea.Sim.Tests;

/// <summary>Balance pass 7 (packages/sim/test/ward.test.ts): the ward remembers, poison tempts, sales by source, the reported cause.</summary>
public class WardTests
{
    private static readonly ContentPack C = Fixtures.Content;

    private static readonly FormulaComponent[] Mild =
        [new("grain_alcohol", 2), new("laudanum", 0.25), new("sugar_syrup", 1), new("water", 4.75)];

    private static readonly FormulaComponent[] Strong =
        [new("grain_alcohol", 2), new("laudanum", 1.25), new("sugar_syrup", 1), new("water", 3.75)];

    private static GameState Start(string seed) => Game.NewGame(C, new NewGameOptions { Seed = seed });

    private static TurnContext Ctx(GameState s, string seed) => new() { State = s, Content = C, Rng = Rng.FromSeed(seed) };

    /// <summary>A shop with <paramref name="bottles"/> of a tonic on the shelf, at 70c.</summary>
    private static GameState Stocked(FormulaComponent[] components, int bottles, string seed)
    {
        var s = Start(seed);
        s.Player.CashCents = 1_000_000;
        s.Month.StartCashCents = s.Player.CashCents;
        var (next, r) = Game.ApplyAction(s, new CreateFormula("T", components, "simple_mix", 70), C);
        Assert.True(r.Ok);
        s = next;
        foreach (var c in components)
            s = Game.ApplyAction(s, new BuyIngredient(c.IngredientId, (int)Math.Ceiling(c.Amount * bottles)), C).State;
        int extra = bottles - C.Config.StillBottles;
        if (extra > 0) s.Player.Capabilities["still_bottles"] = extra;
        var (brewed, br) = Game.ApplyAction(s, new BrewBatch(s.Lab.Formulas[0].Id, bottles), C);
        Assert.True(br.Ok, br.Error);
        Lab.ProductionPhase.Run(Ctx(brewed, seed));
        return brewed;
    }

    private static DeathInput Death(string cause = DeathCauses.Overdose, bool implicated = true) => new()
    {
        Name = "Mr. Test", Segment = "labourer", Consumer = Consumers.Self, Ailment = "rheumatism",
        Cause = cause, ProductImplicated = implicated,
    };

    [Fact]
    public void TheWardNamesAnOverdoseAndAWastingAndOtherwiseWhatADeathLookedLike()
    {
        Assert.Equal(ReportedCauses.Overdose, WardSystem.ReportedCauseOf(DeathCauses.Overdose, "labourer", "rheumatism", C));
        Assert.Equal(ReportedCauses.Wasting, WardSystem.ReportedCauseOf(DeathCauses.Poisoning, "labourer", "rheumatism", C));
        Assert.Equal(ReportedCauses.Ailment, WardSystem.ReportedCauseOf(DeathCauses.Disease, "seamstress", "consumption", C));
        Assert.Equal(ReportedCauses.Ailment, WardSystem.ReportedCauseOf(DeathCauses.Natural, "seamstress", "consumption", C));
        Assert.Equal(ReportedCauses.Ailment, WardSystem.ReportedCauseOf(DeathCauses.Natural, "mother", "teething_infant", C));
        Assert.Equal(ReportedCauses.OldAge, WardSystem.ReportedCauseOf(DeathCauses.Natural, "elderly", "rheumatism", C));
        Assert.Equal(ReportedCauses.Natural, WardSystem.ReportedCauseOf(DeathCauses.Natural, "clerk", "dyspepsia", C));

        var s = Start("hastened");
        Scrutiny.RecordDeath(s, C, Rng.FromSeed("a"), Death(DeathCauses.Natural, false));
        Scrutiny.RecordDeath(s, C, Rng.FromSeed("b"), Death(DeathCauses.Natural, true));
        var seen = Machine.Observe(s, C).Deaths;
        Assert.All(seen, d => Assert.Equal(ReportedCauses.Natural, d.ReportedCause));
        Assert.Equal("rheumatism", seen[0].Ailment);
    }

    [Fact]
    public void ADeathAndAVerdictWeighOnTheWardAndFade()
    {
        var s = Start("memory");
        var cfg = C.Config.Ward;
        Scrutiny.RecordDeath(s, C, Rng.FromSeed("d"), Death());
        Scrutiny.RecordDeath(s, C, Rng.FromSeed("n"), Death(DeathCauses.Natural, false));
        WardSystem.WardPhase.Run(Ctx(s, "w"));
        Assert.Equal(cfg.PerDeath, s.Ward.Memory, 9);

        s.Month.News = [new InquestNews(s.Deaths[0].Id, InquestStates.Implicated, "proprietary_poisoning")];
        double before = s.Ward.Memory;
        WardSystem.WardPhase.Run(Ctx(s, "w"));
        Assert.Equal(before * (1 - cfg.MemoryDecay) - cfg.MemoryDecayFlat + cfg.PerVerdict, s.Ward.Memory, 9);
        var o = Machine.Observe(s, C);
        Assert.Equal(s.Ward.Memory, o.WardMemory);
        Assert.Equal(1, o.WardRemembers.Verdicts);
        Assert.Equal(cfg.RememberMonths, o.WardRemembers.Months);

        double peak = s.Ward.Memory;
        for (int i = 0; i < 12; i++)
        {
            s.Month.News = [];
            WardSystem.WardPhase.Run(Ctx(s, "w"));
        }
        Assert.InRange(s.Ward.Memory, 1e-9, peak * 0.35);
        Assert.Equal(peak, s.Ward.PeakMemory);
    }

    [Fact]
    public void TheMemoryPutsOffStrangersAtTheCounter()
    {
        int forgotten = 0, remembered = 0;
        for (int i = 0; i < 20; i++)
        {
            var a = Stocked(Mild, 300, $"w{i}");
            var b = a.Clone();
            b.Ward.Memory = 100;
            Market.SalesPhase.Run(Ctx(a, $"s{i}"));
            Market.SalesPhase.Run(Ctx(b, $"s{i}"));
            forgotten += a.Month.Sales[0].WalkInBottles;
            remembered += b.Month.Sales[0].WalkInBottles;
        }
        Assert.True(remembered < forgotten * (1 - C.Config.Ward.WalkInLoss / 2), $"{remembered} vs {forgotten}");
    }

    [Fact]
    public void EverySaleIsUnderOneSourceAndThePostIsCountedByRemedy()
    {
        var s = Stocked(Mild, 200, "split");
        s.Player.Investments["almanac"] = new InvestmentHolding { BoughtTurn = 0, EndsTurn = null };
        Market.SalesPhase.Run(Ctx(s, "split"));
        var sale = s.Month.Sales.Single();
        Assert.True(sale.PostBottles > 0);
        Assert.Equal(sale.Bottles, sale.ShopBottles + sale.WalkInBottles + sale.WholesaleBottles + sale.PostBottles);
        Assert.Equal(s.Month.Post.RevenueCents, sale.PostRevenueCents);
        Assert.Equal(sale.WalkInBottles * 70L, sale.WalkInRevenueCents);
        Assert.Empty(Invariants.Check(s, C));
    }

    [Fact]
    public void ARegularWhoCravesAStrongBottlePaysOverItsPrice()
    {
        var strong = Stocked(Strong, 10, "p");
        var mild = Stocked(Mild, 10, "q");
        Offer OfferOf(GameState g) => new(g.Lab.Formulas[0], Formulas.Analyze(g.Lab.Formulas[0].Components, "simple_mix", C));
        var hooked = strong.Clone().Customers[0];
        hooked.Body.Dependency = 0.9;
        Assert.True(Market.CravingPremiumCents(hooked, OfferOf(strong), C) > 0);
        Assert.Equal(0, Market.CravingPremiumCents(hooked, OfferOf(mild), C));
        hooked.DebtCents = 1_000_000;
        Assert.Equal(0, Market.CravingPremiumCents(hooked, OfferOf(strong), C));

        var books = Stocked(Strong, 300, "books");
        foreach (var c in books.Customers) c.Body.Dependency = 0.9;
        Market.SalesPhase.Run(Ctx(books, "books"));
        long premium = books.Month.Sales[0].CravingPremiumCents;
        Assert.True(premium > 0);
        Assert.Equal(premium, books.Month.Deltas.Where(d => d.Cause == Causes.SalesCraving).Sum(d => d.Amount));
        Assert.Empty(Invariants.Check(books, C));

        Assert.Equal(0, Market.WordOfStrength([OfferOf(mild)], C));
        Assert.True(Market.WordOfStrength([OfferOf(strong)], C) > 5);
    }
}
