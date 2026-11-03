namespace Panacea.Sim;

public sealed record ObservedCustomer(
    string Id,
    string Name,
    string Segment,
    string Ailment,
    string Consumer,
    int MonthsAsCustomer,
    int BottlesLastMonth,
    string Looks, // hale, peaky, poorly or gravely_ill
    string Mood, // delighted, content, grumbling or unhappy
    bool Craving, // asks for the medicine by name and won't take substitutes
    bool Withdrawal); // shaking, sweating, irritable: missed their dose

public sealed record ObservedDeath(
    string Id,
    int Turn,
    GameDate Date,
    string Name,
    string Segment,
    string Consumer,
    bool Anonymous,
    string? FormulaId,
    string Inquest,
    string? Verdict);

public sealed record ObservedIngredient(string Id, double CostPerUnitCents);

public sealed record ObservedFormula(
    string Id,
    string Name,
    int PriceCents,
    IReadOnlyList<string> Claims,
    int Stock,
    double UnitCostCents,
    Properties Properties);

public sealed record ObservedBatch(string FormulaId, int Bottles);

public sealed record ObservedPendingEvent(
    string InstanceId,
    string EventId,
    IReadOnlyList<string> ChoiceIds,
    string? DeathId,
    string? SubjectName,
    long? BribeCostCents);

/// <summary>What a player could know (api/machine.ts Observation).</summary>
public sealed class Observation
{
    public int Turn { get; init; }
    public required GameDate Date { get; init; }
    public required GameDate EndDate { get; init; }
    public required string Status { get; init; }
    public string? EndReason { get; init; }
    public long CashCents { get; init; }
    public double Reputation { get; init; }
    public double Notoriety { get; init; }
    public double Heat { get; init; }
    public required OrderedMap<int> Capabilities { get; init; }
    public int StillsFree { get; init; }
    public required OrderedMap<double> Inventory { get; init; }
    public required IReadOnlyList<ObservedIngredient> Ingredients { get; init; }
    public required IReadOnlyList<string> Processes { get; init; }
    public required IReadOnlyList<string> Claims { get; init; }
    public bool CanAdvertise { get; init; }
    public int AdSpendCents { get; init; }
    public required IReadOnlyList<ObservedFormula> Formulas { get; init; }
    public required IReadOnlyList<ObservedBatch> BatchesInProgress { get; init; }
    public required IReadOnlyList<ObservedCustomer> Customers { get; init; }
    public int CustomerCapacity { get; init; }
    public required IReadOnlyList<ObservedDeath> Deaths { get; init; }
    public required IReadOnlyList<ObservedPendingEvent> PendingEvents { get; init; }
    public required IReadOnlyList<string> Unlocked { get; init; }
    public required IReadOnlyList<UnlockProgress> NextUnlocks { get; init; }
    public LedgerEntry? LastMonth { get; init; }
}

/// <summary>A kind of move currently available, with parameter bounds (api/machine.ts ActionSpec).</summary>
public abstract record ActionSpec
{
    public abstract string Type { get; }
}

public sealed record BuyIngredientSpec(string IngredientId, int MaxUnits) : ActionSpec
{
    public override string Type => "BuyIngredient";
}

public sealed record CreateFormulaSpec(
    IReadOnlyList<string> IngredientIds,
    IReadOnlyList<string> ProcessIds,
    IReadOnlyList<string> ClaimIds,
    double MaxVolume) : ActionSpec
{
    public override string Type => "CreateFormula";
}

public sealed record SetPriceSpec(string FormulaId) : ActionSpec
{
    public override string Type => "SetPrice";
}

public sealed record ReformulateSpec(
    string FormulaId,
    IReadOnlyList<string> IngredientIds,
    IReadOnlyList<string> ProcessIds,
    double MaxVolume) : ActionSpec
{
    public override string Type => "Reformulate";
}

public sealed record SetClaimsSpec(string FormulaId, IReadOnlyList<string> ClaimIds, int MaxClaims) : ActionSpec
{
    public override string Type => "SetClaims";
}

public sealed record SetAdSpendSpec : ActionSpec
{
    public override string Type => "SetAdSpend";
}

public sealed record BrewBatchSpec(string FormulaId, int MaxBottles) : ActionSpec
{
    public override string Type => "BrewBatch";
}

public sealed record ResolveEventSpec(string InstanceId, IReadOnlyList<string> ChoiceIds) : ActionSpec
{
    public override string Type => "ResolveEvent";
}

/// <summary>
/// The machine API: what scripted bots, AI playtesters and the client see and can do. Observe
/// returns only what a player could know; LegalActions lists the valid moves.
/// </summary>
public static class Machine
{
    public static string LooksFor(double health) =>
        health >= 70 ? "hale" : health >= 50 ? "peaky" : health >= 30 ? "poorly" : "gravely_ill";

    public static string MoodFor(double s) =>
        s >= 0.4 ? "delighted" : s >= 0 ? "content" : s >= -0.4 ? "grumbling" : "unhappy";

    private static List<string> AvailableClaims(GameState state, ContentPack content) =>
        content.Claims.Where(c => Formulas.ValidateClaims([c.Id], state, content) is null).Select(c => c.Id).ToList();

    private static List<string> AvailableProcesses(GameState state, ContentPack content) =>
        content.Processes.Where(p => Formulas.IsProcessAvailable(p.Id, state, content)).Select(p => p.Id).ToList();

