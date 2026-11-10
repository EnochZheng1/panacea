using Xunit;

namespace Panacea.Sim.Tests;

/// <summary>A port of the main TS unit tests (packages/sim/test/game.test.ts, m1.test.ts, determinism.test.ts).</summary>
public class GameTests
{
    private static readonly ContentPack C = Fixtures.Content;

    private static readonly FormulaComponent[] Tonic =
    [
        new("grain_alcohol", 2),
        new("laudanum", 0.25),
        new("sugar_syrup", 1),
        new("water", 4.75),
    ];

    private static GameState Start(string seed = "test", string? backgroundId = null) =>
        Game.NewGame(C, new NewGameOptions { Seed = seed, BackgroundId = backgroundId });

    private static void AssertHealthy(GameState s) => Assert.Equal(new List<string>(), Invariants.Check(s, C));

    private static GameState Act(GameState state, params GameAction[] actions)
    {
        foreach (var action in actions)
        {
            var (next, result) = Game.ApplyAction(state, action, C);
            Assert.True(result.Ok, $"{action.Type} should succeed: {result.Error}");
            state = next;
            AssertHealthy(state);
        }
        return state;
    }

    private static GameState Months(GameState state, int n)
    {
        for (int i = 0; i < n && state.Meta.Status == GameStatus.Running; i++)
        {
            Game.EndTurnInPlace(state, C);
            AssertHealthy(state);
        }
        return state;
    }

    /// <summary>Puts a copper big enough for a batch of <paramref name="bottles"/> on every still (a test's shortcut).</summary>
    private static void RoomFor(GameState state, int bottles)
    {
        int extra = bottles - C.Config.StillBottles;
        if (extra > state.Player.Capabilities.GetOr("still_bottles", 0)) state.Player.Capabilities["still_bottles"] = extra;
    }

    private static (GameState State, string FormulaId) SetUpTonic(GameState state, int bottles)
    {
        var (s, result) = Game.ApplyAction(state, new CreateFormula("Test Tonic", Tonic, "simple_mix", 50), C);
        Assert.True(result.Ok);
        RoomFor(s, bottles);
        foreach (var c in Tonic) s = Act(s, new BuyIngredient(c.IngredientId, (int)Math.Ceiling(c.Amount * bottles)));
        return (s, result.CreatedId!);
    }

    // --- content -----------------------------------------------------------------------------

    [Fact]
    public void EmbeddedContentLoads()
    {
        Assert.Equal("0.12.0", C.Config.ContentVersion);
        Assert.Equal(17, C.Ingredients.Count);
        Assert.Equal(3, C.Processes.Count);
        Assert.Equal(9, C.Unlocks.Count);
        Assert.Equal(48, C.Config.StillBottles);
        Assert.Equal(0.12, C.Config.Ward.MemoryDecay);
        Assert.Equal(1, C.Config.Market.CravingPremium);
        Assert.Equal(1.15, C.Config.Market.DearAbove);
        Assert.Equal(3, C.Config.WalkIns.WardShareSlack);
        Assert.Equal(180, C.Events.Count);
        Assert.Equal(8, C.Timeline.Count);
        Assert.Equal(12, C.Regulars.Count);
        Assert.Equal(953, C.Strings.Count);
        Assert.Equal(8, C.Investments.Count);
        Assert.Equal("narcotic_discount_pct", C.ById.Investments["laboratory"].Effects.OfType<CapabilityEffect>().Single().Id);
        Assert.Equal("system", C.ById.Events["inquest"].Trigger);
        Assert.Equal("random", C.ById.Events["grateful_letter"].Trigger);
        Assert.Equal(1, C.ById.Processes["simple_mix"].Multiplier("kick"));
        Assert.Equal(1.1, C.ById.Processes["steep"].Multiplier("kick"));
        Assert.Equal("opiate", C.ById.Ingredients["laudanum"].DrugClass);
        Assert.Same(C, Content.LoadEmbedded());
    }

    [Fact]
    public void EveryLedgerCauseHasADisplayString()
    {
        foreach (var cause in Causes.All) Assert.False(string.IsNullOrEmpty(C.Strings.GetValueOrDefault($"cause.{cause}")), cause);
    }

    // --- new game ------------------------------------------------------------------------------

    [Fact]
    public void StartsOnTheConfiguredDateWithTheBackgroundsCash()
    {
        var s = Start();
        Assert.Equal(C.Config.StartDate, s.Meta.Date);
        Assert.Equal(0, s.Meta.Turn);
        Assert.Equal(C.ById.Backgrounds["druggist"].StartingCashCents, s.Player.CashCents);
        Assert.Equal(1, s.Player.Capabilities["stills"]);
        Assert.Equal(GameState.StateVersion, s.Meta.StateVersion);
        AssertHealthy(s);
    }

    [Fact]
    public void RejectsUnknownBackgrounds() =>
        Assert.Throws<ArgumentException>(() => Start("x", "astronaut"));

    // --- actions -------------------------------------------------------------------------------

    [Fact]
    public void ApplyActionDoesNotMutateTheInput()
    {
        var s = Start();
        string before = Game.HashState(s);
        var (after, _) = SetUpTonic(s, 10);
        Assert.NotSame(s, after);
        Assert.Empty(s.Lab.Formulas);
        Assert.Equal(before, Game.HashState(s));
    }

