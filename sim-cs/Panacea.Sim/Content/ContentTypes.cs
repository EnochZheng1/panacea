using System.Text.Json;
using System.Text.Json.Serialization;

namespace Panacea.Sim;

// Typed content records: a port of packages/content/src/schemas.ts (without validation; the TS
// content tests guard the data). Defaults mirror the Zod `.default(...)` values.

/// <summary>A game month. Immutable.</summary>
public sealed record GameDate(int Year, int Month);

public static class DrugClasses
{
    public const string Alcohol = "alcohol";
    public const string Opiate = "opiate";
    public const string Cocaine = "cocaine";
    public static readonly IReadOnlyList<string> All = [Alcohol, Opiate, Cocaine];
}

/// <summary>Formula properties (per fluid ounce for ingredients, per bottle for formulas).</summary>
public sealed class Properties
{
    public static readonly IReadOnlyList<string> Keys =
        ["kick", "calm", "habit", "acuteToxicity", "accumulation", "bitterness", "detectability"];

    public double Kick { get; set; }
    public double Calm { get; set; }
    public double Habit { get; set; }
    public double AcuteToxicity { get; set; }
    public double Accumulation { get; set; }
    public double Bitterness { get; set; }
    public double Detectability { get; set; }

    [JsonIgnore]
    public double this[string key]
    {
        get => key switch
        {
            "kick" => Kick,
            "calm" => Calm,
            "habit" => Habit,
            "acuteToxicity" => AcuteToxicity,
            "accumulation" => Accumulation,
            "bitterness" => Bitterness,
            "detectability" => Detectability,
            _ => throw new KeyNotFoundException(key),
        };
        set
        {
            switch (key)
            {
                case "kick": Kick = value; break;
                case "calm": Calm = value; break;
                case "habit": Habit = value; break;
                case "acuteToxicity": AcuteToxicity = value; break;
                case "accumulation": Accumulation = value; break;
                case "bitterness": Bitterness = value; break;
                case "detectability": Detectability = value; break;
                default: throw new KeyNotFoundException(key);
            }
        }
    }

    public Properties Clone() => (Properties)MemberwiseClone();
}

// ---------------------------------------------------------------------------
// Requirements
// ---------------------------------------------------------------------------

[JsonConverter(typeof(RequirementConverter))]
public abstract record Requirement
{
    public abstract string Type { get; }
}

/// <summary>Requirements of the form "this number is at least X".</summary>
public sealed record AtLeastRequirement(string Kind, double AtLeast) : Requirement
{
    public override string Type => Kind;
}

public sealed record DateRequirement(GameDate From) : Requirement
{
    public override string Type => "date";
}

/// <summary>The month of the year is one of these (1–12): a letter that comes at Christmas or in July.</summary>
public sealed record SeasonRequirement(IReadOnlyList<int> Months) : Requirement
{
    public override string Type => "season";
}

/// <summary>
/// The letter <paramref name="Event"/> has come, and at least <paramref name="AtLeast"/> months ago. A
/// letter whose condition waits on another letter this way is a follow-up: it is drawn before fresh letters.
/// </summary>
public sealed record SinceRequirement(string Event, int AtLeast) : Requirement
{
    public override string Type => "since";
}

/// <summary>The shop's remedy (Requirements.ShopRemedy) assays at least this habit per bottle.</summary>
public sealed record RemedyHabitRequirement(double AtLeast) : Requirement
{
    public override string Type => "remedyHabit";
}

/// <summary>The shop's remedy holds at least this many fluid ounces per bottle of one drug class.</summary>
public sealed record RemedyDrugRequirement(string DrugClass, double AtLeastOz) : Requirement
{
    public override string Type => "remedyDrug";
}

/// <summary>The shop's remedy carries this claim on its label.</summary>
public sealed record RemedyClaimRequirement(string Id) : Requirement
{
    public override string Type => "remedyClaim";
}

/// <summary>The shop's remedy is priced at this many cents or more.</summary>
public sealed record RemedyPriceRequirement(int AtLeastCents) : Requirement
{
    public override string Type => "remedyPrice";
}

