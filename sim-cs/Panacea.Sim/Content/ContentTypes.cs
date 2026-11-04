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
                or "inquests" or "regulars" => new AtLeastRequirement(type, e.GetProperty("atLeast").GetDouble()),
            "date" => new DateRequirement(e.GetProperty("from").Deserialize<GameDate>(options)!),
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
}

public sealed class Names
{
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
}

public sealed class Background
{
    public required string Id { get; init; }
    public long StartingCashCents { get; init; }
    public OrderedMap<int> Capabilities { get; init; } = new();
    public IReadOnlyList<string> Flags { get; init; } = [];
}

public sealed class WalkInsConfig
{
    public int Base { get; init; }
    public double PerReputation { get; init; }
    public double ConversionChance { get; init; }
}

public sealed class MarketConfig
{
    public double WillingnessBase { get; init; }
    public double AppealPerWillingness { get; init; }
    public double WillingnessMax { get; init; }
    public double BottlesPerAttachment { get; init; }
    public double BudgetBase { get; init; }
    public double BudgetPerAttachment { get; init; }
    /// <summary>
    /// A regular can afford usual price × MeansPerMonth a month; spending more runs up a debt, and
    /// a regular who owes more than a month's means is ruined.
    /// </summary>
    public double MeansPerMonth { get; init; }
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
    public double HeatPerDeath { get; init; }
    public double HeatPerInfantDeath { get; init; }
    public double HeatPerAnonymousDeath { get; init; }
    public double HeatPerComplaint { get; init; }
    public double ShutdownHeat { get; init; }
    public int ShutdownGraceMonths { get; init; }
    public double InquestBaseChance { get; init; }
    public long BribeBaseCents { get; init; }
    /// <summary>Chance a bribe comes out: per bribe already paid, plus per point of notoriety, up to the max.</summary>
    public double BribeExposurePerBribe { get; init; }
    public double BribeExposurePerNotoriety { get; init; }
    public double BribeExposureMax { get; init; }
    public required IReadOnlyList<string> BenignVerdicts { get; init; }
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
}