    [Fact]
    public void RejectedActionsLeaveTheStateUntouchedAndExplainWhy()
    {
        var s = Start();
        string before = Game.HashState(s);
        var (next, result) = Game.ApplyAction(s, new BuyIngredient("morphine", 100_000), C);
        Assert.Equal(ActionResult.Fail("not enough cash"), result);
        Assert.Same(s, next);
        Assert.False(Game.ApplyActionInPlace(s, new BuyIngredient("morphine", 100_000), C).Ok);
        Assert.Equal(before, Game.HashState(s));
    }

    [Fact]
    public void BuyingChargesTheIngredientCostToTheLedger()
    {
        var s = Act(Start(), new BuyIngredient("laudanum", 10));
        Assert.Equal(10, s.Lab.Inventory["laudanum"]);
        var d = Assert.Single(s.Month.Deltas);
        Assert.Equal((Metrics.Cash, -150.0, Causes.Ingredients, "laudanum"), (d.Metric, d.Amount, d.Cause, d.Detail));
    }

    [Fact]
    public void RefusesIngredientsBeforeTheirHistoricalDate() =>
        Assert.Equal("\"heroin\" is not yet available", Game.ApplyAction(Start(), new BuyIngredient("heroin", 1), C).Result.Error);

    [Fact]
    public void ValidatesFormulas()
    {
        var s = Start();
        ActionResult Bad(FormulaComponent[] components, string processId = "simple_mix") =>
            Game.ApplyAction(s, new CreateFormula("X", components, processId, 10), C).Result;
        Assert.Equal("formula has no ingredients", Bad([]).Error);
        Assert.Equal("formula volume 9 oz exceeds bottle capacity 8 oz", Bad([new("water", 9)]).Error);
        Assert.False(Bad([new("water", -1)]).Ok);
        Assert.False(Bad([new("unobtainium", 1)]).Ok);
        Assert.Equal("process \"percolate\" is not unlocked", Bad(Tonic, "percolate").Error);
        Assert.Equal("name must be 1-60 characters",
            Game.ApplyAction(s, new CreateFormula(" \u00a0\ufeff\u2028 ", Tonic, "simple_mix", 10), C).Result.Error);
    }

    [Fact]
    public void BrewingConsumesIngredientsChargesBottlingAndUsesTheStill()
    {
        var (state, formulaId) = SetUpTonic(Start(), 10);
        var s = Act(state, new BrewBatch(formulaId, 10));
        Assert.Equal(0.5, s.Lab.Inventory["laudanum"], 9);
        Assert.Single(s.Lab.Batches);
        Assert.Equal(s.Player.CashCents - s.Month.StartCashCents, Ledger.SumDeltas(s.Month.Deltas, Metrics.Cash));
        Assert.Equal(ActionResult.Fail("no free still this month"), Game.ApplyAction(s, new BrewBatch(formulaId, 1), C).Result);
    }

    [Fact]
    public void FormulaAnalysisSumsIngredientPropertiesPerOunce()
    {
        var a = Formulas.Analyze([new("laudanum", 0.5)], "simple_mix", C);
        Assert.Equal(0.5, a.Volume);
        Assert.Equal(4, a.Properties.Calm);
        Assert.Equal(C.Config.BottleCostCents + 7.5, a.UnitCostCents, 9);
    }

    // --- month end -----------------------------------------------------------------------------

    [Fact]
    public void EndTurnAdvancesTheDateChargesRentAndRecordsALedgerEntry()
    {
        var (state, report) = Game.EndTurn(Start(), C);
        Assert.Equal(new GameDate(1885, 2), state.Meta.Date);
        Assert.Equal(1, state.Meta.Turn);
        Assert.Contains(report.Deltas, d => d.Metric == Metrics.Cash && d.Amount == -C.Config.RentCents && d.Cause == Causes.Rent);
        Assert.Equal(report.CashEndCents - report.CashStartCents, Ledger.SumDeltas(report.Deltas, Metrics.Cash));
    }

    [Fact]
    public void FinishedBatchesAreSoldToCustomers()
    {
        var (state, formulaId) = SetUpTonic(Start("sales"), 30);
        var s = Months(Act(state, new BrewBatch(formulaId, 30)), 1);
        var sold = s.Ledger[0].Sales.Find(x => x.FormulaId == formulaId);
        Assert.NotNull(sold);
        Assert.True(sold!.Bottles > 0);
        Assert.Equal(30 - sold.Bottles, Lab.StockOf(s, formulaId));
        Assert.Equal(sold.Bottles, s.Stats.BottlesSold);
    }

    [Fact]
    public void EndsAtTheTimelineEndDate()
    {
        var s = Start();
        s.Player.CashCents = 10_000_000;
        s.Month.StartCashCents = s.Player.CashCents;
        s = Months(s, 10_000);
        Assert.Equal(GameStatus.Ended, s.Meta.Status);
        Assert.Equal(EndReasons.TimelineEnd, s.Meta.EndReason);
        Assert.Equal(C.Config.EndDate, s.Meta.Date);
        Assert.Equal(264, s.Ledger.Count);
        Assert.Throws<InvalidOperationException>(() => Game.EndTurn(s, C));
    }