    public static Observation Observe(GameState state, ContentPack content)
    {
        double threshold = content.Config.Body.WithdrawalThreshold;
        return new Observation
        {
            Turn = state.Meta.Turn,
            Date = state.Meta.Date,
            EndDate = content.Config.EndDate,
            Status = state.Meta.Status,
            EndReason = state.Meta.EndReason,
            CashCents = state.Player.CashCents,
            Reputation = state.Player.Reputation,
            Notoriety = state.Player.Notoriety,
            Heat = state.Scrutiny.Heat,
            Capabilities = state.Player.Capabilities.Clone(),
            StillsFree = Lab.StillsFree(state),
            Inventory = state.Lab.Inventory.Clone(),
            Ingredients = content.Ingredients
                .Where(i => Formulas.IsIngredientAvailable(i.Id, state, content))
                .Select(i => new ObservedIngredient(i.Id, Lab.IngredientUnitCost(state, i.Id, content)))
                .ToList(),
            Processes = AvailableProcesses(state, content),
            Claims = AvailableClaims(state, content),
            CanAdvertise = state.Unlocks.Unlocked.ContainsKey("newspaper_ads"),
            AdSpendCents = state.Marketing.AdSpendCents,
            Formulas = state.Lab.Formulas.Select(f =>
            {
                var a = Sim.Formulas.Analyze(f.Components, f.ProcessId, content);
                return new ObservedFormula(f.Id, f.Name, f.PriceCents, f.Claims.ToList(), Lab.StockOf(state, f.Id),
                    a.UnitCostCents, a.Properties);
            }).ToList(),
            BatchesInProgress = state.Lab.Batches.Select(b => new ObservedBatch(b.FormulaId, b.Bottles)).ToList(),
            Customers = state.Customers.Select(c => new ObservedCustomer(
                c.Id, c.Name, c.Segment, c.Ailment, c.Consumer,
                state.Meta.Turn - c.JoinedTurn,
                c.BottlesLastMonth,
                LooksFor(c.Body.Health),
                MoodFor(c.Satisfaction),
                c.Body.Dependency > threshold,
                c.InWithdrawal)).ToList(),
            CustomerCapacity = CustomersSystem.CustomerCapacity(state, content),
            Deaths = state.Deaths.Select(d => new ObservedDeath(
                d.Id, d.Turn, d.Date, d.Name, d.Segment, d.Consumer, d.Anonymous, d.FormulaId, d.Inquest, d.Verdict)).ToList(),
            PendingEvents = state.Events.Pending.Select(p =>
            {
                var death = !string.IsNullOrEmpty(p.DeathId) ? state.Deaths.Find(d => d.Id == p.DeathId) : null;
                return new ObservedPendingEvent(
                    p.InstanceId,
                    p.EventId,
                    Progression.AvailableChoices(state, content, p).Select(c => c.Id).ToList(),
                    string.IsNullOrEmpty(p.DeathId) ? null : p.DeathId,
                    death?.Name,
                    p.EventId == Scrutiny.InquestEventId ? Scrutiny.BribeCostCents(state, content) : null);
            }).ToList(),
            Unlocked = state.Unlocks.Unlocked.Keys.ToList(),
            NextUnlocks = Progression.NextUnlocks(state, content),
            LastMonth = state.Ledger.Count > 0 ? state.Ledger[^1] : null,
        };
    }

    /// <summary>Every kind of move currently available, with parameter bounds.</summary>
    public static List<ActionSpec> LegalActions(GameState state, ContentPack content)
    {
        var specs = new List<ActionSpec>();
        if (state.Meta.Status != GameStatus.Running) return specs;
        var available = content.Ingredients.Where(i => Formulas.IsIngredientAvailable(i.Id, state, content)).ToList();
        var ingredientIds = available.Select(i => i.Id).ToList();
        var claimIds = AvailableClaims(state, content);

        foreach (var ing in available)
        {
            double unit = Lab.IngredientUnitCost(state, ing.Id, content);
            double maxUnits = unit == 0 ? 10_000 : Math.Floor(state.Player.CashCents / unit);
            if (maxUnits >= 1) specs.Add(new BuyIngredientSpec(ing.Id, (int)Math.Min(maxUnits, int.MaxValue)));
        }
        var processIds = AvailableProcesses(state, content);
        if (state.Lab.Formulas.Count < content.Config.MaxFormulas)
        {
            specs.Add(new CreateFormulaSpec(ingredientIds, processIds, claimIds, content.Config.BottleCapacity));
        }
        foreach (var f in state.Lab.Formulas)
        {
            specs.Add(new SetPriceSpec(f.Id));
            specs.Add(new ReformulateSpec(f.Id, ingredientIds, processIds, content.Config.BottleCapacity));
            specs.Add(new SetClaimsSpec(f.Id, claimIds, content.Config.MaxClaimsPerFormula));
            int maxBottles = Lab.MaxBrewable(state, f.Id, content);
            if (maxBottles >= 1) specs.Add(new BrewBatchSpec(f.Id, maxBottles));
        }
        if (state.Unlocks.Unlocked.ContainsKey("newspaper_ads")) specs.Add(new SetAdSpendSpec());
        foreach (var p in state.Events.Pending)
        {
            var choiceIds = Progression.AvailableChoices(state, content, p).Select(c => c.Id).ToList();
            if (choiceIds.Count > 0) specs.Add(new ResolveEventSpec(p.InstanceId, choiceIds));
        }
        return specs;
    }
}
