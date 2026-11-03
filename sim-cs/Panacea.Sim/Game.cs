namespace Panacea.Sim;

public sealed class NewGameOptions
{
    public required string Seed { get; init; }
    /// <summary>Defaults to the content's default background.</summary>
    public string? BackgroundId { get; init; }
}

/// <summary>The game loop (packages/sim/src/game.ts).</summary>
public static class Game
{
    /// <summary>Month-end phases, in order.</summary>
    public static readonly IReadOnlyList<Phase> Phases =
    [
        Scrutiny.HeatDecayPhase,
        Lab.ProductionPhase,
        Market.AdvertisingPhase,
        Market.SalesPhase,
        BodySystem.BodyPhase,
        Market.ExpensesPhase,
        Progression.TimelinePhase,
        Progression.UnlocksPhase,
    ];

    public static GameState NewGame(ContentPack content, NewGameOptions options)
    {
        string backgroundId = options.BackgroundId ?? content.Config.DefaultBackground;
        if (!content.ById.Backgrounds.TryGetValue(backgroundId, out var background))
            throw new ArgumentException($"unknown background \"{backgroundId}\"", nameof(options));

        var flags = new OrderedMap<bool>();
        foreach (var f in background.Flags) flags[f] = true;

        var state = new GameState
        {
            Meta = new MetaState
            {
                StateVersion = GameState.StateVersion,
                ContentVersion = content.Config.ContentVersion,
                Seed = options.Seed,
                BackgroundId = backgroundId,
                Difficulty = "normal",
                Rng = Rng.SeedRng(options.Seed),
                Turn = 0,
                Date = content.Config.StartDate,
                Status = GameStatus.Running,
                EndReason = null,
                NextId = 1,
            },
            Player = new PlayerState
            {
                CashCents = background.StartingCashCents,
                Reputation = content.Config.StartingReputation,
                Notoriety = 0,
                Capabilities = background.Capabilities.Clone(),
                Flags = flags,
                MonthsInsolvent = 0,
            },
            Lab = new LabState(),
            Marketing = new MarketingState { AdSpendCents = 0 },
            Customers = CustomersSystem.InitialCustomers(content),
            Deaths = new List<DeathRecord>(),
            Scrutiny = new ScrutinyState(),
            Stats = new StatsState(),
            Unlocks = new UnlocksState(),
            Events = new EventsState(),
            Timeline = new TimelineState(),
            Month = new MonthInProgress
            {
                StartCashCents = background.StartingCashCents,
                StartReputation = content.Config.StartingReputation,
                StartNotoriety = 0,
                StartHeat = 0,
            },
        };

        var rng = new Rng(state.Meta.Rng);
        Progression.DrawEvents(state, content, rng);
        state.Meta.Rng = rng.State();
        return state;
    }

    /// <summary>
    /// Applies one player action without mutating the input: returns a deep copy with the action
    /// applied (or the input itself, unchanged, on failure).
    /// </summary>
    public static (GameState State, ActionResult Result) ApplyAction(GameState state, GameAction action, ContentPack content)
    {
        if (state.Meta.Status != GameStatus.Running) return (state, ActionResult.Fail("the game has ended"));
        var draft = state.Clone();
        var result = ApplyActionInPlace(draft, action, content);
        return result.Ok ? (draft, result) : (state, result);
    }

    /// <summary>
    /// Applies one player action, mutating <paramref name="state"/>. A failed action leaves the
    /// state unchanged. Successful actions are appended to the replay log.
    /// </summary>
    public static ActionResult ApplyActionInPlace(GameState state, GameAction action, ContentPack content)
    {
        if (state.Meta.Status != GameStatus.Running) return ActionResult.Fail("the game has ended");
        var rng = new Rng(state.Meta.Rng);
        ActionResult result;
        switch (action)
        {
            case BuyIngredient a:
                result = Lab.BuyIngredient(state, a, content);
                break;
            case CreateFormula a:
                result = Lab.CreateFormula(state, a, content);
                break;
            case SetPrice a:
                result = Lab.SetPrice(state, a);
                break;
            case Reformulate a:
                result = Lab.Reformulate(state, a, content);
                break;
            case SetClaims a:
                result = Lab.SetClaims(state, a, content);
                break;
            case SetAdSpend a:
                result = Lab.SetAdSpend(state, a);
                break;
            case BrewBatch a:
                result = Lab.BrewBatch(state, a, content);
                break;
            case ResolveEvent a:
            {
                var error = Progression.ResolveEvent(state, content, rng, a.InstanceId, a.ChoiceId, false);
                result = error is not null ? ActionResult.Fail(error) : ActionResult.Success;
                break;
            }
            default:
                result = ActionResult.Fail($"unknown action type \"{action?.Type}\"");
                break;
        }
        if (!result.Ok) return result;
        state.Meta.Rng = rng.State();
        state.Log.Add(new LogEntry { Turn = state.Meta.Turn, Action = CanonicalAction(action!) });
        return result;
    }

