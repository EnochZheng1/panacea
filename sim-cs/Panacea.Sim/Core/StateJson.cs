using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Panacea.Sim;

/// <summary>
/// JSON options that reproduce the TS state's shape: camelCase property names, explicit nulls,
/// optional fields omitted, and the "type"/"kind"-tagged unions. Doubles round-trip exactly.
/// Hashes and save files format numbers and strings JavaScript-style on top of this (JsJson).
/// </summary>
public static class StateJson
{
    public static readonly JsonSerializerOptions Options = Create();

    private static JsonSerializerOptions Create()
    {
        var o = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = JsonIgnoreCondition.Never,
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
            NumberHandling = JsonNumberHandling.Strict,
            WriteIndented = false,
        };
        o.Converters.Add(new TaggedUnionConverter<GameAction>("type", new()
        {
            ["BuyIngredient"] = typeof(BuyIngredient),
            ["CreateFormula"] = typeof(CreateFormula),
            ["SetPrice"] = typeof(SetPrice),
            ["Reformulate"] = typeof(Reformulate),
            ["SetClaims"] = typeof(SetClaims),
            ["SetAdSpend"] = typeof(SetAdSpend),
            ["BrewBatch"] = typeof(BrewBatch),
            ["ResolveEvent"] = typeof(ResolveEvent),
        }));
        o.Converters.Add(new TaggedUnionConverter<NewsItem>("kind", new()
        {
            ["timeline"] = typeof(TimelineNews),
            ["unlock"] = typeof(UnlockNews),
            ["event_resolved"] = typeof(EventResolvedNews),
            ["death"] = typeof(DeathNews),
            ["inquest"] = typeof(InquestNews),
            ["bribe_exposed"] = typeof(BribeExposedNews),
            ["customer_joined"] = typeof(CustomerJoinedNews),
            ["customer_left"] = typeof(CustomerLeftNews),
            ["bad_batch_caught"] = typeof(BadBatchCaughtNews),
            ["stock_seized"] = typeof(StockSeizedNews),
            ["ads_lapsed"] = typeof(AdsLapsedNews),
            ["shutdown_warning"] = typeof(ShutdownWarningNews),
        }));
        o.MakeReadOnly(populateMissingResolver: true);
        return o;
    }

    public static string Serialize<T>(T value) => JsonSerializer.Serialize(value, Options);

    public static T Deserialize<T>(string json) =>
        JsonSerializer.Deserialize<T>(json, Options) ?? throw new JsonException("unexpected null");

    public static JsonElement ToElement<T>(T value) => JsonSerializer.SerializeToElement(value, Options);

    /// <summary>Deep copy through JSON (every double round-trips exactly).</summary>
    public static T Clone<T>(T value) =>
        JsonSerializer.Deserialize<T>(JsonSerializer.SerializeToUtf8Bytes(value, Options), Options)!;
}

/// <summary>
/// A union whose JSON objects carry a string discriminator. The discriminator may appear
/// anywhere in the object (TS key order is whatever the caller wrote). The derived types expose
/// the discriminator as a read-only property, so they serialise it themselves.
/// </summary>
internal sealed class TaggedUnionConverter<TBase>(string discriminator, Dictionary<string, Type> types)
    : JsonConverter<TBase> where TBase : class
{
    public override bool CanConvert(Type typeToConvert) => typeToConvert == typeof(TBase);

    public override TBase Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        using var doc = JsonDocument.ParseValue(ref reader);
        var root = doc.RootElement;
        if (root.ValueKind != JsonValueKind.Object || !root.TryGetProperty(discriminator, out var tag) ||
            tag.ValueKind != JsonValueKind.String)
        {
            throw new JsonException($"{typeof(TBase).Name}: missing \"{discriminator}\"");
        }
        string name = tag.GetString()!;
        if (!types.TryGetValue(name, out var type))
            throw new JsonException($"{typeof(TBase).Name}: unknown {discriminator} \"{name}\"");
        return (TBase)root.Deserialize(type, options)!;
    }

    public override void Write(Utf8JsonWriter writer, TBase value, JsonSerializerOptions options) =>
        JsonSerializer.Serialize(writer, value, value.GetType(), options);
}
