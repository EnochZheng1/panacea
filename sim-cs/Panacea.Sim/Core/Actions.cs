using System.Text.Json.Serialization;

namespace Panacea.Sim;

/// <summary>
/// Player commands (core/actions.ts). The UI, scripted bots and AI playtesters all play through
/// exactly these. Serialises as <c>{ "type": "BuyIngredient", ... }</c> like the TS actions.
/// </summary>
public abstract record GameAction
{
    /// <summary>The TS discriminator, e.g. "BuyIngredient".</summary>
    public abstract string Type { get; }
}

public sealed record BuyIngredient(string IngredientId, int Units) : GameAction
{
    public override string Type => "BuyIngredient";
}

public sealed record CreateFormula(
    string Name,
    IReadOnlyList<FormulaComponent> Components,
    string ProcessId,
    int PriceCents,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] IReadOnlyList<string>? Claims = null
) : GameAction
{
    public override string Type => "CreateFormula";
}

public sealed record SetPrice(string FormulaId, int PriceCents) : GameAction
{
    public override string Type => "SetPrice";
}

/// <summary>Quietly change what goes in the bottle. Stock already brewed is unaffected.</summary>
public sealed record Reformulate(string FormulaId, IReadOnlyList<FormulaComponent> Components, string ProcessId) : GameAction
{
    public override string Type => "Reformulate";
}

public sealed record SetClaims(string FormulaId, IReadOnlyList<string> Claims) : GameAction
{
    public override string Type => "SetClaims";
}

public sealed record SetAdSpend(int Cents) : GameAction
{
    public override string Type => "SetAdSpend";
}

public sealed record BrewBatch(string FormulaId, int Bottles) : GameAction
{
    public override string Type => "BrewBatch";
}

public sealed record ResolveEvent(string InstanceId, string ChoiceId) : GameAction
{
    public override string Type => "ResolveEvent";
}

/// <summary>The outcome of an action: ok (maybe with the id of what it created), or an error message.</summary>
public sealed record ActionResult
{
    public bool Ok { get; init; }
    public string? Error { get; init; }
    public string? CreatedId { get; init; }

    public static readonly ActionResult Success = new() { Ok = true };
    public static ActionResult Created(string id) => new() { Ok = true, CreatedId = id };
    public static ActionResult Fail(string error) => new() { Ok = false, Error = error };
}

public static class ActionLimits
{
    public const int MaxPriceCents = 100_000;
    public const int MaxFormulaNameLength = 60;
    public const int MaxAdSpendCents = 100_000;
}

/// <summary>Ledger causes (core/causes.ts). Each needs a <c>cause.&lt;id&gt;</c> display string.</summary>
public static class Causes
{
    public const string SalesShop = "sales.shop";
    public const string SalesWalkIn = "sales.walk_in";
    public const string Rent = "expense.rent";
    public const string Ingredients = "expense.ingredients";
    public const string Bottling = "expense.bottling";
    public const string Advertising = "expense.advertising";
    public const string Bribe = "expense.bribe";
    public const string WordOfMouth = "body.word_of_mouth";
    public const string ReputationDrift = "body.reputation_drift";
    public const string Complaints = "body.complaints";
    public const string Death = "scrutiny.death";
    public const string Inquest = "scrutiny.inquest";
    public const string HeatDecay = "scrutiny.decay";
    public const string Claims = "advertising.claims";
    public const string Event = "event";
    public const string Timeline = "timeline";
    public const string Unlock = "unlock";

    public static readonly IReadOnlyList<string> All =
    [
        SalesShop, SalesWalkIn, Rent, Ingredients, Bottling, Advertising, Bribe, WordOfMouth,
        ReputationDrift, Complaints, Death, Inquest, HeatDecay, Claims, Event, Timeline, Unlock,
    ];
}

/// <summary>Game-date arithmetic (core/date.ts).</summary>
public static class Dates
{
    /// <summary>Months since year 0.</summary>
    public static int Index(GameDate d) => d.Year * 12 + (d.Month - 1);

    public static GameDate FromIndex(int i) => new(i / 12, i % 12 + 1);

    public static GameDate AddMonths(GameDate d, int months) => FromIndex(Index(d) + months);

    public static int Compare(GameDate a, GameDate b) => Index(a) - Index(b);

    public static int MonthsBetween(GameDate from, GameDate to) => Index(to) - Index(from);

    public static string Format(GameDate d) => $"{d.Year}-{d.Month:00}";
}
