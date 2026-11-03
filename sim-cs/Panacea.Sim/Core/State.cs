using System.Text.Json.Serialization;

namespace Panacea.Sim;

// The complete game state: a port of packages/sim/src/core/state.ts. It serialises (via
// StateJson.Options) to exactly the TS state's camelCase JSON shape, which the parity hash and
// save files depend on. String-valued enums keep the TS spellings; see the constant classes.

public static class Metrics
{
    public const string Cash = "cash";
    public const string Reputation = "reputation";
    public const string Notoriety = "notoriety";
    public const string Heat = "heat";
}

public static class LeaveReasons
{
    public const string Recovered = "recovered";
    public const string Dissatisfied = "dissatisfied";
    public const string Drifted = "drifted";
    public const string Bereaved = "bereaved";
}

/// <summary>Inquest states of a death: none, pending, or an outcome (cleared, implicated, bribed).</summary>
public static class InquestStates
{
    public const string None = "none";
    public const string Pending = "pending";
    public const string Cleared = "cleared";
    public const string Implicated = "implicated";
    public const string Bribed = "bribed";
}

public static class DeathCauses
{
    public const string Overdose = "overdose";
    public const string Poisoning = "poisoning";
    public const string Disease = "disease";
    public const string Natural = "natural";
}

public static class EndReasons
{
    public const string TimelineEnd = "timeline_end";
    public const string Bankrupt = "bankrupt";
    public const string ShutDown = "shut_down";
}

public static class GameStatus
{
    public const string Running = "running";
    public const string Ended = "ended";
}

public static class Consumers
{
    public const string Self = "self";
    public const string Infant = "infant";
}

public sealed class CausalDelta
{
    /// <summary>cash, reputation, notoriety or heat (<see cref="Metrics"/>).</summary>
    public required string Metric { get; set; }
    public double Amount { get; set; }
    /// <summary>A <see cref="Causes"/> id.</summary>
    public required string Cause { get; set; }
    /// <summary>Optional sub-cause, e.g. the event or formula id responsible.</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Detail { get; set; }
}

// ---------------------------------------------------------------------------
// News items (discriminated by "kind")
// ---------------------------------------------------------------------------

public abstract record NewsItem
{
    public abstract string Kind { get; }
}

public sealed record TimelineNews(string Id) : NewsItem
{
    public override string Kind => "timeline";
}

public sealed record UnlockNews(string Id) : NewsItem
{
    public override string Kind => "unlock";
}

public sealed record EventResolvedNews(string EventId, string ChoiceId, bool Auto) : NewsItem
{
    public override string Kind => "event_resolved";
}

public sealed record DeathNews(string DeathId) : NewsItem
{
    public override string Kind => "death";
}

public sealed record InquestNews(string DeathId, string Outcome, string Verdict) : NewsItem
{
    public override string Kind => "inquest";
}

public sealed record CustomerJoinedNews(string CustomerId) : NewsItem
{
    public override string Kind => "customer_joined";
}

public sealed record CustomerLeftNews(string CustomerId, string Name, string Reason) : NewsItem
{
    public override string Kind => "customer_left";
}

public sealed record BadBatchCaughtNews(string FormulaId, int Bottles) : NewsItem
{
    public override string Kind => "bad_batch_caught";
}

public sealed record StockSeizedNews(int Bottles) : NewsItem
{
    public override string Kind => "stock_seized";
}

public sealed record AdsLapsedNews : NewsItem
{
    public override string Kind => "ads_lapsed";
}

public sealed record ShutdownWarningNews(int MonthsLeft) : NewsItem
{
    public override string Kind => "shutdown_warning";
}

public sealed class SaleRecord
{
    public required string FormulaId { get; set; }
    public int Bottles { get; set; }
    public long RevenueCents { get; set; }
}

/// <summary>Running totals for the month in progress.</summary>
public sealed class MonthInProgress
{
    public long StartCashCents { get; set; }
    public double StartReputation { get; set; }
    public double StartNotoriety { get; set; }
    public double StartHeat { get; set; }
    public List<CausalDelta> Deltas { get; set; } = new();
    public List<NewsItem> News { get; set; } = new();
    public List<SaleRecord> Sales { get; set; } = new();
    /// <summary>Grumbles this month (withdrawal, disappointment).</summary>
    public int Complaints { get; set; }
}

