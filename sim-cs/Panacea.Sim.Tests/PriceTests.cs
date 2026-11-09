using Xunit;

namespace Panacea.Sim.Tests;

/// <summary>Balance pass 8 (packages/sim/test/price.test.ts): what the ward can pay, and how many of each kind it has.</summary>
public class PriceTests
{
    private static readonly ContentPack C = Fixtures.Content;

    private static readonly FormulaComponent[] Spirits =
        [new("grain_alcohol", 3), new("valerian", 1), new("sugar_syrup", 1), new("water", 3)];

    private static readonly FormulaComponent[] Laudanum =
        [new("grain_alcohol", 2), new("laudanum", 1), new("sugar_syrup", 1), new("water", 4)];

    private static GameState Start(string seed) => Game.NewGame(C, new NewGameOptions { Seed = seed });

    private static TurnContext Ctx(GameState s, string seed) => new() { State = s, Content = C, Rng = Rng.FromSeed(seed) };

    private static GameState Stocked(FormulaComponent[] components, int priceCents, int bottles, string seed)
    {
        var s = Start(seed);
        s.Player.CashCents = 1_000_000;
        s.Month.StartCashCents = s.Player.CashCents;
        var (next, r) = Game.ApplyAction(s, new CreateFormula("T", components, "simple_mix", priceCents), C);
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

    private static Offer OfferAt(FormulaComponent[] components, int priceCents)
    {
        var f = Stocked(components, priceCents, 1, "offer").Lab.Formulas[0];
        return new Offer(f, Formulas.Analyze(components, "simple_mix", C));
    }

    private static Customer Labourer() => Start("dear").Clone().Customers.First(c => c.Segment == "labourer");

    [Fact]
    public void ARegularBuysEveryMonthUpToALittleOverTheirUsualPriceAndLessOftenAboveIt()
    {
        var m = C.Config.Market;
        var c = Labourer();
        double comfortable = c.Profile.BaseWillingnessCents * m.DearAbove;
        Assert.Equal(1, Market.Dearness(c, OfferAt(Spirits, (int)Math.Floor(comfortable)), C));
        int p15 = (int)JsMath.Round(comfortable * 1.5);
        int p2 = (int)JsMath.Round(comfortable * 2);
        double dear = Market.Dearness(c, OfferAt(Spirits, p15), C);
        Assert.Equal(Math.Pow(comfortable / p15, m.DearElasticity), dear, 9);
        Assert.True(dear < 1);
        double dearer = Market.Dearness(c, OfferAt(Spirits, p2), C);
        Assert.True(dearer < dear);
        Assert.True(dearer * p2 < dear * p15);
    }

    [Fact]
    public void ATasteForSpiritsDoesNotPayADearPriceANarcoticHabitDoes()
    {
        var m = C.Config.Market;
        var c = Labourer();
        c.Body.Dependency = 1;
        int price = (int)JsMath.Round(c.Profile.BaseWillingnessCents * m.DearAbove * 1.6);
        Assert.Equal(0, Market.NarcoticHabit(OfferAt(Spirits, price).Analysis));
        Assert.True(Market.Dearness(c, OfferAt(Spirits, price), C) < 1);
        Assert.True(Market.NarcoticHabit(OfferAt(Laudanum, price).Analysis) >= m.DearHabitHold);
        Assert.Equal(1, Market.Dearness(c, OfferAt(Laudanum, price), C));
    }

    [Fact]
    public void RegularsWhoWouldPayGoWithoutADearBottleAndAreCountedAsTurnedAwayByPrice()
    {
        var s = Stocked(Spirits, 140, 400, "shelf");
        foreach (var c in s.Customers)
        {
            c.Body.Dependency = 1;
            c.Satisfaction = 1;
        }
        Market.SalesPhase.Run(Ctx(s, "shelf"));
        int without = s.Customers.Count(c => c.BottlesLastMonth == 0);
        Assert.True(without >= 3, $"{without} regulars went without");
        Assert.True(s.Month.TurnedAwayByPrice >= without - 1);
        Assert.Empty(Invariants.Check(s, C));
    }

    [Fact]
    public void AShopWinsAtMostItsSlackTimesCapacityTimesTheKindsShareOfAnyOneKind()
    {
        double total = C.Segments.Sum(x => x.Weight);
        double Share(string kind) => C.ById.Segments[kind].Weight / total;
        GameState Filled(string kind, int n)
        {
            var s = Start("kinds");
            s.Customers.Clear();
            for (int i = 0; i < n; i++)
            {
                var c = Start("kinds").Customers[0];
                c.Id = $"c{i}";
                c.Segment = kind;
                s.Customers.Add(c);
            }
            return s;
        }
        const int capacity = 70;
        double limit = C.Config.WalkIns.WardShareSlack * capacity * Share("clerk");
        Assert.InRange(limit, 20, capacity / 2.0);
        Assert.True(Market.WardHasRoomFor(Filled("clerk", (int)Math.Ceiling(limit) - 1), C, "clerk", capacity));
        Assert.False(Market.WardHasRoomFor(Filled("clerk", (int)Math.Ceiling(limit)), C, "clerk", capacity));
        Assert.True(Market.WardHasRoomFor(Filled("clerk", 60), C, "labourer", capacity));
        Assert.True(C.Config.WalkIns.WardShareSlack * capacity * Share("well_to_do") < 10);
    }
}