    [Fact]
    public void BankruptsAfterTheGracePeriodOfInsolvency()
    {
        var s = Start("broke");
        s.Player.CashCents = 0;
        s.Month.StartCashCents = 0;
        s = Months(s, 10);
        Assert.Equal(EndReasons.Bankrupt, s.Meta.EndReason);
        Assert.Equal(C.Config.BankruptcyGraceMonths, s.Ledger.Count);
        Assert.False(Game.ApplyAction(s, new SetPrice("x", 1), C).Result.Ok);
    }

    [Fact]
    public void FiresHistoricalEventsOnTheirDates()
    {
        var s = Start();
        s.Player.CashCents = 10_000_000;
        s.Month.StartCashCents = s.Player.CashCents;
        s = Months(s, 22); // through October 1886
        Assert.True(s.Timeline.Fired.ContainsKey("antifebrin"));
        Assert.True(s.Player.Flags["era_acetanilide"]);
        Assert.False(s.Timeline.Fired.ContainsKey("poison_squad"));
    }

    [Fact]
    public void GrantsUnlocksWhenTheirRequirementIsMet()
    {
        var s = Start();
        s.Stats.BatchesBrewed = 10;
        s = Months(s, 1);
        Assert.True(s.Unlocks.Unlocked.ContainsKey("percolator"));
        Assert.Contains(new UnlockNews("percolator"), s.Ledger[0].News);
        Assert.Contains("percolate", Machine.Observe(s, C).Processes);
    }

    [Fact]
    public void AutoResolvesUndecidedEventsWithTheirDefaultChoice()
    {
        var s = Start();
        s.Stats.BottlesSold = 10;
        s.Events.Pending.Add(new PendingEvent { InstanceId = "manual", EventId = "grateful_letter", DrawnTurn = 0 });
        s = Months(s, 1);
        Assert.True(s.Player.Flags["testimonial_received"]);
        Assert.Contains(new EventResolvedNews("grateful_letter", "print_testimonial", true), s.Ledger[0].News);
    }

    [Fact]
    public void AppliesAChosenEventOutcome()
    {
        var s0 = Start();
        s0.Events.Pending.Add(new PendingEvent { InstanceId = "m", EventId = "diploma_mill", DrawnTurn = 0 });
        var s = Act(s0, new ResolveEvent("m", "buy_diploma"));
        Assert.True(s.Player.Flags["bought_diploma"]);
        Assert.Equal(5, s.Player.Notoriety);
        Assert.Empty(s.Events.Pending);
    }

    [Fact]
    public void HeatDiesDownWhenNothingHappens()
    {
        var s = Start();
        s.Scrutiny.Heat = 40;
        s.Month.StartHeat = 40;
        s.Player.CashCents = 1_000_000;
        s.Month.StartCashCents = s.Player.CashCents;
        s = Months(s, 6);
        Assert.True(s.Scrutiny.Heat < 30);
    }

    // --- machine API ---------------------------------------------------------------------------

    [Fact]
    public void ListsLegalActionsThatActuallySucceed()
    {
        var (state, formulaId) = SetUpTonic(Start(), 5);
        var brew = Assert.IsType<BrewBatchSpec>(Machine.LegalActions(state, C).Single(x => x is BrewBatchSpec));
        Assert.Equal(formulaId, brew.FormulaId);
        Assert.Equal(5, brew.MaxBottles);
        Assert.True(Game.ApplyAction(state, new BrewBatch(formulaId, brew.MaxBottles), C).Result.Ok);
    }

    [Fact]
    public void OffersNothingOnceTheGameHasEnded()
    {
        var s = Start();
        s.Meta.Status = GameStatus.Ended;
        s.Meta.EndReason = EndReasons.Bankrupt;
        Assert.Empty(Machine.LegalActions(s, C));
    }

    [Fact]
    public void ObservationHidesTruth()
    {
        var s = Months(Start("observe"), 2);
        var o = Machine.Observe(s, C);
        Assert.Equal(s.Customers.Count, o.Customers.Count);
        Assert.All(o.Customers, c => Assert.Contains(c.Looks, new[] { "hale", "peaky", "poorly", "gravely_ill" }));
        Assert.Equal(s.Ledger[^1], o.LastMonth);
        Assert.Equal(3, o.NextUnlocks.Count);
    }

    // --- saves ---------------------------------------------------------------------------------

    [Fact]
    public void SavesRoundTripExactly()
    {
        var (state, _) = SetUpTonic(Start("save"), 4);
        var s = Months(state, 3);
        string json = SaveFile.Serialize(s);
        var loaded = SaveFile.Deserialize(json);
        Assert.Equal(Hash.StableStringify(s), Hash.StableStringify(loaded));
        Assert.Equal(json, SaveFile.Serialize(loaded));
        // Play on from the loaded game: identical to playing on from the original.
        Assert.Equal(Game.HashState(Months(s, 5)), Game.HashState(Months(loaded, 5)));
    }

    [Fact]
    public void SavesLoadTheTypeScriptFormat()
    {
        // A TS checkpoint is the full state; wrap it the way serializeGame does.
        using var doc = Fixtures.ReadJson("runs/briber-parity-1.json.gz");
        string snapshot = doc.RootElement.GetProperty("checkpoints").GetProperty("12").GetString()!;
        var loaded = SaveFile.Deserialize($"{{\"saveVersion\":1,\"state\":{snapshot}}}");
        Assert.Equal(snapshot, Hash.StableStringify(loaded));
        Assert.Equal(doc.RootElement.GetProperty("hashes")[12].GetString(), Game.HashState(loaded));
    }