public sealed record FlagRequirement(string Id) : Requirement
{
    public override string Type => "flag";
}

public sealed record UnlockedRequirement(string Id) : Requirement
{
    public override string Type => "unlocked";
}

public sealed record AllRequirement(IReadOnlyList<Requirement> Of) : Requirement
{
    public override string Type => "all";
}

public sealed record AnyRequirement(IReadOnlyList<Requirement> Of) : Requirement
{
    public override string Type => "any";
}

public sealed record NotRequirement(Requirement Requirement) : Requirement
{
    public override string Type => "not";
}

internal sealed class RequirementConverter : JsonConverter<Requirement>
{
    public override Requirement Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        using var doc = JsonDocument.ParseValue(ref reader);
        return Parse(doc.RootElement, options);
    }

    private static Requirement Parse(JsonElement e, JsonSerializerOptions options)
    {
        string type = e.GetProperty("type").GetString()!;
        return type switch
        {
            "cash" => new AtLeastRequirement(type, e.GetProperty("atLeastCents").GetDouble()),
            "reputation" or "notoriety" or "bottlesSold" or "batchesBrewed" or "heat" or "deaths"
                or "inquests" or "regulars" or "months" or "remedies" => new AtLeastRequirement(type, e.GetProperty("atLeast").GetDouble()),
            "date" => new DateRequirement(e.GetProperty("from").Deserialize<GameDate>(options)!),
            "season" => new SeasonRequirement(e.GetProperty("months").EnumerateArray().Select(x => x.GetInt32()).ToList()),
            "since" => new SinceRequirement(e.GetProperty("event").GetString()!, e.GetProperty("atLeast").GetInt32()),
            "remedyHabit" => new RemedyHabitRequirement(e.GetProperty("atLeast").GetDouble()),
            "remedyDrug" => new RemedyDrugRequirement(e.GetProperty("drugClass").GetString()!, e.GetProperty("atLeastOz").GetDouble()),
            "remedyClaim" => new RemedyClaimRequirement(e.GetProperty("id").GetString()!),
            "remedyPrice" => new RemedyPriceRequirement(e.GetProperty("atLeastCents").GetInt32()),
            "flag" => new FlagRequirement(e.GetProperty("id").GetString()!),
            "unlocked" => new UnlockedRequirement(e.GetProperty("id").GetString()!),
            "all" => new AllRequirement(e.GetProperty("of").EnumerateArray().Select(x => Parse(x, options)).ToList()),
            "any" => new AnyRequirement(e.GetProperty("of").EnumerateArray().Select(x => Parse(x, options)).ToList()),
            "not" => new NotRequirement(Parse(e.GetProperty("requirement"), options)),
            _ => throw new JsonException($"unknown requirement type \"{type}\""),
        };
    }

    public override void Write(Utf8JsonWriter writer, Requirement value, JsonSerializerOptions options)
    {
        writer.WriteStartObject();
        writer.WriteString("type", value.Type);
        switch (value)
        {
            case AtLeastRequirement a:
                writer.WriteNumber(a.Kind == "cash" ? "atLeastCents" : "atLeast", a.AtLeast);
                break;
            case DateRequirement d:
                writer.WritePropertyName("from");
                JsonSerializer.Serialize(writer, d.From, options);
                break;
            case SeasonRequirement season:
                writer.WritePropertyName("months");
                JsonSerializer.Serialize(writer, season.Months, options);
                break;
            case SinceRequirement since:
                writer.WriteString("event", since.Event);
                writer.WriteNumber("atLeast", since.AtLeast);
                break;
            case RemedyHabitRequirement rh:
                writer.WriteNumber("atLeast", rh.AtLeast);
                break;
            case RemedyDrugRequirement rd:
                writer.WriteString("drugClass", rd.DrugClass);
                writer.WriteNumber("atLeastOz", rd.AtLeastOz);
                break;
            case RemedyClaimRequirement rc:
                writer.WriteString("id", rc.Id);
                break;
            case RemedyPriceRequirement rp:
                writer.WriteNumber("atLeastCents", rp.AtLeastCents);
                break;
            case FlagRequirement f:
                writer.WriteString("id", f.Id);
                break;
            case UnlockedRequirement u:
                writer.WriteString("id", u.Id);
                break;
            case AllRequirement all:
                writer.WritePropertyName("of");
                JsonSerializer.Serialize(writer, all.Of, options);
                break;
            case AnyRequirement any:
                writer.WritePropertyName("of");
                JsonSerializer.Serialize(writer, any.Of, options);
                break;
            case NotRequirement not:
                writer.WritePropertyName("requirement");
                JsonSerializer.Serialize(writer, not.Requirement, options);
                break;
        }
        writer.WriteEndObject();
    }
}

