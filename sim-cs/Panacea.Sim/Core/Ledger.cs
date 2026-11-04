namespace Panacea.Sim;

/// <summary>Causal accounting (core/ledger.ts): the single way to change a player-visible number.</summary>
public static class Ledger
{
    public const double ReputationMin = 0;
    public const double ReputationMax = 100;
    public const double HeatMax = 100;

    /// <summary>
    /// Applies the change (with clamping), records a causal delta for the Ledger, and returns the
    /// amount actually applied.
    /// </summary>
    public static double Adjust(GameState state, string metric, double amount, string cause, string? detail = null)
    {
        double applied;
        switch (metric)
        {
            case Metrics.Cash:
            {
                applied = JsMath.Round(amount);
                state.Player.CashCents += (long)applied;
                break;
            }
            case Metrics.Reputation:
            {
                double before = state.Player.Reputation;
                double after = Math.Min(ReputationMax, Math.Max(ReputationMin, before + amount));
                state.Player.Reputation = after;
                applied = after - before;
                break;
            }
            case Metrics.Notoriety:
            {
                double before = state.Player.Notoriety;
                double after = Math.Max(0, before + amount);
                state.Player.Notoriety = after;
                applied = after - before;
                break;
            }
            case Metrics.Heat:
            {
                double before = state.Scrutiny.Heat;
                double after = Math.Min(HeatMax, Math.Max(0, before + amount));
                state.Scrutiny.Heat = after;
                state.Scrutiny.PeakHeat = Math.Max(state.Scrutiny.PeakHeat, after);
                applied = after - before;
                break;
            }
            default:
                throw new ArgumentException($"unknown metric \"{metric}\"", nameof(metric));
        }
        if (applied != 0)
        {
            state.Month.Deltas.Add(new CausalDelta { Metric = metric, Amount = applied, Cause = cause, Detail = detail });
        }
        return applied;
    }

    public static MonthInProgress OpenMonth(GameState state) => new()
    {
        StartCashCents = state.Player.CashCents,
        StartReputation = state.Player.Reputation,
        StartNotoriety = state.Player.Notoriety,
        StartHeat = state.Scrutiny.Heat,
    };

    public static LedgerEntry CloseMonth(GameState state)
    {
        var m = state.Month;
        return new LedgerEntry
        {
            Turn = state.Meta.Turn,
            Date = state.Meta.Date,
            CashStartCents = m.StartCashCents,
            CashEndCents = state.Player.CashCents,
            ReputationStart = m.StartReputation,
            ReputationEnd = state.Player.Reputation,
            NotorietyStart = m.StartNotoriety,
            NotorietyEnd = state.Player.Notoriety,
            HeatStart = m.StartHeat,
            HeatEnd = state.Scrutiny.Heat,
            Deltas = m.Deltas,
            News = m.News,
            Sales = m.Sales,
            Customers = state.Customers.Count,
            Complaints = m.Complaints,
            TurnedAwayByPrice = m.TurnedAwayByPrice,
        };
    }

    /// <summary>Sums a list of deltas for one metric (in list order, like the TS reduce).</summary>
    public static double SumDeltas(IEnumerable<CausalDelta> deltas, string metric)
    {
        double s = 0;
        foreach (var d in deltas)
            if (d.Metric == metric) s = s + d.Amount;
        return s;
    }

    /// <summary>Totals per cause for one metric, in first-seen order.</summary>
    public static OrderedMap<double> DeltasByCause(IEnumerable<CausalDelta> deltas, string metric)
    {
        var o = new OrderedMap<double>();
        foreach (var d in deltas)
            if (d.Metric == metric) o[d.Cause] = o.GetOr(d.Cause, 0) + d.Amount;
        return o;
    }
}

/// <summary>One bottle bought this month, with the true strength of the batch it came from.</summary>
public sealed record Bought(
    string FormulaId,
    string LotId,
    IReadOnlyList<FormulaComponent> Components,
    string ProcessId,
    double Potency,
    bool BadBatch);

/// <summary>Working data passed between phases within a single month-end. Never saved.</summary>
public sealed class TurnScratch
{
    /// <summary>Customer id → bottles bought this month.</summary>
    public Dictionary<string, List<Bought>> Purchases { get; } = new(StringComparer.Ordinal);
    /// <summary>One-off walk-in purchases (anonymous drinkers).</summary>
    public List<(string SegmentId, string AilmentId, Bought Bought)> WalkIns { get; } = new();
    public int AdVisitors { get; set; }
}

/// <summary>What every turn phase receives.</summary>
public sealed class TurnContext
{
    public required GameState State { get; init; }
    public required ContentPack Content { get; init; }
    public required Rng Rng { get; init; }
    public TurnScratch Scratch { get; } = new();
}

public sealed record Phase(string Id, Action<TurnContext> Run);