    [Fact]
    public void SavesRefuseNewerBuildsAndNonSaves()
    {
        Assert.Contains("newer than this build",
            Assert.Throws<InvalidDataException>(() => SaveFile.Deserialize("{\"saveVersion\":2,\"state\":{}}")).Message);
        Assert.Contains("not a Panacea save", Assert.Throws<InvalidDataException>(() => SaveFile.Deserialize("{\"hello\":1}")).Message);
    }

    // --- determinism ---------------------------------------------------------------------------

    /// <summary>A random but reproducible playthrough through the legal-action API.</summary>
    private static GameState Play(string seed, int months, bool checkEveryStep)
    {
        var s = Start(seed);
        var r = Rng.FromSeed($"{seed}:script");
        for (int m = 0; m < months && s.Meta.Status == GameStatus.Running; m++)
        {
            int moves = r.Int(0, 5);
            for (int i = 0; i < moves; i++)
            {
                var specs = Machine.LegalActions(s, C);
                if (specs.Count == 0) break;
                GameAction action = r.Pick(specs) switch
                {
                    BuyIngredientSpec b => new BuyIngredient(b.IngredientId, r.Int(1, Math.Min(b.MaxUnits, 40))),
                    CreateFormulaSpec f => new CreateFormula($"Fuzz {m}",
                        [new(r.Pick(f.IngredientIds), 0.1 + r.Float() * 3), new("water", 1 + r.Float() * 3)],
                        r.Pick(f.ProcessIds), r.Int(10, 300), r.Chance(0.5) ? f.ClaimIds.Take(2).ToList() : null),
                    ReformulateSpec f => new Reformulate(f.FormulaId,
                        [new(r.Pick(f.IngredientIds), 0.1 + r.Float() * 2), new("water", 2)], r.Pick(f.ProcessIds)),
                    SetPriceSpec p => new SetPrice(p.FormulaId, r.Int(10, 300)),
                    SetClaimsSpec c => new SetClaims(c.FormulaId, c.ClaimIds.Where(_ => r.Chance(0.3)).Take(c.MaxClaims).ToList()),
                    SetAdSpendSpec => new SetAdSpend(r.Int(0, 3000)),
                    BrewBatchSpec b => new BrewBatch(b.FormulaId, r.Int(1, b.MaxBottles)),
                    ResolveEventSpec e => new ResolveEvent(e.InstanceId, r.Pick(e.ChoiceIds)),
                    _ => throw new InvalidOperationException(),
                };
                Game.ApplyActionInPlace(s, action, C);
                if (checkEveryStep) AssertHealthy(s);
            }
            Game.EndTurnInPlace(s, C);
            AssertHealthy(s);
        }
        return s;
    }

    [Theory]
    [InlineData("fuzz-a")]
    [InlineData("fuzz-b")]
    [InlineData("")]
    public void RandomPlaythroughsAreDeterministicKeepInvariantsAndReplay(string seed)
    {
        var a = Play(seed, 40, checkEveryStep: true);
        var b = Play(seed, 40, checkEveryStep: false);
        Assert.Equal(Game.HashState(a), Game.HashState(b));
        var replayed = Game.Replay(C, new NewGameOptions { Seed = seed }, a.Log);
        Assert.Equal(Game.HashState(a), Game.HashState(replayed));
    }

    [Fact]
    public void DifferentSeedsDiverge()
    {
        string Run(string seed)
        {
            var s = Start(seed);
            s.Player.CashCents = 1_000_000;
            s.Month.StartCashCents = s.Player.CashCents;
            s.Stats.BottlesSold = 100;
            return Game.HashState(Months(s, 24));
        }
        Assert.NotEqual(Run("one"), Run("two"));
    }

    [Fact]
    public void NonMutatingEndTurnMatchesInPlace()
    {
        var (state, formulaId) = SetUpTonic(Start("pure"), 20);
        state = Act(state, new BrewBatch(formulaId, 20));
        var copy = state.Clone();
        var (next, _) = Game.EndTurn(state, C);
        Game.EndTurnInPlace(copy, C);
        Assert.Equal(Game.HashState(copy), Game.HashState(next));
        Assert.NotEqual(Game.HashState(state), Game.HashState(next));
    }

    // --- balance pass 3 ----------------------------------------------------------------------

    private static DeathRecord Death(double detectability, bool badBatch = false) => new()
    {
        Id = "death_x", Date = new GameDate(1885, 1), Name = "Mr. Test", Segment = "labourer",
        Consumer = "self", Cause = "overdose", ReportedCause = "overdose", ProductImplicated = true, Detectability = detectability,
        BadBatch = badBatch, Inquest = InquestStates.Pending,
    };

    [Fact]
    public void TestimonyHelpsAMildBottleAndDamnsALoadedOne()
    {
        var s = Start("inq");
        Assert.True(Scrutiny.ImplicationChance(s, Death(2), "testify") < Scrutiny.ImplicationChance(s, Death(2), "silent"));
        Assert.True(Scrutiny.ImplicationChance(s, Death(9), "testify") > Scrutiny.ImplicationChance(s, Death(9), "silent"));
        // silence keeps a mis-measured batch out of the record
        Assert.Equal(Scrutiny.ImplicationChance(s, Death(4), "silent"), Scrutiny.ImplicationChance(s, Death(4, true), "silent"));
        Assert.Equal(Scrutiny.ImplicationChance(s, Death(4), "testify") + 0.2, Scrutiny.ImplicationChance(s, Death(4, true), "testify"), 9);
    }