// ---------------------------------------------------------------------------
// Effects
// ---------------------------------------------------------------------------

[JsonConverter(typeof(EffectConverter))]
public abstract record Effect
{
    public abstract string Type { get; }
}

public sealed record CashEffect(long DeltaCents) : Effect
{
    public override string Type => "cash";
}

/// <summary>A change to reputation, notoriety or heat (<see cref="Metric"/>).</summary>
public sealed record MetricEffect(string Metric, double Delta) : Effect
{
    public override string Type => Metric;
}

public sealed record SetFlagEffect(string Id) : Effect
{
    public override string Type => "setFlag";
}

public sealed record ClearFlagEffect(string Id) : Effect
{
    public override string Type => "clearFlag";
}

/// <summary>Adds to a capability; with <see cref="Months"/>, only for that many month-ends, then it reverts.</summary>
public sealed record CapabilityEffect(string Id, int Delta, int? Months = null) : Effect
{
    public override string Type => "capability";
}

public sealed record SeizeStockEffect : Effect
{
    public override string Type => "seizeStock";
}

/// <summary>
/// Nudges the ward's memory of harm (0–100; see <c>WardSystem</c>): a sermon or a kind notice that
/// helps the street forget, a story that reminds it.
/// </summary>
public sealed record WardMemoryEffect(double Delta) : Effect
{
    public override string Type => "wardMemory";
}

/// <summary>Resolves the coroner's inquest attached to a system event: testify, silent or bribe.</summary>
public sealed record InquestEffect(string Approach) : Effect
{
    public override string Type => "inquest";
}

internal sealed class EffectConverter : JsonConverter<Effect>
{
    public override Effect Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        using var doc = JsonDocument.ParseValue(ref reader);
        var e = doc.RootElement;
        string type = e.GetProperty("type").GetString()!;
        return type switch
        {
            "cash" => new CashEffect(e.GetProperty("deltaCents").GetInt64()),
            "reputation" or "notoriety" or "heat" => new MetricEffect(type, e.GetProperty("delta").GetDouble()),
            "setFlag" => new SetFlagEffect(e.GetProperty("id").GetString()!),
            "clearFlag" => new ClearFlagEffect(e.GetProperty("id").GetString()!),
            "capability" => new CapabilityEffect(e.GetProperty("id").GetString()!, e.GetProperty("delta").GetInt32(),
                e.TryGetProperty("months", out var months) ? months.GetInt32() : null),
            "seizeStock" => new SeizeStockEffect(),
            "wardMemory" => new WardMemoryEffect(e.GetProperty("delta").GetDouble()),
            "inquest" => new InquestEffect(e.GetProperty("approach").GetString()!),
            _ => throw new JsonException($"unknown effect type \"{type}\""),
        };
    }

    public override void Write(Utf8JsonWriter writer, Effect value, JsonSerializerOptions options)
    {
        writer.WriteStartObject();
        writer.WriteString("type", value.Type);
        switch (value)
        {
            case CashEffect c: writer.WriteNumber("deltaCents", c.DeltaCents); break;
            case MetricEffect m: writer.WriteNumber("delta", m.Delta); break;
            case SetFlagEffect s: writer.WriteString("id", s.Id); break;
            case ClearFlagEffect c: writer.WriteString("id", c.Id); break;
            case CapabilityEffect c:
                writer.WriteString("id", c.Id);
                writer.WriteNumber("delta", c.Delta);
                if (c.Months is int months) writer.WriteNumber("months", months);
                break;
            case WardMemoryEffect w: writer.WriteNumber("delta", w.Delta); break;
            case InquestEffect i: writer.WriteString("approach", i.Approach); break;
        }
        writer.WriteEndObject();
    }
}

