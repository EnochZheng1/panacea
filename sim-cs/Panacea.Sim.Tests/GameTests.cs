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

    private static (GameState State, string FormulaId) SetUpTonic(GameState state, int bottles)
    {
        var (s, result) = Game.ApplyAction(state, new CreateFormula("Test Tonic", Tonic, "simple_mix", 50), C);
        Assert.True(result.Ok);
        foreach (var c in Tonic) s = Act(s, new BuyIngredient(c.IngredientId, (int)Math.Ceiling(c.Amount * bottles)));
        return (s, result.CreatedId!);
    }

    // --- content -----------------------------------------------------------------------------

    [Fact]
    public void EmbeddedContentLoads()
    {
        Assert.Equal("0.2.0", C.Config.ContentVersion);
        Assert.Equal(17, C.Ingredients.Count);
        Assert.Equal(3, C.Processes.Count);
        Assert.Equal(9, C.Unlocks.Count);
        Assert.Equal(8, C.Events.Count);
        Assert.Equal(8, C.Timeline.Count);
        Assert.Equal(12, C.Regulars.Count);
        Assert.Equal(160, C.Strings.Count);
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
        s = Months(s, 13);
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
}
