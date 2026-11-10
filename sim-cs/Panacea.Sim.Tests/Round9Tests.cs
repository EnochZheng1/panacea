using Xunit;

namespace Panacea.Sim.Tests;

/// <summary>
/// Round-9 balance (packages/sim/test/round9.test.ts): mild bottles, the careful households, and
/// the coroner's envelope.
/// </summary>
public class Round9Tests
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

    /// <summary>The cozy player's mild tonic: 2 oz of spirit (habit 0.8, the assay's "none").</summary>
    private static readonly FormulaComponent[] Mild = Recipe(2);
    /// <summary>A stronger spirit tonic: 2.5 oz (habit 1, "they may come back").</summary>
    private static readonly FormulaComponent[] Spirited = Recipe(2.5);
    private static readonly FormulaComponent[] QuarterOz = Recipe(2, 0.25);
    private static readonly FormulaComponent[] HalfOz = Recipe(2, 0.5);

    private static GameState Start(string seed) => Game.NewGame(C, new NewGameOptions { Seed = seed });

    private static GameState Stocked(FormulaComponent[] components, int priceCents, int bottles, string seed,
        string[]? claims = null)
    {
        var s = Start(seed);
        s.Player.CashCents = 1_000_000;
        s.Month.StartCashCents = s.Player.CashCents;
        var (next, r) = Game.ApplyAction(s, new CreateFormula("T", components, "simple_mix", priceCents, claims ?? []), C);
        Assert.True(r.Ok, r.Error);
        s = next;
        s.Lab.Stock.Add(new Lot
        {
            Id = "lot_t",
            FormulaId = s.Lab.Formulas[0].Id,
            Components = components.ToList(),
            ProcessId = "simple_mix",
            Bottles = bottles,
            Potency = 1,
            BadBatch = false,
        });
        return s;
    }

    private static Offer OfferOf(FormulaComponent[] components, int priceCents = 60, string[]? claims = null) =>
        new(Stocked(components, priceCents, 1, "offer", claims).Lab.Formulas[0], Formulas.Analyze(components, "simple_mix", C));

    private static Buyer MakeBuyer(bool wary, string segment = "mother")
    {
        var seg = C.ById.Segments[segment];
        return new Buyer(seg.Profile, seg.Ailments[0].Id, Pharmacology.ZeroTolerance(), 0, wary);
    }

    // --- a mild bottle makes a taste for it, never a craving ---------------------------------

    /// <summary>One healthy regular takes three bottles of this recipe a month for two years.</summary>
    private static Customer TwoYears(FormulaComponent[] components, double dependency = 0)
    {
        var s = Start("mild");
        var c = s.Customers.First(x => x.Consumer == Consumers.Self);
        s.Customers = [c];
        c.Body.Dependency = dependency;
        c.Body.Health = 100;
        var bought = new Bought("f", "lot", components, "simple_mix", 1, false);
        for (int m = 0; m < 24; m++)
        {
            var ctx = new TurnContext { State = s, Content = C, Rng = Rng.FromSeed($"mild{m}") };
            ctx.Scratch.Purchases[c.Id] = [bought, bought, bought];
            BodySystem.BodyPhase.Run(ctx);
            c.Body.Health = 100;
            if (!s.Customers.Contains(c)) s.Customers = [c];
        }
        return c;
    }

    [Fact]
    public void TheAssayReadsTwoOuncesOfSpiritAsUnderTheFloor()
    {
        var b = C.Config.Body;
        Assert.True(Formulas.Analyze(Mild, "simple_mix", C).Properties.Habit < b.HabitFloor);
        Assert.True(Formulas.Analyze(Spirited, "simple_mix", C).Properties.Habit >= b.HabitFloor);
        Assert.True(b.MildDependencyCap < b.WithdrawalThreshold);
    }

    [Fact]
    public void ARegularOnAMildBottleNeverComesToCraveIt()
    {
        var b = C.Config.Body;
        var c = TwoYears(Mild);
        Assert.True(c.Body.Dependency > 0);
        Assert.True(c.Body.Dependency <= b.MildDependencyCap + 1e-12);
        Assert.True(c.Body.Dependency < b.WithdrawalThreshold);
    }

    [Fact]
    public void AStrongerSpiritTonicOrAnyLaudanumCanMakeACraving()
    {
        Assert.True(TwoYears(Spirited).Body.Dependency > C.Config.Body.WithdrawalThreshold);
        Assert.True(TwoYears(QuarterOz).Body.Dependency > C.Config.Body.WithdrawalThreshold);
    }

    [Fact]
    public void AHabitAlreadyMadeFadesAsItWouldWhenTheBottleIsMadeMild()
    {
        var c = TwoYears(Mild, 0.8);
        Assert.True(c.Body.Dependency < 0.8);
        Assert.True(c.Body.Dependency > C.Config.Body.MildDependencyCap);
    }

    // --- the careful households ---------------------------------------------------------------

    [Fact]
    public void AWaryBuyerWillNotTakeABottleThatSmellsOfThePoppy()
    {
        var m = C.Config.Market;
        Assert.True(Market.NarcoticHabit(OfferOf(HalfOz).Analysis) >= m.WaryHabit);
        Assert.True(Market.NarcoticHabit(OfferOf(QuarterOz).Analysis) < m.WaryHabit);
        Assert.True(Market.RefusesNarcotic(MakeBuyer(true), OfferOf(HalfOz).Analysis, C));
        Assert.False(Market.RefusesNarcotic(MakeBuyer(true), OfferOf(QuarterOz).Analysis, C));
        Assert.False(Market.RefusesNarcotic(MakeBuyer(false), OfferOf(HalfOz).Analysis, C));
    }

    [Fact]
    public void ACleanBottleWithAModestLabelIsItsOwnRecommendationToTheWary()
    {
        var m = C.Config.Market;
        var clean = OfferOf(Mild, 60, ["soothes_nerves"]);
        var bold = OfferOf(Mild, 60, ["cures_everything"]);
        var traced = OfferOf(QuarterOz, 60, ["soothes_nerves"]);
        Assert.True(Market.TrustedByWary(clean, C));
        Assert.False(Market.TrustedByWary(bold, C));
        Assert.False(Market.TrustedByWary(traced, C));
        Assert.True(C.ById.Claims["cures_everything"].Boldness > m.WaryBoldness);
        double plain = Market.LabelAppeal(clean, MakeBuyer(false), C);
        Assert.Equal(plain + m.CleanAppeal, Market.LabelAppeal(clean, MakeBuyer(true), C), 9);
        Assert.Equal(Market.LabelAppeal(bold, MakeBuyer(false), C), Market.LabelAppeal(bold, MakeBuyer(true), C), 9);
    }

    [Fact]
    public void AtTheSamePriceTheWaryTakeTheCleanBottle()
    {
        var s = Stocked(Mild, 60, 10, "two");
        var clean = new Offer(s.Lab.Formulas[0], Formulas.Analyze(Mild, "simple_mix", C));
        var nf = new Formula
        {
            Id = "n",
            Name = "N",
            Components = HalfOz.ToList(),
            ProcessId = "simple_mix",
            PriceCents = 60,
            Claims = [],
            CreatedTurn = 0,
        };
        s.Lab.Formulas.Add(nf);
        s.Lab.Stock.Add(new Lot { Id = "lot_n", FormulaId = "n", Components = HalfOz.ToList(), ProcessId = "simple_mix", Bottles = 10, Potency = 1 });
        var narcotic = new Offer(nf, Formulas.Analyze(HalfOz, "simple_mix", C));
        var labourer = MakeBuyer(false, "labourer");
        Assert.Equal("n", Market.ChooseOffer(s, [clean, narcotic], labourer, C)?.Formula.Id);
        Assert.Equal(clean.Formula.Id, Market.ChooseOffer(s, [clean, narcotic], labourer with { Wary = true }, C)?.Formula.Id);
    }

    [Fact]
    public void WaryRegularsWhoWillNotTakeTheBottleAreCountedApartFromThePrice()
    {
        var s = Stocked(HalfOz, 40, 200, "refuse");
        foreach (var c in s.Customers)
        {
            c.Wary = true;
            c.Satisfaction = 1;
        }
        Market.SalesPhase.Run(new TurnContext { State = s, Content = C, Rng = Rng.FromSeed("refuse") });
        Assert.Equal(0, s.Month.Sales.Sum(x => x.ShopBottles));
        Assert.True(s.Month.RefusedNarcotic > 3, $"{s.Month.RefusedNarcotic} refused");
        Assert.Empty(Invariants.Check(s, C));
    }

    [Fact]
    public void PleasedWaryRegularsSendCallersWhileTheShelfHoldsABottleTheyTrust()
    {
        var s = Start("careful");
        var wary = s.Customers.Where(c => c.Wary).ToList();
        Assert.NotEmpty(wary);
        foreach (var c in s.Customers) c.Satisfaction = 0.5;
        int expected = (int)Math.Floor(C.Config.WalkIns.CarefulPerWary * wary.Count);
        Assert.Equal(expected, Market.CarefulCallers(s, [OfferOf(Mild, 60, ["soothes_nerves"])], C));
        Assert.Equal(0, Market.CarefulCallers(s, [OfferOf(Mild, 60, ["cures_everything"])], C));
        Assert.Equal(0, Market.CarefulCallers(s, [OfferOf(HalfOz)], C));
        foreach (var c in wary) c.Satisfaction = -0.5;
        Assert.Equal(0, Market.CarefulCallers(s, [OfferOf(Mild, 60, ["soothes_nerves"])], C));
    }

    [Fact]
    public void TheWardHasWaryHouseholdsAndThreeWaryStartingRegulars()
    {
        Assert.Equal(0.4, C.ById.Segments["mother"].Wary);
        Assert.Equal(3, C.Regulars.Count(r => r.Wary));
        Assert.Equal(3, Start("w").Customers.Count(c => c.Wary));
        Assert.Equal(1, C.Config.Body.HabitFloor);
        Assert.Equal(2, C.Config.Market.WaryHabit);
    }

    // --- the coroner's envelope ---------------------------------------------------------------

    private static GameState WithInquest(string seed)
    {
        var s = Start(seed);
        s.Player.CashCents = 1_000_000;
        s.Month.StartCashCents = s.Player.CashCents;
        s.Scrutiny.Heat = 40;
        s.Month.StartHeat = 40;
        s.Deaths.Add(new DeathRecord
        {
            Id = "death_x",
            Turn = 0,
            Date = s.Meta.Date,
            CustomerId = null,
            Name = "Mr. Test",
            Segment = "labourer",
            Consumer = Consumers.Self,
            Anonymous = false,
            Ailment = "rheumatism",
            Cause = DeathCauses.Overdose,
            ReportedCause = "overdose",
            ProductImplicated = true,
            FormulaId = null,
            BadBatch = false,
            Detectability = 5,
            Boldness = 0,
            Inquest = InquestStates.Pending,
            Verdict = null,
        });
        return s;
    }

    [Fact]
    public void ABoughtVerdictDoesNotQuietTheTalkAndTheFirstEnvelopeCanComeOut()
    {
        int bought = 0, exposed = 0;
        for (int i = 0; i < 80; i++)
        {
            var s = WithInquest($"env{i}");
            Assert.Null(Scrutiny.ResolveInquest(s, C, Rng.FromSeed($"env{i}"), "death_x", "bribe"));
            if (s.Deaths[0].Inquest == InquestStates.Bribed)
            {
                bought += 1;
                Assert.True(s.Scrutiny.Heat >= 40);
            }
            else exposed += 1;
        }
        Assert.True(bought > 50, $"{bought} bought");
        Assert.True(exposed > 2, $"{exposed} exposed");
        Assert.True(Scrutiny.BribeExposureChance(WithInquest("x"), C) >= C.Config.Scrutiny.BribeExposureBase);
    }

    [Fact]
    public void EachEnvelopeCostsMoreThanTheLastAndMakesTheNextLikelierToComeOut()
    {
        var s = WithInquest("fees");
        var fees = new List<long>();
        var risks = new List<double>();
        for (int k = 0; k < 4; k++)
        {
            fees.Add(Scrutiny.BribeCostCents(s, C));
            risks.Add(Scrutiny.BribeExposureChance(s, C));
            s.Scrutiny.LastBribeCents = fees[k];
            s.Scrutiny.BribesPaid += 1;
        }
        for (int k = 1; k < 4; k++)
        {
            Assert.True(fees[k] - fees[k - 1] >= 500);
            Assert.True(risks[k] > risks[k - 1]);
        }
        Assert.True(fees[3] >= 2 * fees[0]);
    }
}