// ---------------------------------------------------------------------------
// Entities
// ---------------------------------------------------------------------------

public sealed class Appearance
{
    public string Color { get; init; } = "#d8d0c0";
    public double Opacity { get; init; } = 0.2;
}

public sealed class Ingredient
{
    public required string Id { get; init; }
    public required string SubstanceClass { get; init; }
    public string? DrugClass { get; init; }
    public required Properties Properties { get; init; }
    public double CostPerUnitCents { get; init; }
    public GameDate? AvailableFrom { get; init; }
    public IReadOnlyList<string> Tags { get; init; } = [];
    public Appearance Appearance { get; init; } = new();
    public string? Note { get; init; }
}

public sealed class Process
{
    public required string Id { get; init; }
    /// <summary>Partial: properties without a multiplier are unchanged (×1).</summary>
    public IReadOnlyDictionary<string, double> Multipliers { get; init; } = new Dictionary<string, double>();
    public int ExtraCostPerBottleCents { get; init; }
    public string? RequiresUnlock { get; init; }

    public double Multiplier(string key) => Multipliers.TryGetValue(key, out var m) ? m : 1;
}

public sealed class Unlock
{
    public required string Id { get; init; }
    public required string Category { get; init; }
    public required Requirement Requirement { get; init; }
    public IReadOnlyList<Effect> Effects { get; init; } = [];
}

public sealed class EventChoice
{
    public required string Id { get; init; }
    public IReadOnlyList<Effect> Effects { get; init; } = [];
    public Requirement? Requires { get; init; }
    public bool Default { get; init; }
    /// <summary>
    /// Where the reply's cash effects are booked (schemas.ts LEDGER_TAGS): "outlay", "supplies",
    /// "fees", "subscriptions" or "trade"; null for "letters answered" (Progression.LetterLedger).
    /// </summary>
    public string? Ledger { get; init; }
}

public sealed class GameEvent
{
    public required string Id { get; init; }
    /// <summary>"random" or "system".</summary>
    public string Trigger { get; init; } = "random";
    public Requirement? Condition { get; init; }
    public double Chance { get; init; }
    public bool Once { get; init; }
    public int CooldownMonths { get; init; }
    public required IReadOnlyList<EventChoice> Choices { get; init; }
}

public sealed class TimelineEntry
{
    public required string Id { get; init; }
    public required GameDate Date { get; init; }
    public bool ApproximateDate { get; init; }
    public IReadOnlyList<Effect> Effects { get; init; } = [];
    public string? Note { get; init; }
}

public sealed class Preference
{
    public double Kick { get; set; }
    public double Calm { get; set; }
}

public sealed class BuyerProfile
{
    public int BaseWillingnessCents { get; set; }
    public double BuyChance { get; set; }
    public Preference Preference { get; set; } = new();
    public double BitternessTolerance { get; set; }
    public double BitternessAversion { get; set; }

    public BuyerProfile Clone() => new()
    {
        BaseWillingnessCents = BaseWillingnessCents,
        BuyChance = BuyChance,
        Preference = new Preference { Kick = Preference.Kick, Calm = Preference.Calm },
        BitternessTolerance = BitternessTolerance,
        BitternessAversion = BitternessAversion,
    };
}

public sealed class Ailment
{
    public required string Id { get; init; }
    public double Severity { get; init; }
    public double HealthDriftPerMonth { get; init; }
    /// <summary>[min, max] months, or null if the complaint never clears by itself.</summary>
    public IReadOnlyList<int>? ResolvesAfterMonths { get; init; }
    /// <summary>"self" or "infant".</summary>
    public string Consumer { get; init; } = "self";
    public double MortalityMultiplier { get; init; } = 1;
}