    [Fact]
    public void TheBooksValueStockAtCost()
    {
        var s = Start("books");
        s.Player.CashCents = 100_000;
        s.Month.StartCashCents = s.Player.CashCents;
        Assert.Equal(new StockValue(0, 0, 0), Machine.Observe(s, C).StockValue);
        s = Act(s, new BuyIngredient("morphine", 2), new BuyIngredient("grain_alcohol", 20));
        long spent = 100_000 - s.Player.CashCents;
        Assert.Equal(new StockValue(0, spent, spent), Machine.Observe(s, C).StockValue);
        var (made, created) = Game.ApplyAction(s,
            new CreateFormula("Books", [new("morphine", 0.1), new("grain_alcohol", 2)], "simple_mix", 100), C);
        Assert.True(created.Ok);
        s = Act(made, new BrewBatch(created.CreatedId!, 10));
        Assert.Equal(spent + 10 * C.Config.BottleCostCents, Machine.Observe(s, C).StockValue.TotalCents);
        var (next, report) = Game.EndTurn(s, C);
        Assert.Equal(0, report.StockValueStartCents);
        Assert.Equal(Lab.StockValue(next, C).TotalCents, report.StockValueEndCents);
        Assert.Equal(report.StockValueEndCents, next.Month.StartStockValueCents);
    }

    // --- balance pass 4: investments, inquest replies, names, progress ------------------------

    private static GameState RichShop(string seed = "inv")
    {
        var s = Start(seed);
        s.Player.CashCents = 500_000;
        s.Month.StartCashCents = s.Player.CashCents;
        foreach (var id in new[] { "newspaper_ads", "larger_shop", "percolator" }) s.Unlocks.Unlocked[id] = 0;
        s.Stats.BottlesSold = 5000;
        return s;
    }

    [Fact]
    public void InvestmentsAreOfferedWhenTheirRequirementHoldsAndOnlyOnceWhileHeld()
    {
        Assert.Equal("not yet offered", Game.ApplyAction(Start("inv0"), new Invest("branch_shop"), C).Result.Error);
        var s = RichShop();
        var spec = Assert.IsType<InvestSpec>(Machine.LegalActions(s, C).Single(x => x is InvestSpec));
        Assert.Equal(new[] { "branch_shop", "almanac", "druggists_order", "wholesale_contract", "laboratory" }, spec.InvestmentIds);
        long before = s.Player.CashCents;
        int capacity = Machine.Observe(s, C).CustomerCapacity;
        s = Act(s, new Invest("branch_shop"));
        Assert.Equal(C.ById.Investments["branch_shop"].CostCents, before - s.Player.CashCents);
        Assert.Equal(capacity + 20, Machine.Observe(s, C).CustomerCapacity);
        Assert.Equal("already held", Game.ApplyAction(s, new Invest("branch_shop"), C).Result.Error);
        var report = Game.EndTurnInPlace(s, C);
        Assert.Contains(report.Deltas, d => d.Cause == Causes.Upkeep && d.Detail == "branch_shop" &&
            d.Amount == -C.ById.Investments["branch_shop"].UpkeepCents);
    }

    [Fact]
    public void AnAlmanacRunsTwelveMonthsThenLapses()
    {
        var (s, formulaId) = SetUpTonic(RichShop("alm"), 400);
        s = Act(s, new BrewBatch(formulaId, 400), new Invest("almanac"));
        var first = Game.EndTurnInPlace(s, C);
        Assert.Equal(10, first.Callers["almanac"]);
        s = Months(s, 10);
        Assert.Equal(1, Machine.Observe(s, C).Investments.Single(i => i.Id == "almanac").MonthsLeft);
        var last = Game.EndTurnInPlace(s, C);
        Assert.Contains(last.News, n => n is InvestmentNews { Id: "almanac", Change: InvestmentChanges.Lapsed });
        Assert.False(s.Player.Investments.ContainsKey("almanac"));
    }

    [Fact]
    public void DruggistsTakeTheirOrderAtTheWholesalePriceAndCancelWhenTheTalkTurns()
    {
        var (s, formulaId) = SetUpTonic(RichShop("whl"), 300);
        s = Act(s, new SetPrice(formulaId, 100), new BrewBatch(formulaId, 300), new Invest("wholesale_contract"));
        var w = C.ById.Investments["wholesale_contract"].Wholesale!;
        double factor = Math.Max(0, Math.Min(w.MaxOrderBoost, 1 + w.PriceElasticity * (w.RefPriceCents - 100) / w.RefPriceCents));
        int order = (int)Math.Floor(w.BottlesPerMonth * (1 + s.Player.Reputation * w.PerReputation) * factor);
        Assert.True(order > 0);
        int price = (int)Math.Min(w.MaxPriceCents, JsMath.Round(100 * w.PricePct / 100));
        var report = Game.EndTurnInPlace(s, C);
        Assert.Equal(order, report.Wholesale.Ordered);
        Assert.Equal(order, report.Wholesale.Delivered);
        Assert.Equal(order * price, report.Wholesale.RevenueCents);
        s.Scrutiny.Heat = 80;
        var hot = Game.EndTurnInPlace(s, C);
        Assert.Contains(hot.News, n => n is InvestmentNews { Id: "wholesale_contract", Change: InvestmentChanges.Lost });
    }

