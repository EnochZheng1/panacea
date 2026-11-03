using System.Collections;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Panacea.Sim;

/// <summary>
/// A string-keyed map that iterates in insertion order, like a JavaScript object with
/// non-numeric keys (all game ids are lower_snake_case and start with a letter). Removing a key
/// and adding it again moves it to the end, as in JavaScript. Serialises as a JSON object.
/// .NET's Dictionary does not guarantee order after removals, so state uses this instead.
/// </summary>
[JsonConverter(typeof(OrderedMapConverterFactory))]
public sealed class OrderedMap<TValue> : IEnumerable<KeyValuePair<string, TValue>>, IReadOnlyDictionary<string, TValue>
{
    private readonly List<string> _keys = new();
    private readonly Dictionary<string, TValue> _values = new(StringComparer.Ordinal);

    public OrderedMap() { }

    public OrderedMap(IEnumerable<KeyValuePair<string, TValue>> items)
    {
        foreach (var kv in items) this[kv.Key] = kv.Value;
    }

    public int Count => _keys.Count;

    public TValue this[string key]
    {
        get => _values[key];
        set
        {
            if (!_values.ContainsKey(key)) _keys.Add(key);
            _values[key] = value;
        }
    }

    public IEnumerable<string> Keys => _keys;
    public IEnumerable<TValue> Values => _keys.Select(k => _values[k]);

    public bool ContainsKey(string key) => _values.ContainsKey(key);

    public bool TryGetValue(string key, [MaybeNullWhen(false)] out TValue value) => _values.TryGetValue(key, out value);

    /// <summary>The value, or <paramref name="fallback"/> when absent (JavaScript's <c>obj[k] ?? fallback</c>).</summary>
    public TValue GetOr(string key, TValue fallback) => _values.TryGetValue(key, out var v) ? v : fallback;

    public bool Remove(string key)
    {
        if (!_values.Remove(key)) return false;
        _keys.Remove(key);
        return true;
    }

    public OrderedMap<TValue> Clone() => new(this);

    public IEnumerator<KeyValuePair<string, TValue>> GetEnumerator()
    {
        foreach (var k in _keys) yield return new KeyValuePair<string, TValue>(k, _values[k]);
    }

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}

internal sealed class OrderedMapConverterFactory : JsonConverterFactory
{
    public override bool CanConvert(Type typeToConvert) =>
        typeToConvert.IsGenericType && typeToConvert.GetGenericTypeDefinition() == typeof(OrderedMap<>);

    public override JsonConverter CreateConverter(Type typeToConvert, JsonSerializerOptions options) =>
        (JsonConverter)Activator.CreateInstance(
            typeof(OrderedMapConverter<>).MakeGenericType(typeToConvert.GetGenericArguments()[0]))!;
}

internal sealed class OrderedMapConverter<TValue> : JsonConverter<OrderedMap<TValue>>
{
    public override OrderedMap<TValue> Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType != JsonTokenType.StartObject) throw new JsonException("expected an object");
        var map = new OrderedMap<TValue>();
        while (reader.Read())
        {
            if (reader.TokenType == JsonTokenType.EndObject) return map;
            string key = reader.GetString()!;
            reader.Read();
            map[key] = JsonSerializer.Deserialize<TValue>(ref reader, options)!;
        }
        throw new JsonException("unterminated object");
    }

    public override void Write(Utf8JsonWriter writer, OrderedMap<TValue> value, JsonSerializerOptions options)
    {
        writer.WriteStartObject();
        foreach (var kv in value)
        {
            writer.WritePropertyName(kv.Key);
            JsonSerializer.Serialize(writer, kv.Value, options);
        }
        writer.WriteEndObject();
    }
}