public sealed class SegmentAilment
{
    public required string Id { get; init; }
    public double Weight { get; init; }
}

public sealed class Segment
{
    public required string Id { get; init; }
    public double Weight { get; init; }
    public double Vulnerability { get; init; }
    public double BaseHealth { get; init; }
    public double NaturalHazard { get; init; }
    /// <summary>"female", "male" or "any".</summary>
    public required string Gender { get; init; }
    public required IReadOnlyList<string> Titles { get; init; }
    public required IReadOnlyList<SegmentAilment> Ailments { get; init; }
    public required BuyerProfile Profile { get; init; }
    /// <summary>
    /// The share of these households wary of a remedy with a narcotic in it (the temperance and
    /// chapel households, the mothers who have read of soothing syrups, the respectable trade).
    /// </summary>
    public double Wary { get; init; }
}

public sealed class Names
{
    public required IReadOnlyList<NameOrigin> Origins { get; init; }
}

/// <summary>One stock of names: given names are only paired with its own surnames.</summary>
public sealed class NameOrigin
{
    public required string Id { get; init; }
    public required double Weight { get; init; }
    public required IReadOnlyList<string> Female { get; init; }
    public required IReadOnlyList<string> Male { get; init; }
    public required IReadOnlyList<string> Surnames { get; init; }
}

public sealed class Claim
{
    public required string Id { get; init; }
    public IReadOnlyList<string> Ailments { get; init; } = [];
    public double Appeal { get; init; }
    public double Boldness { get; init; }
    public string? RequiresUnlock { get; init; }
}

public sealed class Regular
{
    public required string Id { get; init; }
    public required string DisplayName { get; init; }
    public required string Segment { get; init; }
    public required string Ailment { get; init; }
    public required BuyerProfile Profile { get; init; }
    /// <summary>Wary of narcotics (see <see cref="Segment.Wary"/>).</summary>
    public bool Wary { get; init; }
}

public sealed class Background
{
    public required string Id { get; init; }
    public long StartingCashCents { get; init; }
    public OrderedMap<int> Capabilities { get; init; } = new();
    public IReadOnlyList<string> Flags { get; init; } = [];
}

/// <summary>
/// Orders from away (schemas.ts OrdersSchema): druggists out of town under contract
/// (InvestmentSchema.wholesale), or readers who send by post (InvestmentSchema.postOrders).
/// </summary>
public sealed class WholesaleSpec
{
    /// <summary>Bottles a month, × (1 + reputation × PerReputation).</summary>
    public int BottlesPerMonth { get; init; }
    public double PerReputation { get; init; }
    /// <summary>They pay this share of the shelf price, in per cent…</summary>
    public double PricePct { get; init; }
    /// <summary>…but at most this much a bottle.</summary>
    public int MaxPriceCents { get; init; }
    /// <summary>Health of the strangers they sell to (no tolerance): a strong bottle kills some.</summary>
    public double StrangerHealth { get; init; }
    /// <summary>The shelf price at which they order BottlesPerMonth…</summary>
    public int RefPriceCents { get; init; } = 60;
    /// <summary>…and the order's factor, 1 + PriceElasticity × (RefPriceCents − price) / RefPriceCents…</summary>
    public double PriceElasticity { get; init; }
    /// <summary>…between 0 and this.</summary>
    public double MaxOrderBoost { get; init; } = 1;
}

/// <summary>A monthly chance of an accident while an investment is held.</summary>
public sealed class MishapSpec
{
    public double ChancePerMonth { get; init; }
    public required IReadOnlyList<Effect> Effects { get; init; }
}