/// <summary>A closed month, as shown in the Ledger.</summary>
public sealed class LedgerEntry
{
    public int Turn { get; set; }
    public required GameDate Date { get; set; }
    public long CashStartCents { get; set; }
    public long CashEndCents { get; set; }
    public double ReputationStart { get; set; }
    public double ReputationEnd { get; set; }
    public double NotorietyStart { get; set; }
    public double NotorietyEnd { get; set; }
    public double HeatStart { get; set; }
    public double HeatEnd { get; set; }
    public List<CausalDelta> Deltas { get; set; } = new();
    public List<NewsItem> News { get; set; } = new();
    public List<SaleRecord> Sales { get; set; } = new();
    public int Customers { get; set; }
    public int Complaints { get; set; }
}

public sealed record FormulaComponent(string IngredientId, double Amount);

public sealed class Formula
{
    public required string Id { get; set; }
    public required string Name { get; set; }
    public List<FormulaComponent> Components { get; set; } = new();
    public required string ProcessId { get; set; }
    public int PriceCents { get; set; }
    /// <summary>Label and advertising claims (claim ids).</summary>
    public List<string> Claims { get; set; } = new();
    public int CreatedTurn { get; set; }
}

public sealed class Batch
{
    public required string Id { get; set; }
    public required string FormulaId { get; set; }
    /// <summary>The recipe as it was when brewed.</summary>
    public List<FormulaComponent> Components { get; set; } = new();
    public required string ProcessId { get; set; }
    public int Bottles { get; set; }
    public int StartedTurn { get; set; }
    public int ReadyTurn { get; set; }
}

/// <summary>Finished bottles from one batch. Truth: <see cref="Potency"/> is the batch's real strength.</summary>
public sealed class Lot
{
    public required string Id { get; set; }
    public required string FormulaId { get; set; }
    public List<FormulaComponent> Components { get; set; } = new();
    public required string ProcessId { get; set; }
    public int Bottles { get; set; }
    public double Potency { get; set; }
    public bool BadBatch { get; set; }

    public Lot ShallowCopy() => (Lot)MemberwiseClone();
}

/// <summary>Per drug class values (tolerance), keyed alcohol / opiate / cocaine.</summary>
public sealed class DrugValues
{
    public double Alcohol { get; set; }
    public double Opiate { get; set; }
    public double Cocaine { get; set; }

    [JsonIgnore]
    public double this[string drugClass]
    {
        get => drugClass switch
        {
            DrugClasses.Alcohol => Alcohol,
            DrugClasses.Opiate => Opiate,
            DrugClasses.Cocaine => Cocaine,
            _ => throw new KeyNotFoundException(drugClass),
        };
        set
        {
            switch (drugClass)
            {
                case DrugClasses.Alcohol: Alcohol = value; break;
                case DrugClasses.Opiate: Opiate = value; break;
                case DrugClasses.Cocaine: Cocaine = value; break;
                default: throw new KeyNotFoundException(drugClass);
            }
        }
    }
}

public sealed class Body
{
    public double Health { get; set; }
    public double BaseHealth { get; set; }
    public DrugValues Tolerance { get; set; } = new();
    /// <summary>0..1</summary>
    public double Dependency { get; set; }
    /// <summary>Accumulated toxins that do not clear quickly (heavy metals).</summary>
    public double Load { get; set; }
}

/// <summary>A regular customer of the shop.</summary>
public sealed class Customer
{
    public required string Id { get; set; }
    public required string Name { get; set; }
    public required string Segment { get; set; }
    public required string Ailment { get; set; }
    /// <summary>"self" or "infant" (<see cref="Consumers"/>).</summary>
    public required string Consumer { get; set; }
    public required BuyerProfile Profile { get; set; }
    public int JoinedTurn { get; set; }
    public int? AilmentEndsTurn { get; set; }
    public required Body Body { get; set; }
    /// <summary>-1..1</summary>
    public double Satisfaction { get; set; }
    public int Purchases { get; set; }
    public int? LastPurchaseTurn { get; set; }
    public string? LastFormulaId { get; set; }
    public int BottlesLastMonth { get; set; }
    public bool InWithdrawal { get; set; }
}

/// <summary>The human ledger. Truth fields are hidden from the player.</summary>
public sealed class DeathRecord
{
    public required string Id { get; set; }
    public int Turn { get; set; }
    public required GameDate Date { get; set; }
    public string? CustomerId { get; set; }
    public required string Name { get; set; }
    public required string Segment { get; set; }
    public required string Consumer { get; set; }
    public bool Anonymous { get; set; }
    /// <summary>Truth (<see cref="DeathCauses"/>).</summary>
    public required string Cause { get; set; }
    /// <summary>Truth: did your product cause or hasten this death?</summary>
    public bool ProductImplicated { get; set; }
    public string? FormulaId { get; set; }
    public bool BadBatch { get; set; }
    public double Detectability { get; set; }
    public double Boldness { get; set; }
    /// <summary><see cref="InquestStates"/>.</summary>
    public required string Inquest { get; set; }
    public string? Verdict { get; set; }
}

