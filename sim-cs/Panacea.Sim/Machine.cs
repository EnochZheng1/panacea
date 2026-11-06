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
    bool Withdrawal, // shaking, sweating, irritable: missed their dose
    bool Ruined); // spent beyond their means on the bottle and can no longer pay: one bottle at most

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
    string? Verdict,
    string? Ailment, // the complaint they took the remedy for, if known (a stranger by post: null)
    // the cause as the ward and the coroner's register give it (ReportedCauses): overdose, wasting,
    // ailment (of the complaint they came in with), old_age or natural; whether the remedy hastened
    // a death that looked natural is not shown
    string ReportedCause);

public sealed record ObservedIngredient(string Id, double CostPerUnitCents);

public sealed record ObservedFormula(
    string Id,
    string Name,
    int PriceCents,
    IReadOnlyList<string> Claims,
    int Stock,
    double UnitCostCents, // a bottle at today's prices: ingredients (with any discount or surcharge) and bottling
    Properties Properties);

public sealed record ObservedBatch(string FormulaId, int Bottles);

public sealed record ObservedPendingEvent(
    string InstanceId,
    string EventId,
    IReadOnlyList<string> ChoiceIds,
    string? DeathId,
    string? SubjectName,
    long? BribeCostCents,
    double? BribeExposureChance); // inquests: the chance a bribe paid now would come out

/// <summary>An investment as the proprietor sees it (api/machine.ts ObservedInvestment).</summary>
public sealed record ObservedInvestment(
    string Id,
    long CostCents,
    long UpkeepCents, // paid every month-end while held
    int? Months, // how long it runs once bought, or null for good
    bool Held,
    int? BoughtTurn,
    int? MonthsLeft, // held, with a term: month-ends left, this one included
    bool CanBuy,
    string? Refusal, // "already held", "not yet offered" or "not enough cash"
    int WalkInsPerMonth,
    double HeatPerBoldness, // heat a month per point of label boldness while held
    int? WholesaleBottles, // wholesale: bottles ordered this month at the shop's name and shelf price
    int? WholesalePriceCents, // wholesale: paid a bottle for the bottle the shop has most of
    int? PostBottles, // orders by post (the almanac's readers): bottles a month at the shelf price
    int? PostPriceCents, // orders by post: what each brings
    double? MishapChance, // chance of an accident each month while held
    // what it waits for: every condition of its requirement (met or not), then its price against
    // the cash in hand (Kind "cost"); empty while held
    IReadOnlyList<ConditionProgress> Progress);

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
    /// <summary>
    /// What the ward remembers against the shop, 0..100: the neighbours' memory of the harm the remedy
    /// did (not the authorities' suspicion, which is Heat). It fades over most of a year, and while it
    /// lasts strangers buy less, few come back as regulars, and some regulars leave.
    /// </summary>
    public double WardMemory { get; init; }
    /// <summary>What it remembers, counted over the last Months months, for the paper.</summary>
    public required WardRemembers WardRemembers { get; init; }
    public required OrderedMap<int> Capabilities { get; init; }
    /// <summary>Stills in the back room, lit or cold.</summary>
    public int Stills { get; init; }
    public int StillsFree { get; init; }
    /// <summary>The most bottles one still makes in a batch (every still alike).</summary>
    public int StillBottles { get; init; }
    /// <summary>
    /// What would add to the stills or their size, not yet had: unlocks (in content order), then
    /// ventures, each with what it waits for. The first with Stills &gt; 0 is the next still.
    /// </summary>
    public required IReadOnlyList<CapacityStep> NextCapacity { get; init; }
    public required OrderedMap<double> Inventory { get; init; }
    /// <summary>
    /// The stock at today's cost: bottles brewing or on the shelf, and ingredients on hand. Buying
    /// stock moves money from cash into here; the month's books are cash and stock together.
    /// </summary>
    public required StockValue StockValue { get; init; }
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
    public required IReadOnlyList<ObservedInvestment> Investments { get; init; }
    public LedgerEntry? LastMonth { get; init; }
    /// <summary>
    /// Of the last three closed months, how many ended with the shelf bare: the months an order
    /// from away, which takes only what is left on the shelf, would have gone short.
    /// </summary>
    public int ShelfBareMonths { get; init; }
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