/// <summary>
/// Something the proprietor buys that pays back over months or years (schemas.ts
/// InvestmentSchema): a branch shop, an almanac, a wholesale contract, a laboratory.
/// </summary>
public sealed class Investment
{
    public required string Id { get; init; }
    /// <summary>Paid once, when bought (and again when a lapsed one is renewed).</summary>
    public long CostCents { get; init; }
    /// <summary>Paid at every month-end while held.</summary>
    public long UpkeepCents { get; init; }
    /// <summary>Runs this many month-ends from the month bought, then lapses; null = for good.</summary>
    public int? Months { get; init; }
    /// <summary>Offered only while this holds.</summary>
    public Requirement? Requires { get; init; }
    /// <summary>Checked at every month-end: if it holds, the investment is lost.</summary>
    public Requirement? LostWhen { get; init; }
    /// <summary>Extra callers a month while held.</summary>
    public int WalkInsPerMonth { get; init; }
    /// <summary>Heat a month per point of label boldness while held.</summary>
    public double HeatPerBoldness { get; init; }
    public WholesaleSpec? Wholesale { get; init; }
    /// <summary>Readers who send for a bottle by post (an almanac's), at the shelf price.</summary>
    public WholesaleSpec? PostOrders { get; init; }
    public MishapSpec? Mishap { get; init; }
    /// <summary>Applied when bought; capability changes are undone when it lapses or is lost.</summary>
    public IReadOnlyList<Effect> Effects { get; init; } = [];
}

public sealed class WalkInsConfig
{
    public int Base { get; init; }
    public double PerReputation { get; init; }
    public double ConversionChance { get; init; }
    /// <summary>
    /// Word of a strong bottle brings callers of its own ("word"): ⌊WordPerHabit ×
    /// clamp(habit − WordHabitFloor, 0, WordHabitCap)⌋ a month, by the most habit-forming bottle on the shelf.
    /// </summary>
    public double WordPerHabit { get; init; }
    public double WordHabitFloor { get; init; }
    public double WordHabitCap { get; init; }
    /// <summary>
    /// The ward is so many households of each kind (by segment weight): a shop keeps at most
    /// WardShareSlack × its capacity × the kind's share of the ward of any one kind. 0: no limit.
    /// </summary>
    public double WardShareSlack { get; init; }
    /// <summary>
    /// While the shelf holds a bottle the careful households trust (no narcotic, a modest label),
    /// each wary regular pleased with the shop sends CarefulPerWary callers a month (rounded down),
    /// all wary ("careful").
    /// </summary>
    public double CarefulPerWary { get; init; }
}

public sealed class MarketConfig
{
    public double WillingnessBase { get; init; }
    public double AppealPerWillingness { get; init; }
    public double WillingnessMax { get; init; }
    public double BottlesPerAttachment { get; init; }
    /// <summary>
    /// A craving regular's attachment for the extra bottles is dependency × (BottlesPerAttachment +
    /// BottlesPerTolerance × total tolerance above ToleranceFloor), at most HabitBottlesMax extra.
    /// </summary>
    public double BottlesPerTolerance { get; init; }
    public double ToleranceFloor { get; init; }
    /// <summary>
    /// A regular who craves a strong bottle pays over its price, up to CravingPremium × dependency of
    /// it, while they owe less than PremiumDebtMonths months' means: in full for a bottle whose habit
    /// is PremiumHabitFloor + PremiumHabitSpan or more, nothing at PremiumHabitFloor or less.
    /// </summary>
    public double CravingPremium { get; init; }
    public double PremiumDebtMonths { get; init; }
    public double PremiumHabitFloor { get; init; }
    public double PremiumHabitSpan { get; init; }
    public int HabitBottlesMax { get; init; }
    public double BudgetBase { get; init; }
    public double BudgetPerAttachment { get; init; }
    /// <summary>
    /// A regular can afford usual price × MeansPerMonth a month; spending more runs up a debt, and
    /// a regular who owes more than a month's means is ruined.
    /// </summary>
    public double MeansPerMonth { get; init; }
    /// <summary>A regular who owes more than RuinMonths months' means is ruined.</summary>
    public double RuinMonths { get; init; }
    /// <summary>The habit eats the means: they are multiplied by 1 − MeansLostToHabit × dependency.</summary>
    public double MeansLostToHabit { get; init; }
    /// <summary>
    /// A walk-in hesitates over a dear bottle: their chance to buy is multiplied by
    /// 1 − PriceHesitation × clamp(1 − headroom / HesitationMargin, 0, 1), where headroom is
    /// 1 − price / willingness to pay.
    /// </summary>
    public double PriceHesitation { get; init; }
    public double HesitationMargin { get; init; }
    /// <summary>
    /// A regular makes the last bottle do when it costs more than they can comfortably spend on one,
    /// DearAbove × their usual price (twice that for a bottle whose narcotics make a habit of
    /// DearHabitHold or more): above it they buy that month with chance
    /// (comfortable / price) ^ DearElasticity.
    /// </summary>
    public double DearAbove { get; init; }
    public double DearElasticity { get; init; }
    public double DearHabitHold { get; init; }
    /// <summary>
    /// A wary buyer will not take a bottle whose opiates and cocaine make a habit of WaryHabit or
    /// more, and finds a bottle with no opiate or coca in it and a label no bolder than WaryBoldness
    /// worth CleanAppeal more, as a claim on its label would be.
    /// </summary>
    public double WaryHabit { get; init; }
    public double CleanAppeal { get; init; }
    public double WaryBoldness { get; init; }
    /// <summary>
    /// Orders from away are filled after the regulars have had theirs, from at most this share of
    /// what is left on the shelf; the walk-ins get the rest.
    /// </summary>
    public double OrdersShelfShare { get; init; }
}

