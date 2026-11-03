using System.Globalization;
using System.Text;
using System.Text.Json;

namespace Panacea.Sim;

/// <summary>JavaScript-compatible JSON text: Number::toString and JSON.stringify string quoting.</summary>
public static class JsJson
{
    /// <summary>
    /// Formats a double exactly as JavaScript's <c>String(x)</c> / <c>JSON.stringify(x)</c> does:
    /// shortest round-trip digits, exponent forms like 1e+21 and 1e-7, integers without a decimal
    /// point, and -0 as "0".
    /// </summary>
    public static string FormatNumber(double x)
    {
        if (double.IsNaN(x)) return "NaN";
        if (x == 0) return "0";
        if (double.IsInfinity(x)) return x > 0 ? "Infinity" : "-Infinity";
        if (x < 0) return "-" + FormatNumber(-x);

        // Shortest round-trip digits from .NET ("R" is shortest round-trippable on .NET Core 3+).
        string r = x.ToString("R", CultureInfo.InvariantCulture);
        int exp = 0;
        int ePos = r.IndexOfAny(['E', 'e']);
        string mantissa = r;
        if (ePos >= 0)
        {
            exp = int.Parse(r.AsSpan(ePos + 1), NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture);
            mantissa = r[..ePos];
        }
        int point = mantissa.IndexOf('.');
        string digits = point >= 0 ? mantissa.Remove(point, 1) : mantissa;
        // value = 0.d1d2d3… × 10^n
        int n = (point >= 0 ? point : mantissa.Length) + exp;
        int lead = 0;
        while (lead < digits.Length - 1 && digits[lead] == '0') lead++;
        digits = digits[lead..];
        n -= lead;
        digits = digits.TrimEnd('0');
        if (digits.Length == 0) return "0";
        int k = digits.Length;

        var sb = new StringBuilder(32);
        if (k <= n && n <= 21)
        {
            sb.Append(digits).Append('0', n - k);
        }
        else if (0 < n && n <= 21)
        {
            sb.Append(digits, 0, n).Append('.').Append(digits, n, k - n);
        }
        else if (-6 < n && n <= 0)
        {
            sb.Append("0.").Append('0', -n).Append(digits);
        }
        else
        {
            int e = n - 1;
            sb.Append(digits[0]);
            if (k > 1) sb.Append('.').Append(digits, 1, k - 1);
            sb.Append('e').Append(e >= 0 ? '+' : '-').Append(Math.Abs(e).ToString(CultureInfo.InvariantCulture));
        }
        return sb.ToString();
    }

    /// <summary>Quotes a string exactly as JavaScript's JSON.stringify does (non-ASCII is not escaped).</summary>
    public static void Quote(StringBuilder sb, string s)
    {
        sb.Append('"');
        for (int i = 0; i < s.Length; i++)
        {
            char c = s[i];
            switch (c)
            {
                case '"': sb.Append("\\\""); break;
                case '\\': sb.Append("\\\\"); break;
                case '\b': sb.Append("\\b"); break;
                case '\f': sb.Append("\\f"); break;
                case '\n': sb.Append("\\n"); break;
                case '\r': sb.Append("\\r"); break;
                case '\t': sb.Append("\\t"); break;
                default:
                    if (c < 0x20)
                    {
                        AppendUnicodeEscape(sb, c);
                    }
                    else if (char.IsHighSurrogate(c))
                    {
                        if (i + 1 < s.Length && char.IsLowSurrogate(s[i + 1]))
                        {
                            sb.Append(c).Append(s[i + 1]);
                            i++;
                        }
                        else
                        {
                            AppendUnicodeEscape(sb, c);
                        }
                    }
                    else if (char.IsLowSurrogate(c))
                    {
                        AppendUnicodeEscape(sb, c);
                    }
                    else
                    {
                        sb.Append(c);
                    }
                    break;
            }
        }
        sb.Append('"');
    }

    public static string Quote(string s)
    {
        var sb = new StringBuilder(s.Length + 2);
        Quote(sb, s);
        return sb.ToString();
    }

    private static void AppendUnicodeEscape(StringBuilder sb, char c) =>
        sb.Append("\\u").Append(((int)c).ToString("x4", CultureInfo.InvariantCulture));