public sealed class PendingEvent
{
    public required string InstanceId { get; set; }
    public required string EventId { get; set; }
    public int DrawnTurn { get; set; }
    /// <summary>For system events: the death an inquest concerns.</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? DeathId { get; set; }
}

/// <summary>A replay log entry: either an action, or the end of a month.</summary>
public sealed class LogEntry
{
    public int Turn { get; set; }
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public GameAction? Action { get; set; }
    /// <summary>True for an end-of-month entry; null (absent) for an action.</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public bool? EndTurn { get; set; }

    [JsonIgnore]
    public bool IsEndTurn => EndTurn == true;
}

public sealed class MetaState
{
    public int StateVersion { get; set; }
    public required string ContentVersion { get; set; }
    public required string Seed { get; set; }
    public required string BackgroundId { get; set; }
    public string Difficulty { get; set; } = "normal";
    public required RngState Rng { get; set; }
    /// <summary>0-based month counter.</summary>
    public int Turn { get; set; }
    public required GameDate Date { get; set; }
    /// <summary>"running" or "ended" (<see cref="GameStatus"/>).</summary>
    public string Status { get; set; } = GameStatus.Running;
    /// <summary><see cref="EndReasons"/>, or null while running.</summary>
    public string? EndReason { get; set; }
    /// <summary>Counter for generated ids.</summary>
    public int NextId { get; set; }
}

public sealed class PlayerState
{
    public long CashCents { get; set; }
    public double Reputation { get; set; }
    public double Notoriety { get; set; }
    public OrderedMap<int> Capabilities { get; set; } = new();
    public OrderedMap<bool> Flags { get; set; } = new();
    public int MonthsInsolvent { get; set; }
}

public sealed class LabState
{
    /// <summary>Fluid ounces on hand, by ingredient id.</summary>
    public OrderedMap<double> Inventory { get; set; } = new();
    public List<Formula> Formulas { get; set; } = new();
    public List<Batch> Batches { get; set; } = new();
    /// <summary>Finished bottles on the shelf, oldest first.</summary>
    public List<Lot> Stock { get; set; } = new();
}

public sealed class MarketingState
{
    public int AdSpendCents { get; set; }
}

public sealed class ScrutinyState
{
    /// <summary>0..100</summary>
    public double Heat { get; set; }
    public double PeakHeat { get; set; }
    public int BribesPaid { get; set; }
    public int InquestsHeld { get; set; }
    public int TimesImplicated { get; set; }
    public int MonthsAtShutdownHeat { get; set; }
}

public sealed class StatsState
{
    public int BottlesSold { get; set; }
    public int BatchesBrewed { get; set; }
    public long RevenueCents { get; set; }
    public int CustomersJoined { get; set; }
    public int CustomersLost { get; set; }
    public int Complaints { get; set; }
}

public sealed class UnlocksState
{
    /// <summary>Unlock id → turn it was gained.</summary>
    public OrderedMap<int> Unlocked { get; set; } = new();
}

public sealed class EventsState
{
    public List<PendingEvent> Pending { get; set; } = new();
    /// <summary>Event id → last turn it was drawn.</summary>
    public OrderedMap<int> LastDrawn { get; set; } = new();
    /// <summary>Event id → times drawn.</summary>
    public OrderedMap<int> TimesDrawn { get; set; } = new();
}

public sealed class TimelineState
{
    /// <summary>Timeline entry id → turn it fired.</summary>
    public OrderedMap<int> Fired { get; set; } = new();
}

/// <summary>The complete game state. Money is always integer cents.</summary>
public sealed class GameState
{
    public const int StateVersion = 1;

    public required MetaState Meta { get; set; }
    public required PlayerState Player { get; set; }
    public required LabState Lab { get; set; }
    public required MarketingState Marketing { get; set; }
    public List<Customer> Customers { get; set; } = new();
    public List<DeathRecord> Deaths { get; set; } = new();
    public required ScrutinyState Scrutiny { get; set; }
    public required StatsState Stats { get; set; }
    public required UnlocksState Unlocks { get; set; }
    public required EventsState Events { get; set; }
    public required TimelineState Timeline { get; set; }
    public required MonthInProgress Month { get; set; }
    /// <summary>Closed months.</summary>
    public List<LedgerEntry> Ledger { get; set; } = new();
    /// <summary>Replay log.</summary>
    public List<LogEntry> Log { get; set; } = new();

    /// <summary>A deep copy (via the JSON shape, which round-trips every double exactly).</summary>
    public GameState Clone() => StateJson.Clone(this);
}