public sealed class AdvertisingConfig
{
    public double VisitorsPerSqrtCent { get; init; }
    public double HeatPerBoldness { get; init; }
    public double LabelHeatPerBoldness { get; init; }
}

public sealed class BatchesConfig
{
    public double PotencySpread { get; init; }
    public double BadBatchChance { get; init; }
    public required IReadOnlyList<double> BadBatchPotency { get; init; }
}

public sealed class BodyConfig
{
    public double ToleranceGain { get; init; }
    public double ToleranceDecay { get; init; }
    public double ToleranceProtection { get; init; }
    public double DependencyGain { get; init; }
    public double DependencyDecay { get; init; }
    public double WithdrawalThreshold { get; init; }
    public double WithdrawalHarm { get; init; }
    /// <summary>
    /// A bottle whose habit is under HabitFloor (the assay's "none") makes a taste for it, not a
    /// craving: a month of such bottles builds dependency up to MildDependencyCap and no further.
    /// </summary>
    public double HabitFloor { get; init; }
    public double MildDependencyCap { get; init; }
    public double LoadDecay { get; init; }
    public double LoadSafe { get; init; }
    public double LoadHarm { get; init; }
    public double AcuteHarm { get; init; }
    public double Recovery { get; init; }
    public double LapseChance { get; init; }
    public double InfantVulnerability { get; init; }
    public double InfantDoseScale { get; init; }
}

public sealed class ScrutinyConfig
{
    public double HeatDecayFlat { get; init; }
    public double HeatDecayRate { get; init; }
    /// <summary>Extra decay at high heat: HeatDecayHigh × heat × heat / 100 a month.</summary>
    public double HeatDecayHigh { get; init; }
    public double HeatPerDeath { get; init; }
    public double HeatPerInfantDeath { get; init; }
    public double HeatPerAnonymousDeath { get; init; }
    public double HeatPerComplaint { get; init; }
    public double ShutdownHeat { get; init; }
    /// <summary>A letter's correspondent sends this much fewer callers at the heat that shuts the shop
    /// (more than 1: nobody, some way before it). Also scales the standing a reply to a letter earns.</summary>
    public double LetterCallerLoss { get; init; }
    public int ShutdownGraceMonths { get; init; }
    public double InquestBaseChance { get; init; }
    public long BribeBaseCents { get; init; }
    /// <summary>Each bribe already paid raises the fee by this share of the base.</summary>
    public double BribeRisePerBribe { get; init; }
    /// <summary>Whatever the heat, every fee is at least this much more than the last one paid.</summary>
    public long BribeMinRiseCents { get; init; }
    /// <summary>The fee is multiplied by 1 + heat / BribeHeatDoubling: a notorious case costs more.</summary>
    public double BribeHeatDoubling { get; init; }
    /// <summary>
    /// Chance a bribe comes out: a base chance, plus per bribe already paid, plus per point of
    /// notoriety, up to the max.
    /// </summary>
    public double BribeExposureBase { get; init; }
    public double BribeExposurePerBribe { get; init; }
    public double BribeExposurePerNotoriety { get; init; }
    public double BribeExposureMax { get; init; }
    public required IReadOnlyList<string> BenignVerdicts { get; init; }
    /// <summary>What a bought verdict does to the heat (0: the envelope stops the talk rising, no more).</summary>
    public double BribedVerdictHeat { get; init; }
}