    /// <summary>A fresh copy of the action holding exactly the fields the rules read (game.ts canonicalAction).</summary>
    public static GameAction CanonicalAction(GameAction action) => action switch
    {
        CreateFormula a => a with { Components = a.Components.ToList(), Claims = a.Claims?.ToList() },
        Reformulate a => a with { Components = a.Components.ToList() },
        SetClaims a => a with { Claims = a.Claims.ToList() },
        _ => action,
    };

    /// <summary>Ends the month without mutating the input: returns a deep copy and the month's report.</summary>
    public static (GameState State, LedgerEntry Report) EndTurn(GameState state, ContentPack content)
    {
        if (state.Meta.Status != GameStatus.Running) throw new InvalidOperationException("endTurn: the game has ended");
        var draft = state.Clone();
        return (draft, EndTurnInPlace(draft, content));
    }

    /// <summary>
    /// Ends the month, mutating <paramref name="state"/>: auto-resolves undecided events, runs every
    /// phase, closes the Ledger entry, checks for the end of the game, and opens the next month.
    /// </summary>
    public static LedgerEntry EndTurnInPlace(GameState state, ContentPack content)
    {
        if (state.Meta.Status != GameStatus.Running) throw new InvalidOperationException("endTurn: the game has ended");
        var rng = new Rng(state.Meta.Rng);
        var ctx = new TurnContext { State = state, Content = content, Rng = rng };

        state.Log.Add(new LogEntry { Turn = state.Meta.Turn, EndTurn = true });
        Progression.AutoResolveEvents(state, content, rng);
        foreach (var phase in Phases) phase.Run(ctx);

        state.Player.MonthsInsolvent = state.Player.CashCents < 0 ? state.Player.MonthsInsolvent + 1 : 0;
        var cfg = content.Config.Scrutiny;
        var scrutiny = state.Scrutiny;
        scrutiny.MonthsAtShutdownHeat = scrutiny.Heat >= cfg.ShutdownHeat ? scrutiny.MonthsAtShutdownHeat + 1 : 0;
        if (scrutiny.MonthsAtShutdownHeat > 0 && scrutiny.MonthsAtShutdownHeat < cfg.ShutdownGraceMonths)
        {
            state.Month.News.Add(new ShutdownWarningNews(cfg.ShutdownGraceMonths - scrutiny.MonthsAtShutdownHeat));
        }
        var report = Ledger.CloseMonth(state);
        state.Ledger.Add(report);

        if (state.Player.MonthsInsolvent >= content.Config.BankruptcyGraceMonths)
        {
            state.Meta.Status = GameStatus.Ended;
            state.Meta.EndReason = EndReasons.Bankrupt;
        }
        else if (scrutiny.MonthsAtShutdownHeat >= cfg.ShutdownGraceMonths)
        {
            state.Meta.Status = GameStatus.Ended;
            state.Meta.EndReason = EndReasons.ShutDown;
        }
        else if (Dates.Compare(state.Meta.Date, content.Config.EndDate) >= 0)
        {
            state.Meta.Status = GameStatus.Ended;
            state.Meta.EndReason = EndReasons.TimelineEnd;
        }
        else
        {
            state.Meta.Turn += 1;
            state.Meta.Date = Dates.AddMonths(state.Meta.Date, 1);
            state.Month = Ledger.OpenMonth(state);
            Progression.DrawEvents(state, content, rng);
        }

        state.Meta.Rng = rng.State();
        return report;
    }

    /// <summary>Rebuilds a game from its options and action log.</summary>
    public static GameState Replay(ContentPack content, NewGameOptions options, IEnumerable<LogEntry> log)
    {
        var state = NewGame(content, options);
        foreach (var entry in log)
        {
            if (entry.IsEndTurn)
            {
                EndTurnInPlace(state, content);
            }
            else
            {
                var result = ApplyActionInPlace(state, entry.Action!, content);
                if (!result.Ok)
                    throw new InvalidOperationException($"replay: action at turn {entry.Turn} failed: {result.Error}");
            }
        }
        return state;
    }

    /// <summary>The fingerprint of a state, identical to the TS <c>hashValue(state)</c>.</summary>
    public static string HashState(GameState state) => Hash.HashValue(state);
}
