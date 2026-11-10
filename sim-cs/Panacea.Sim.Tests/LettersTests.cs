using Xunit;

namespace Panacea.Sim.Tests;

/// <summary>Events passes 1 and 2 and the round-8 letters fix (packages/sim/test/letters.test.ts): a fair draw, seasons, follow-ups, the shop's age and remedy, the ward's memory, a letter's callers.</summary>
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
    public void ThePostBringsASeasonalLetterOnlyInItsMonths()
    {
        var a = Letters(1)[0];
        var season = new SeasonRequirement([12, 1]);
        var pack = WithEvents([new GameEvent { Id = a.Id, Condition = season, Chance = 1, Choices = a.Choices }]);
        var s = Fresh("season");
        var months = new List<int>();
        for (int m = 1; m <= 12; m++)
        {
            s.Events.Pending.Clear();
            s.Meta.Date = new GameDate(1890, m);
            Assert.Equal(m is 12 or 1, Requirements.Evaluate(season, s, pack).Met);
            Progression.DrawEvents(s, pack, Rng.FromSeed($"season-{m}"));
            if (s.Events.Pending.Count > 0) months.Add(m);
        }
        Assert.Equal([1, 12], months.ToArray());
    }

    [Fact]
    public void ALetterWaitsForTheShopsAgeAndForTheLetterItFollows()
    {
        var s = Fresh("months");
        s.Meta.Turn = 11;
        var age = new AtLeastRequirement("months", 12);
        Assert.Equal(new RequirementStatus(false, 11.0 / 12), Requirements.Evaluate(age, s, C));
        s.Meta.Turn = 12;
        Assert.True(Requirements.Evaluate(age, s, C).Met);

        var since = new SinceRequirement("burglary", 2);
        Assert.False(Requirements.Evaluate(since, s, C).Met); // never came
        s.Events.LastDrawn["burglary"] = 11;
        Assert.False(Requirements.Evaluate(since, s, C).Met); // a month ago
        s.Events.LastDrawn["burglary"] = 10;
        Assert.True(Requirements.Evaluate(since, s, C).Met);
        Assert.True(Progression.WaitsOnLetter(new AllRequirement([new FlagRequirement("x"), since])));
        Assert.False(Progression.WaitsOnLetter(new NotRequirement(since)));
        Assert.False(Progression.WaitsOnLetter(new FlagRequirement("x")));
    }

    [Fact]
    public void ThePostDrawsAFollowUpBeforeFreshLetters()
    {
        var many = Letters(12);
        var follow = new GameEvent { Id = "follow_up", Condition = new SinceRequirement("letter_0", 0), Chance = 1, Choices = many[0].Choices };
        var pack = WithEvents([.. many, follow]);
        for (int i = 0; i < 50; i++)
        {
            var s = Fresh($"follow-{i}");
            s.Events.LastDrawn["letter_0"] = -5;
            s.Events.TimesDrawn["letter_0"] = 1;
            Progression.DrawEvents(s, pack, Rng.FromSeed($"follow-{i}"));
            Assert.Contains(s.Events.Pending, p => p.EventId == "follow_up");
        }
    }

    [Fact]
    public void ALetterKnowsTheShopsRemedy()
    {
        var s = Fresh("remedy");
        var habit = new RemedyHabitRequirement(1);
        var spirit = new RemedyDrugRequirement("alcohol", 2);
        var opiate = new RemedyDrugRequirement("opiate", 0.01);
        var label = new RemedyClaimRequirement("soothes_nerves");
        Assert.Null(Requirements.ShopRemedy(s));
        foreach (var r in new Requirement[] { habit, spirit, opiate, label }) Assert.False(Requirements.Evaluate(r, s, C).Met);
        s.Lab.Formulas.Add(new Formula
        {
            Id = "plain", Name = "Plain", ProcessId = "simple_mix", Claims = ["soothes_nerves"],
            Components = [new("grain_alcohol", 1), new("valerian", 1), new("water", 6)],
        });
        s.Lab.Formulas.Add(new Formula { Id = "strong", Name = "Strong", ProcessId = "simple_mix", Components = [.. Tonic] });
        // nothing sold yet: the newest in the formula book
        Assert.Equal("strong", Requirements.ShopRemedy(s)!.Id);
        Assert.True(Requirements.Evaluate(habit, s, C).Met);
        Assert.True(Requirements.Evaluate(spirit, s, C).Met);
        Assert.True(Requirements.Evaluate(opiate, s, C).Met);
        Assert.False(Requirements.Evaluate(label, s, C).Met);
        // the bottle the shop sold most of last month
        s.Ledger.Add(new LedgerEntry
        {
            Date = s.Meta.Date,
            Sales = [new SaleRecord { FormulaId = "strong", Bottles = 3 }, new SaleRecord { FormulaId = "plain", Bottles = 9 }],
        });
        Assert.Equal("plain", Requirements.ShopRemedy(s)!.Id);
        Assert.False(Requirements.Evaluate(habit, s, C).Met);
        Assert.False(Requirements.Evaluate(spirit, s, C).Met);
        Assert.False(Requirements.Evaluate(opiate, s, C).Met);
        Assert.True(Requirements.Evaluate(label, s, C).Met);
    }

    [Fact]
    public void ALettersCallersAreFewerWhenTheWardRemembersOrTheBoardSuspects()
    {
        var s = Fresh("trust");
        s.Scrutiny.Heat = 0;
        s.Ward.Memory = 0;
        Effects.Apply(s, [new CapabilityEffect("callers_wheelmen", 6, null)], "event");
        Effects.Apply(s, [new CapabilityEffect("callers_bazaar", 4)], "event");
        Effects.Apply(s, [new CapabilityEffect("callers_portrait", 1)], "event");
        Assert.Equal(1, Market.LetterTrust(s, C));
        // the ward remembers: half the word is given
        s.Ward.Memory = 50;
        Assert.Equal(0.5, Market.LetterTrust(s, C), 9);
        Assert.Equal([("bazaar", 2), ("portrait", 1), ("wheelmen", 3)], Market.LetterCallers(s, C).ToArray());
        // and the Board suspects it: at a fifth of the way to shutting it, 1 − 1.5 × 0.2 of that
        Assert.Equal(1.5, C.Config.Scrutiny.LetterCallerLoss);
        s.Scrutiny.Heat = 20;
        Assert.Equal(0.35, Market.LetterTrust(s, C), 9);
        Assert.Equal([("bazaar", 1), ("wheelmen", 2)], Market.LetterCallers(s, C).ToArray());
        // a ward that remembers everything sends nobody on a letter's word
        s.Scrutiny.Heat = 0;
        s.Ward.Memory = 100;
        Assert.Empty(Market.LetterCallers(s, C));
        // nor for a shop two-thirds of the way to being shut
        s.Ward.Memory = 0;
        s.Scrutiny.Heat = 70;
        Assert.Equal(0, Market.LetterTrust(s, C));
        Assert.Empty(Market.LetterCallers(s, C));
    }

    [Fact]
    public void TheStandingAReplyEarnsIsCreditedAsFarAsTheWardTrustsTheShopAndTakenInFull()
    {
        var s = Fresh("credit");
        var letter = C.ById.Events["parish_bazaar"];
        var inquest = C.ById.Events["inquest"];
        Effect[] effects = [new MetricEffect(Metrics.Reputation, 2), new MetricEffect(Metrics.Reputation, -1), new CashEffect(-300)];
        s.Scrutiny.Heat = 0;
        s.Ward.Memory = 0;
        Assert.Equal(effects, Progression.CreditedEffects(s, C, letter, effects).ToArray());
        s.Scrutiny.Heat = 20;
        s.Ward.Memory = 50;
        Assert.Equal(0.35, Market.LetterTrust(s, C), 9);
        Assert.Equal(
            [new MetricEffect(Metrics.Reputation, 2 * Market.LetterTrust(s, C)), new MetricEffect(Metrics.Reputation, -1), new CashEffect(-300)],
            Progression.CreditedEffects(s, C, letter, effects).ToArray());
        // the coroner's notice is not a letter of the ward's
        Assert.Equal(effects, Progression.CreditedEffects(s, C, inquest, effects).ToArray());
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
        s.Scrutiny.Heat = 0;
        Assert.Equal([("bazaar", 4), ("wheelmen", 6)], Market.LetterCallers(s, C).ToArray());

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

    // ---- the round-9 letters fix: correspondents who remember ----

    /// <summary>A shop in its second summer whose <paramref name="remedies"/> bottles, each at <paramref name="priceCents"/>, all sold last month.</summary>
    private static GameState Shop(string seed, int priceCents, int remedies = 1)
    {
        var s = Fresh(seed);
        for (int i = 0; i < remedies; i++)
            s.Lab.Formulas.Add(new Formula { Id = $"r{i}", Name = $"Remedy {i}", ProcessId = "simple_mix", PriceCents = priceCents, Components = [.. Tonic] });
        s.Ledger.Add(new LedgerEntry
        {
            Date = s.Meta.Date,
            Sales = s.Lab.Formulas.Select((f, i) => new SaleRecord { FormulaId = f.Id, Bottles = 9 - i }).ToList(),
        });
        s.Stats.BottlesSold = 700;
        s.Stats.BatchesBrewed = 30;
        s.Meta.Turn = 17;
        s.Meta.Date = new GameDate(1886, 6);
        s.Player.CashCents = 100_000;
        s.Scrutiny.Heat = 0;
        s.Ward.Memory = 0;
        return s;
    }

    private static bool Could(GameState s, string id) => Requirements.Evaluate(C.ById.Events[id].Condition!, s, C).Met;

    /// <summary>Answers the letter as if it had come <paramref name="ago"/> months back.</summary>
    private static void Reply(GameState s, string eventId, string choiceId, int ago = 2)
    {
        s.Events.Pending.Add(new PendingEvent { InstanceId = $"r_{eventId}", EventId = eventId, DrawnTurn = s.Meta.Turn });
        Assert.Null(Progression.ResolveEvent(s, C, Rng.FromSeed(eventId), $"r_{eventId}", choiceId, false));
        s.Events.LastDrawn[eventId] = s.Meta.Turn - ago;
        s.Events.TimesDrawn[eventId] = 1;
    }

    [Fact]
    public void FeeneyRemembersHowHisOpeningWasTakenAndOnlyUndersellsADearerBottle()
    {
        var slandered = Shop("feeney-slander", 75);
        Reply(slandered, "rival_shop_opens", "spread_rumour");
        Assert.True(Could(slandered, "rival_price_war_slander"));
        Assert.False(Could(slandered, "rival_price_war"));
        Assert.True(Could(slandered, "feeney_boy_slander"));
        Assert.False(Could(slandered, "feeney_boy"));

        var ignored = Shop("feeney-ignored", 75);
        Reply(ignored, "rival_shop_opens", "ignore");
        Assert.True(Could(ignored, "rival_price_war"));
        Assert.False(Could(ignored, "rival_price_war_slander"));
        Assert.True(Could(ignored, "feeney_boy"));
        Assert.False(Could(ignored, "feeney_boy_slander"));

        slandered.Lab.Formulas[0].PriceCents = 65;
        ignored.Lab.Formulas[0].PriceCents = 65;
        Assert.False(Could(slandered, "rival_price_war_slander"));
        Assert.False(Could(ignored, "rival_price_war"));
    }

    [Fact]
    public void FeeneysCopyFollowsEitherErrandBoyLetterAndABoyWhoLeftIsGone()
    {
        var s = Shop("feeney-boy", 75, 2);
        Reply(s, "rival_shop_opens", "spread_rumour", 4);
        Assert.True(Could(s, "wrong_bottle"));
        Assert.True(Could(s, "boy_measles"));
        Reply(s, "feeney_boy_slander", "let_him_go");
        Assert.True(Could(s, "feeney_copy"));
        Assert.True(Progression.WaitsOnLetter(C.ById.Events["feeney_copy"].Condition!));
        Assert.False(Could(s, "wrong_bottle"));
        Assert.False(Could(s, "boy_measles"));
        Assert.Equal(2, Requirements.RemediesSold(s));
        Assert.False(Could(Shop("one-remedy", 75, 1), "wrong_bottle"));
        Assert.True(Requirements.Evaluate(new RemedyPriceRequirement(75), s, C).Met);
        Assert.False(Requirements.Evaluate(new RemedyPriceRequirement(76), s, C).Met);
        Assert.Equal(new RequirementStatus(false, 0.5), Requirements.Evaluate(new AtLeastRequirement("remedies", 4), s, C));
    }

    [Fact]
    public void TheReporterAndMrsStoweRememberWhatYouAnswered()
    {
        var paid = Shop("pryor", 75);
        paid.Meta.Date = new GameDate(1892, 3);
        Reply(paid, "reporter_calls", "pay_him");
        Assert.True(Could(paid, "reporter_proofs_paid"));
        Assert.False(Could(paid, "reporter_proofs"));
        Reply(paid, "reporter_proofs_paid", "buy_the_head", 12);
        paid.Meta.Date = new GameDate(1904, 1);
        Assert.True(Could(paid, "reporter_returns"));

        var door = Shop("pryor-door", 75);
        door.Meta.Date = new GameDate(1892, 3);
        Reply(door, "reporter_calls", "show_the_door");
        Assert.True(Could(door, "reporter_proofs"));
        Assert.False(Could(door, "reporter_proofs_paid"));

        foreach (var (answer, letter, other) in new[]
        {
            ("purely_vegetable", "stowe_washington_vegetable", "stowe_washington"),
            ("answer_truly", "stowe_washington", "stowe_washington_vegetable"),
        })
        {
            var s = Shop($"stowe-{answer}", 75);
            Reply(s, "stowe_tract", answer, 18 * 12);
            s.Meta.Date = new GameDate(1905, 3);
            Assert.True(Could(s, letter));
            Assert.False(Could(s, other));
        }
    }

    [Fact]
    public void AnAnyWaitsOnALetterOnlyWhenEveryWayOfMeetingItDoes()
    {
        var a = new SinceRequirement("a", 1);
        var b = new SinceRequirement("b", 1);
        Assert.True(Progression.WaitsOnLetter(new AnyRequirement([a, b])));
        Assert.False(Progression.WaitsOnLetter(new AnyRequirement([new FlagRequirement("x"), a])));
        Assert.False(Progression.WaitsOnLetter(C.ById.Events["jobber_rival"].Condition!));
        var s = Shop("jobbers", 75);
        Assert.True(Could(s, "jobber_rival"));
        Reply(s, "jobber_exclusive", "sign", 3);
        Assert.False(Could(s, "jobber_rival"));
        s.Events.LastDrawn["jobber_exclusive"] = s.Meta.Turn - 12;
        Assert.True(Could(s, "jobber_rival"));
    }

    [Fact]
    public void AReplysMoneyIsBookedWhereItBelongs()
    {
        List<(string, string)> Booked(string eventId, string choiceId)
        {
            var s = Shop($"ledger-{eventId}", 75);
            int before = s.Month.Deltas.Count;
            Reply(s, eventId, choiceId);
            return s.Month.Deltas.Skip(before).Select(d => (d.Metric, d.Cause)).ToList();
        }
        Assert.Equal([("cash", Causes.Ingredients)], Booked("spirits_dearer", "outside_distiller"));
        Assert.Equal([("cash", Causes.Investment)], Booked("sign_painter", "gilt_sign"));
        Assert.Equal([("cash", Causes.Investment), ("reputation", Causes.Event), ("notoriety", Causes.Event)], Booked("photographer", "sit_for_it"));
        Assert.Equal([("cash", Causes.Bribe), ("notoriety", Causes.Event)], Booked("sanitary_inspector", "pay_fee"));
        Assert.Equal([("cash", Causes.Retainer), ("reputation", Causes.Event)], Booked("pedestal_fund", "box_and_dollar"));
        Assert.Equal([("cash", Progression.LetterLedger["trade"]), ("reputation", Causes.Event)], Booked("jury_duty", "serve"));
        Assert.Equal([("cash", Causes.Event)], Booked("medicine_show_returns", "take_money"));
    }
}