/// <summary>
/// The ward's memory of harm (0..100), apart from the heat: what the neighbours say. Each month-end
/// it fades to (1 − MemoryDecay) of itself, less MemoryDecayFlat, and the month's harms are added.
/// </summary>
public sealed class WardConfig
{
    public double MemoryDecay { get; init; }
    public double MemoryDecayFlat { get; init; }
    /// <summary>A regular's death the remedy caused, that looked it (an overdose or a wasting).</summary>
    public double PerDeath { get; init; }
    public double PerInfantDeath { get; init; }
    /// <summary>A stranger found dead with the bottle (a walk-in, or a buyer by post).</summary>
    public double PerStrangerDeath { get; init; }
    /// <summary>A death that looked natural, but the remedy hastened.</summary>
    public double PerHastenedDeath { get; init; }
    /// <summary>Added for a death from a mis-measured batch.</summary>
    public double PerBadBatchDeath { get; init; }
    /// <summary>An inquest whose jury named the remedy.</summary>
    public double PerVerdict { get; init; }
    /// <summary>Added when the verdict came of a bribe found out.</summary>
    public double PerExposedBribe { get; init; }
    /// <summary>The Board of Health carried the bottles away.</summary>
    public double PerSeizure { get; init; }
    /// <summary>At a memory of 100, strangers at the counter buy this much less often…</summary>
    public double WalkInLoss { get; init; }
    /// <summary>…come back as regulars this much less often…</summary>
    public double ConversionLoss { get; init; }
    /// <summary>…and an undependent regular leaves with this chance a month. Scales with the memory.</summary>
    public double LeaveChance { get; init; }
    /// <summary>At a memory of 100, a letter's correspondent sends this much fewer callers ("callers_*"),
    /// and a reply to a letter earns this much less standing.</summary>
    public double LetterCallerLoss { get; init; }
    /// <summary>The months the observation counts what the ward remembers over.</summary>
    public int RememberMonths { get; init; }
}

public sealed class Config
{
    public required string ContentVersion { get; init; }
    public required GameDate StartDate { get; init; }
    public required GameDate EndDate { get; init; }
    public required string DefaultBackground { get; init; }
    public double StartingReputation { get; init; }
    public double ReputationBaseline { get; init; }
    public double ReputationDrift { get; init; }
    public long RentCents { get; init; }
    public int BottleCostCents { get; init; }
    public double BottleCapacity { get; init; }
    /// <summary>
    /// The most bottles one still makes in a batch. A capability "still_bottles" (a larger copper)
    /// adds to it, for every still.
    /// </summary>
    public int StillBottles { get; init; }
    public int MaxFormulas { get; init; }
    public int MaxEventsPerMonth { get; init; }
    public int BankruptcyGraceMonths { get; init; }
    public int MaxRegulars { get; init; }
    public int MaxClaimsPerFormula { get; init; }
    public required WalkInsConfig WalkIns { get; init; }
    public required MarketConfig Market { get; init; }
    public required AdvertisingConfig Advertising { get; init; }
    public required BatchesConfig Batches { get; init; }
    public required BodyConfig Body { get; init; }
    public required ScrutinyConfig Scrutiny { get; init; }
    public required WardConfig Ward { get; init; }
}