    // --- balance pass 5: orders from away, the post, progress --------------------------------

    [Fact]
    public void DruggistsOrderFewerTheDearerTheBottleAndNoneWhenFarTooDear()
    {
        var w = C.ById.Investments["wholesale_contract"].Wholesale!;
        var s = Start("elastic");
        Assert.Equal(1, Investments.OrderPriceFactor(w, w.RefPriceCents));
        Assert.Equal(1, Investments.OrderPriceFactor(w, null));
        Assert.Equal(w.MaxOrderBoost, Investments.OrderPriceFactor(w, 10));
        Assert.Equal(0, Investments.OrderPriceFactor(w, 300));
        long At(int cents) => (long)Investments.WholesaleOrder(s, w, cents) * Investments.WholesalePrice(w, cents);
        Assert.True(At(70) > At(100));
        Assert.True(Investments.WholesaleOrder(s, w, 90) < Investments.WholesaleOrder(s, w, 60));
    }

    [Fact]
    public void OrdersFromAwayTakeAtMostTheirShareOfWhatTheRegularsLeft()
    {
        var (s, formulaId) = SetUpTonic(RichShop("share"), 10);
        s.Customers.Clear();
        s = Act(s, new BrewBatch(formulaId, 10), new Invest("wholesale_contract"), new Invest("druggists_order"), new Invest("almanac"));
        var report = Game.EndTurnInPlace(s, C);
        Assert.Equal((int)Math.Ceiling(10 * C.Config.Market.OrdersShelfShare), report.Wholesale.Delivered + report.Post.Delivered);
        Assert.True(report.Wholesale.Ordered > report.Wholesale.Delivered);
    }

    [Fact]
    public void AnAlmanacsReadersSendForBottlesByPostAtTheShelfPrice()
    {
        var (s, formulaId) = SetUpTonic(RichShop("post"), 300);
        s = Act(s, new SetPrice(formulaId, 70), new BrewBatch(formulaId, 300), new Invest("almanac"));
        var spec = C.ById.Investments["almanac"].PostOrders!;
        int order = Investments.WholesaleOrder(s, spec, 70);
        Assert.True(order > 0);
        var seen = Machine.Observe(s, C).Investments.Single(i => i.Id == "almanac");
        Assert.Equal(order, seen.PostBottles);
        Assert.Equal(70, seen.PostPriceCents);
        Assert.Null(seen.WholesaleBottles);
        var report = Game.EndTurnInPlace(s, C);
        Assert.Equal(order, report.Post.Ordered);
        Assert.Equal(order, report.Post.Delivered);
        Assert.Equal(order * 70, report.Post.RevenueCents);
        Assert.Equal(0, report.Wholesale.Ordered);
        Assert.Contains(report.Deltas, d => d.Cause == Causes.SalesPost && d.Detail == "almanac" && d.Amount == order * 70);
    }

    [Fact]
    public void TheProspectusShowsHowNearEachVentureIsAsNumbers()
    {
        var s = Start("prospectus");
        s.Stats.BottlesSold = 1120;
        s.Scrutiny.Heat = 12;
        var o = Machine.Observe(s, C);
        var whl = C.ById.Investments["wholesale_contract"];
        double need = ((AtLeastRequirement)((AllRequirement)whl.Requires!).Of[0]).AtLeast;
        Assert.Equal(new[]
        {
            new ConditionProgress("bottlesSold", null, "atLeast", 1120, need, false, null),
            new ConditionProgress("heat", null, "below", 12, 30, true, null),
            new ConditionProgress("cost", null, "atLeast", s.Player.CashCents, whl.CostCents, false, null),
        }, o.Investments.Single(i => i.Id == "wholesale_contract").Progress);
        var branch = o.Investments.Single(i => i.Id == "branch_shop").Progress;
        Assert.Equal(new ConditionProgress("unlocked", "larger_shop", "atLeast", 0, 1, false, null), branch[0]);
        // the wall comes down only when the shop next door is taken up (a venture of its own)
        Assert.Equal(new[] { new ConditionProgress("flag", "knocked_through", "atLeast", 0, 1, false, "larger_shop") },
            branch.Where(c => c.Via == "larger_shop"));
        Assert.Equal("cost", branch[^1].Kind);
        var rich = Act(RichShop("held"), new Invest("branch_shop"));
        Assert.Empty(Machine.Observe(rich, C).Investments.Single(i => i.Id == "branch_shop").Progress);
    }