    /// <summary>JavaScript JSON.stringify of a parsed JSON value, keeping property order.</summary>
    public static string Stringify(JsonElement value)
    {
        var sb = new StringBuilder(4096);
        Write(sb, value, sortKeys: false);
        return sb.ToString();
    }

    /// <summary>Deterministic JSON with sorted keys: a port of core/hash.ts stableStringify.</summary>
    public static string StableStringify(JsonElement value)
    {
        var sb = new StringBuilder(4096);
        Write(sb, value, sortKeys: true);
        return sb.ToString();
    }

    internal static void Write(StringBuilder sb, JsonElement value, bool sortKeys)
    {
        switch (value.ValueKind)
        {
            case JsonValueKind.Object:
            {
                sb.Append('{');
                bool first = true;
                if (sortKeys)
                {
                    var props = new List<JsonProperty>();
                    foreach (var p in value.EnumerateObject()) props.Add(p);
                    props.Sort((a, b) => string.CompareOrdinal(a.Name, b.Name));
                    foreach (var p in props)
                    {
                        if (!first) sb.Append(',');
                        first = false;
                        Quote(sb, p.Name);
                        sb.Append(':');
                        Write(sb, p.Value, true);
                    }
                }
                else
                {
                    foreach (var p in value.EnumerateObject())
                    {
                        if (!first) sb.Append(',');
                        first = false;
                        Quote(sb, p.Name);
                        sb.Append(':');
                        Write(sb, p.Value, false);
                    }
                }
                sb.Append('}');
                break;
            }
            case JsonValueKind.Array:
            {
                sb.Append('[');
                bool first = true;
                foreach (var item in value.EnumerateArray())
                {
                    if (!first) sb.Append(',');
                    first = false;
                    Write(sb, item, sortKeys);
                }
                sb.Append(']');
                break;
            }
            case JsonValueKind.String:
                Quote(sb, value.GetString()!);
                break;
            case JsonValueKind.Number:
            {
                double d = value.GetDouble();
                if (!double.IsFinite(d)) throw new InvalidOperationException($"stableStringify: non-finite number {d}");
                sb.Append(FormatNumber(d));
                break;
            }
            case JsonValueKind.True:
                sb.Append("true");
                break;
            case JsonValueKind.False:
                sb.Append("false");
                break;
            case JsonValueKind.Null:
                sb.Append("null");
                break;
            default:
                throw new InvalidOperationException($"unexpected JSON value kind {value.ValueKind}");
        }
    }
}

/// <summary>Port of core/hash.ts: stable serialisation and the cyrb53 fingerprint.</summary>
public static class Hash
{
    /// <summary>Deterministic JSON of any state object (camelCase, sorted keys), as the TS sim produces.</summary>
    public static string StableStringify<T>(T value) =>
        JsJson.StableStringify(JsonSerializer.SerializeToElement(value, StateJson.Options));

    /// <summary>cyrb53 of a string (over UTF-16 code units), as 14 hex digits.</summary>
    public static string HashString(string str, uint seed = 0)
    {
        unchecked
        {
            uint h1 = 0xdeadbeefu ^ seed, h2 = 0x41c6ce57u ^ seed;
            for (int i = 0; i < str.Length; i++)
            {
                uint ch = str[i];
                h1 = (h1 ^ ch) * 2654435761u;
                h2 = (h2 ^ ch) * 1597334677u;
            }
            h1 = (h1 ^ (h1 >> 16)) * 2246822507u;
            h1 ^= (h2 ^ (h2 >> 13)) * 3266489909u;
            h2 = (h2 ^ (h2 >> 16)) * 2246822507u;
            h2 ^= (h1 ^ (h1 >> 13)) * 3266489909u;
            ulong v = 4294967296UL * (2097151u & h2) + h1;
            return v.ToString("x", CultureInfo.InvariantCulture).PadLeft(14, '0');
        }
    }

    /// <summary>Stable fingerprint of a value (e.g. a GameState): cyrb53 of its stable JSON.</summary>
    public static string HashValue<T>(T value) => HashString(StableStringify(value));

    public static string HashJson(JsonElement value) => HashString(JsJson.StableStringify(value));
}