public sealed record InvestSpec(IReadOnlyList<string> InvestmentIds) : ActionSpec
{
    public override string Type => "Invest";
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
            WardMemory = state.Ward.Memory,
            WardRemembers = WardSystem.Remembers(state, content),
            Capabilities = state.Player.Capabilities.Clone(),
            Stills = Math.Max(0, state.Player.Capabilities.GetOr("stills", 0)),
            StillsFree = Lab.StillsFree(state),
            StillBottles = Lab.StillBottles(state, content),
            NextCapacity = Progression.CapacitySteps(state, content),
            Inventory = state.Lab.Inventory.Clone(),
            StockValue = Lab.StockValue(state, content),
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
                    Lab.FormulaUnitCost(state, f.Components, f.ProcessId, content), a.Properties);
            }).ToList(),
            BatchesInProgress = state.Lab.Batches.Select(b => new ObservedBatch(b.FormulaId, b.Bottles)).ToList(),
            Customers = state.Customers.Select(c => new ObservedCustomer(
                c.Id, c.Name, c.Segment, c.Ailment, c.Consumer,
                state.Meta.Turn - c.JoinedTurn,
                c.BottlesLastMonth,
                LooksFor(c.Body.Health),
                MoodFor(c.Satisfaction),
                c.Body.Dependency > threshold,
                c.InWithdrawal,
                Market.IsRuined(c, content))).ToList(),
            CustomerCapacity = CustomersSystem.CustomerCapacity(state, content),
            Deaths = state.Deaths.Select(d => new ObservedDeath(
                d.Id, d.Turn, d.Date, d.Name, d.Segment, d.Consumer, d.Anonymous, d.FormulaId, d.Inquest, d.Verdict,
                d.Ailment, d.ReportedCause)).ToList(),
            PendingEvents = state.Events.Pending.Select(p =>
            {
                var death = !string.IsNullOrEmpty(p.DeathId) ? state.Deaths.Find(d => d.Id == p.DeathId) : null;
                return new ObservedPendingEvent(
                    p.InstanceId,
                    p.EventId,
                    Progression.AvailableChoices(state, content, p).Select(c => c.Id).ToList(),
                    string.IsNullOrEmpty(p.DeathId) ? null : p.DeathId,
                    death?.Name,
                    p.EventId == Scrutiny.InquestEventId ? Scrutiny.BribeCostCents(state, content) : null,
                    p.EventId == Scrutiny.InquestEventId ? Scrutiny.BribeExposureChance(state, content) : null);
            }).ToList(),
            Unlocked = state.Unlocks.Unlocked.Keys.ToList(),
            NextUnlocks = Progression.NextUnlocks(state, content),
            Investments = ObserveInvestments(state, content),
            LastMonth = state.Ledger.Count > 0 ? state.Ledger[^1] : null,
            ShelfBareMonths = state.Ledger.Skip(Math.Max(0, state.Ledger.Count - 3)).Count(e => e.ShelfEndBottles == 0),
        };
    }

    /// <summary>The formula orders from away would take: the one with most bottles on the shelf.</summary>
    private static Formula? OrderFormula(GameState state)
    {
        Formula? best = state.Lab.Formulas.Count > 0 ? state.Lab.Formulas[0] : null;
        int most = 0;
        foreach (var f in state.Lab.Formulas)
        {
            int n = Lab.StockOf(state, f.Id);
            if (n > most)
            {
                best = f;
                most = n;
            }
        }
        return best;
    }

    public static List<ObservedInvestment> ObserveInvestments(GameState state, ContentPack content)
    {
        var formula = OrderFormula(state);
        int? price = formula?.PriceCents;
        return content.Investments.Select(inv =>
        {
            state.Player.Investments.TryGetValue(inv.Id, out var holding);
            string? refusal = Sim.Investments.InvestRefusal(state, inv, content);
            var progress = holding is null ? Sim.Investments.Progress(state, inv, content) : new List<ConditionProgress>();
            return new ObservedInvestment(
                inv.Id,
                inv.CostCents,
                inv.UpkeepCents,
                inv.Months,
                holding is not null,
                holding?.BoughtTurn,
                holding?.EndsTurn is int ends ? ends - state.Meta.Turn : null,
                refusal is null,
                refusal,
                inv.WalkInsPerMonth,
                inv.HeatPerBoldness,
                inv.Wholesale is { } w ? Sim.Investments.WholesaleOrder(state, w, price) : null,
                inv.Wholesale is { } w2 && price is int p ? Sim.Investments.WholesalePrice(w2, p) : null,
                inv.PostOrders is { } po ? Sim.Investments.WholesaleOrder(state, po, price) : null,
                inv.PostOrders is { } po2 && price is int p2 ? Sim.Investments.WholesalePrice(po2, p2) : null,
                inv.Mishap?.ChancePerMonth,
                progress);
        }).ToList();
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
        var investmentIds = content.Investments
            .Where(inv => Sim.Investments.InvestRefusal(state, inv, content) is null)
            .Select(inv => inv.Id)
            .ToList();
        if (investmentIds.Count > 0) specs.Add(new InvestSpec(investmentIds));
        return specs;
    }
}