    [Fact]
    public void TestimonyAndSilenceDifferByAtLeastATenthInEveryState()
    {
        var s = Start("odds");
        foreach (bool implicated in new[] { true, false })
        for (double det = 0; det <= 20; det += 0.5)
        foreach (double heat in new double[] { 0, 35, 75, 100 })
        foreach (double rep in new double[] { 0, 15, 60 })
        {
            s.Scrutiny.Heat = heat;
            s.Player.Reputation = rep;
            var d = new DeathRecord
            {
                Id = "d", Date = s.Meta.Date, Name = "", Segment = "labourer", Consumer = Consumers.Self,
                Cause = DeathCauses.Natural, ReportedCause = ReportedCauses.Natural, Inquest = InquestStates.Pending, ProductImplicated = implicated,
                Detectability = det, Boldness = 5,
            };
            double t = Scrutiny.ImplicationChance(s, d, "testify");
            double q = Scrutiny.ImplicationChance(s, d, "silent");
            Assert.True(Math.Abs(t - q) >= 0.1 - 1e-9, $"det {det} heat {heat}: {t} vs {q}");
            Assert.True(det <= 5 ? t < q : q < t);
        }
    }

    [Fact]
    public void NoTwoCustomersShareANameAndTheOpeningCastVariesBySeed()
    {
        var (s, formulaId) = SetUpTonic(RichShop("names"), 2000);
        s = Months(Act(s, new BrewBatch(formulaId, 2000)), 60);
        Assert.Equal(s.Meta.UsedNames.Count, s.Meta.UsedNames.Distinct().Count());
        Assert.NotEqual(Start("cast-a").Customers.Select(c => c.Name), Start("cast-b").Customers.Select(c => c.Name));
    }

    [Fact]
    public void AnUnlockThatNeedsEverythingIsOnlyAsNearAsItsLeastMetPart()
    {
        var s = Start("progress");
        s.Stats.BottlesSold = 330;
        s.Unlocks.Unlocked["second_still"] = 0;
        Assert.Equal(0.55, Requirements.Evaluate(C.ById.Unlocks["third_still"].Requirement, s, C).Progress, 9);
        s.Stats.BatchesBrewed = 1;
        s.Player.CashCents = 50_000;
        Assert.Equal(1.0 / 40, Requirements.Evaluate(C.ById.Unlocks["quality_control"].Requirement, s, C).Progress, 9);
    }

    // --- balance pass 6: the stills, the shop next door, the coroner's fee --------------------

    [Fact]
    public void AStillMakesAtMostACoppersWorthABatchAndALargerCopperMakesEveryStillBigger()
    {
        int copper = C.Config.StillBottles;
        var s = Start("copper");
        s.Player.CashCents = 500_000;
        s.Month.StartCashCents = s.Player.CashCents;
        var (state, formulaId) = SetUpTonic(s, copper); // no larger copper needed for this one
        foreach (var c in Tonic) state = Act(state, new BuyIngredient(c.IngredientId, (int)Math.Ceiling(c.Amount * 200)));
        var o = Machine.Observe(state, C);
        Assert.Equal((1, copper), (o.Stills, o.StillBottles));
        Assert.Equal(copper, Assert.IsType<BrewBatchSpec>(Machine.LegalActions(state, C).Single(x => x is BrewBatchSpec)).MaxBottles);
        var over = Game.ApplyAction(state, new BrewBatch(formulaId, copper + 1), C).Result;
        Assert.Equal($"a still makes at most {copper} bottles a batch", over.Error);

        state.Unlocks.Unlocked["second_still"] = 0;
        state.Player.Capabilities["stills"] = 2;
        var after = Act(state, new Invest("larger_copper"));
        int bigger = copper + C.ById.Investments["larger_copper"].Effects.OfType<CapabilityEffect>().Single().Delta;
        Assert.Equal(bigger, Machine.Observe(after, C).StillBottles);
        var both = Act(after, new BrewBatch(formulaId, bigger), new BrewBatch(formulaId, bigger - 1));
        Assert.Equal(new[] { bigger, bigger - 1 }, both.Lab.Batches.Select(b => b.Bottles));
    }

    [Fact]
    public void TheStillCardCanSayWhatTheNextStillWaitsFor()
    {
        var s = Start("next");
        s.Stats.BottlesSold = 150;
        var steps = Machine.Observe(s, C).NextCapacity;
        Assert.Equal(new[] { ("second_still", "unlock", 1, 0), ("third_still", "unlock", 1, 0), ("larger_copper", "investment", 0, 24), ("fourth_still", "investment", 1, 0) },
            steps.Select(c => (c.Id, c.Source, c.Stills, c.StillBottles)));
        Assert.Equal(new[] { new ConditionProgress("bottlesSold", null, "atLeast", 150, 200, false, null) }, steps[0].Progress);
        Assert.Equal(new[] { ("unlocked", (string?)null), ("bottlesSold", "second_still"), ("bottlesSold", null) },
            steps[1].Progress.Select(c => (c.Kind, c.Via)));
        Assert.Equal(("cost", 20_000.0), (steps[2].Progress[^1].Kind, steps[2].Progress[^1].Need));

        s.Unlocks.Unlocked["second_still"] = 0;
        s.Stats.BottlesSold = 450;
        var third = Machine.Observe(s, C).NextCapacity[0];
        Assert.Equal("third_still", third.Id);
        Assert.Equal(new[]
        {
            new ConditionProgress("unlocked", "second_still", "atLeast", 1, 1, true, null),
            new ConditionProgress("bottlesSold", null, "atLeast", 450, 600, false, null),
        }, third.Progress);
        s.Unlocks.Unlocked["third_still"] = 0;
        Assert.Equal(new[] { "larger_copper", "fourth_still" }, Machine.Observe(s, C).NextCapacity.Select(c => c.Id));
    }

