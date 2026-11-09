using Xunit;

namespace Panacea.Sim.Tests;

/// <summary>Events pass 1 (packages/sim/test/letters.test.ts): a fair draw, the ward's memory, a letter's callers.</summary>
public class LettersTests
{
    private static readonly ContentPack C = Fixtures.Content;

    private static readonly FormulaComponent[] Tonic =
        [new("grain_alcohol", 2), new("laudanum", 0.25), new("sugar_syrup", 1), new("water", 4.75)];

    /// <summary><paramref name="n"/> random letters that could all come this month.</summary>
    private static List<GameEvent> Letters(int n, double chance = 1) => Enumerable.Range(0, n).Select(i => new GameEvent
    {
        Id = $"letter_{i}",
        Condition = new DateRequirement(new GameDate(1885, 1)),
        Chance = chance,
        Choices = [new EventChoice { Id = "ok", Default = true }],
    }).ToList();

    private static ContentPack WithEvents(List<GameEvent> events) => new()
    {
        Config = C.Config, Ingredients = C.Ingredients, Processes = C.Processes, Unlocks = C.Unlocks, Events = events,
        Timeline = C.Timeline, Regulars = C.Regulars, Backgrounds = C.Backgrounds, Investments = C.Investments,
        Segments = C.Segments, Ailments = C.Ailments, Claims = C.Claims, Names = C.Names, Strings = C.Strings,
        ById = new ContentById
        {
            Ingredients = C.ById.Ingredients, Processes = C.ById.Processes, Unlocks = C.ById.Unlocks,
            Events = events.ToDictionary(e => e.Id), Backgrounds = C.ById.Backgrounds, Investments = C.ById.Investments,
            Segments = C.ById.Segments, Ailments = C.ById.Ailments, Claims = C.ById.Claims,
        },
    };

    private static GameState Fresh(string seed)
    {
        var s = Game.NewGame(C, new NewGameOptions { Seed = seed });
        s.Events = new EventsState();
        return s;
    }

    [Fact]
    public void ThePostDrawsNoMoreThanTheMonthAllowsAndEveryLetterHasAFairTurn()
    {
        var pack = WithEvents(Letters(20));
        var counts = new Dictionary<string, int>();
        for (int i = 0; i < 400; i++)
        {
            var s = Fresh($"fair-{i}");
            Progression.DrawEvents(s, pack, Rng.FromSeed($"fair-{i}"));
            Assert.Equal(pack.Config.MaxEventsPerMonth, s.Events.Pending.Count);
            foreach (var p in s.Events.Pending) counts[p.EventId] = counts.GetValueOrDefault(p.EventId) + 1;
        }
        Assert.Equal(20, counts.Count);
        Assert.All(counts.Values, n => Assert.InRange(n, 21, 64));
    }

    [Fact]
    public void ThePostLeavesOutALetterWaitingSpentCoolingDownOrNotYetDue()
    {
        var l = Letters(5);
        var pack = WithEvents([
            l[0],
            new GameEvent { Id = l[1].Id, Condition = l[1].Condition, Chance = 1, Once = true, Choices = l[1].Choices },
            new GameEvent { Id = l[2].Id, Condition = l[2].Condition, Chance = 1, CooldownMonths = 12, Choices = l[2].Choices },
            new GameEvent { Id = l[3].Id, Condition = new DateRequirement(new GameDate(1900, 1)), Chance = 1, Choices = l[3].Choices },
            l[4],
        ]);
        var s = Fresh("rules");
        s.Events.Pending.Add(new PendingEvent { InstanceId = "x", EventId = "letter_0", DrawnTurn = 0 });
        s.Events.TimesDrawn["letter_1"] = 1;
        s.Events.LastDrawn["letter_2"] = s.Meta.Turn - 3;
        Progression.DrawEvents(s, pack, Rng.FromSeed("rules"));
        Assert.Equal(["letter_0", "letter_4"], s.Events.Pending.Select(p => p.EventId).ToArray());
        Assert.Equal(s.Meta.Turn, s.Events.LastDrawn["letter_4"]);
        Assert.Equal(1, s.Events.TimesDrawn["letter_4"]);
    }

    [Fact]
    public void ThePostDrawsEachLetterWithItsOwnChance()
    {
        var pack = WithEvents(Letters(1, 0.25));
        int drawn = 0;
        for (int i = 0; i < 2000; i++)
        {
            var s = Fresh($"chance-{i}");
            Progression.DrawEvents(s, pack, Rng.FromSeed($"chance-{i}"));
            drawn += s.Events.Pending.Count;
        }
        Assert.InRange(drawn / 2000.0, 0.22, 0.28);
    }

    [Fact]
    public void ALetterNudgesTheWardMemoryWithinItsBoundsAndRaisesItsPeak()
    {
        var s = Fresh("memory");
        Effects.Apply(s, [new WardMemoryEffect(30)], "event");
        Assert.Equal(30, s.Ward.Memory);
        Assert.Equal(30, s.Ward.PeakMemory);
        Effects.Apply(s, [new WardMemoryEffect(-50)], "event");
        Assert.Equal(0, s.Ward.Memory);
        Assert.Equal(30, s.Ward.PeakMemory);
        Effects.Apply(s, [new WardMemoryEffect(500)], "event");
        Assert.Equal(100, s.Ward.Memory);
        Assert.Equal(100, s.Ward.PeakMemory);
    }

    [Fact]
    public void ALettersCallersComeToTheCounterUnderTheirOwnName()
    {
        var s = Fresh("callers");
        s.Player.CashCents = 100_000;
        s.Month.StartCashCents = s.Player.CashCents;
        Effects.Apply(s, [new CapabilityEffect("callers_wheelmen", 6, 12)], "event");
        Effects.Apply(s, [new CapabilityEffect("callers_bazaar", 4)], "event");
        Effects.Apply(s, [new CapabilityEffect("callers_fountain", 0)], "event");
        Assert.Equal([("bazaar", 4), ("wheelmen", 6)], Market.LetterCallers(s).ToArray());

        var (made, r) = Game.ApplyAction(s, new CreateFormula("T", Tonic, "simple_mix", 60), C);
        Assert.True(r.Ok);
        s = made;
        foreach (var c in Tonic) s = Game.ApplyAction(s, new BuyIngredient(c.IngredientId, (int)Math.Ceiling(c.Amount * 40)), C).State;
        s.Player.Capabilities["still_bottles"] = 40 - C.Config.StillBottles > 0 ? 40 - C.Config.StillBottles : 0;
        var (brewed, br) = Game.ApplyAction(s, new BrewBatch(s.Lab.Formulas[0].Id, 40), C);
        Assert.True(br.Ok, br.Error);
        var ctx = new TurnContext { State = brewed, Content = C, Rng = Rng.FromSeed("callers") };
        Lab.ProductionPhase.Run(ctx);
        Market.SalesPhase.Run(ctx);
        Assert.Equal(6, brewed.Month.Callers["wheelmen"]);
        Assert.Equal(4, brewed.Month.Callers["bazaar"]);
        Assert.False(brewed.Month.Callers.ContainsKey("fountain"));
    }
}