    [Fact]
    public void AFormulasUnitCostIsWhatAnOrderChargesTodayAfterTheMarketMoves()
    {
        var s = Start("prices");
        s.Player.CashCents = 100_000;
        s.Month.StartCashCents = s.Player.CashCents;
        var created = Act(s, new CreateFormula("T", Tonic, "simple_mix", 60));
        double before = Machine.Observe(created, C).Formulas[0].UnitCostCents;
        var dearer = created.Clone();
        dearer.Player.Capabilities["surcharge_pct_grain_alcohol"] = 100;
        dearer.Player.Capabilities["ingredient_discount_pct"] = 15;
        double now = Machine.Observe(dearer, C).Formulas[0].UnitCostCents;
        Assert.NotEqual(before, now);
        double expected = C.Config.BottleCostCents;
        foreach (var c in Tonic) expected += c.Amount * Lab.IngredientUnitCost(dearer, c.IngredientId, C);
        Assert.Equal(expected, now, 9);
        Assert.Equal(Lab.FormulaUnitCost(dearer, Tonic, "simple_mix", C), now, 9);
    }

    [Fact]
    public void TheBooksRecordWhatWasLeftOnTheShelfAndObserveCountsTheBareMonths()
    {
        var (s, formulaId) = SetUpTonic(Start("bare"), 10);
        var (sold, report) = Game.EndTurn(Act(s, new BrewBatch(formulaId, 10)), C);
        Assert.Equal(Lab.TotalStock(sold), report.ShelfEndBottles);
        sold = Months(sold, 3);
        Assert.All(sold.Ledger.TakeLast(3), e => Assert.Equal(0, e.ShelfEndBottles));
        Assert.Equal(3, Machine.Observe(sold, C).ShelfBareMonths);
    }

    [Fact]
    public void TheShopNextDoorIsKnockedThroughOnlyWhenTakenUp()
    {
        var s = Start("nextdoor");
        s.Player.CashCents = 100_000;
        s.Month.StartCashCents = s.Player.CashCents;
        var json = StateJson.Serialize(s.Customers[0]);
        while (s.Customers.Count < 30)
        {
            var c = StateJson.Deserialize<Customer>(json);
            c.Id = $"c{s.Customers.Count}";
            s.Customers.Add(c);
        }
        var (left, report) = Game.EndTurn(s, C);
        Assert.False(left.Unlocks.Unlocked.ContainsKey("larger_shop"));
        Assert.Equal(-C.Config.RentCents, report.Deltas.Where(d => d.Cause == Causes.Rent).Sum(d => d.Amount));
        var offer = Machine.Observe(left, C).Investments.Single(i => i.Id == "shop_next_door");
        Assert.True(offer.CanBuy);
        Assert.Equal(300, offer.UpkeepCents);
        var taken = Act(left, new Invest("shop_next_door"));
        var (state, after) = Game.EndTurn(taken, C);
        Assert.Equal(taken.Meta.Turn, state.Unlocks.Unlocked["larger_shop"]);
        Assert.Contains(after.Deltas, d => d.Cause == Causes.Upkeep && d.Amount == -300 && d.Detail == "shop_next_door");
        Assert.Equal(Machine.Observe(left, C).CustomerCapacity + 20, Machine.Observe(state, C).CustomerCapacity);
        Assert.NotEqual("not yet offered", Machine.Observe(state, C).Investments.Single(i => i.Id == "branch_shop").Refusal);
    }

    [Fact]
    public void TheCoronersFeeRisesWithEveryEnvelopeEvenWhenTheTalkHasDiedDown()
    {
        var state = Start("fee");
        state.Player.CashCents = 100_000;
        state.Month.StartCashCents = state.Player.CashCents;
        state.Unlocks.Unlocked["coroner_fees"] = 0;
        state.Scrutiny.Heat = 100;
        state.Month.StartHeat = 100;
        var fees = new List<long>();
        for (int i = 0; i < 3; i++)
        {
            state = state.Clone();
            state.Deaths.Add(new DeathRecord
            {
                Id = $"death_{i}", Date = state.Meta.Date, Name = "Mr. Test", Segment = "labourer", Consumer = Consumers.Self,
                Cause = DeathCauses.Overdose, ReportedCause = ReportedCauses.Overdose, ProductImplicated = true, Detectability = 4, Inquest = InquestStates.Pending,
            });
            state.Events.Pending.Add(new PendingEvent { InstanceId = $"inq_{i}", EventId = "inquest", DrawnTurn = 0, DeathId = $"death_{i}" });
            long cost = Scrutiny.BribeCostCents(state, C);
            Assert.Equal(cost, Machine.Observe(state, C).PendingEvents[^1].BribeCostCents);
            fees.Add(cost);
            state = Act(state, new ResolveEvent($"inq_{i}", "bribe_coroner"));
            Assert.Equal(cost, state.Scrutiny.LastBribeCents);
            double cooled = Math.Max(0, state.Scrutiny.Heat - 30);
            state.Month.StartHeat -= state.Scrutiny.Heat - cooled;
            state.Scrutiny.Heat = cooled;
        }
        long rise = C.Config.Scrutiny.BribeMinRiseCents;
        Assert.True(fees[1] >= fees[0] + rise, $"{fees[0]} then {fees[1]}");
        Assert.True(fees[2] >= fees[1] + rise, $"{fees[1]} then {fees[2]}");
    }
}
