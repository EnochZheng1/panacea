using System.Text.Json;
using Xunit;

namespace Panacea.Sim.Tests;

/// <summary>
/// Low-level parity vectors recorded from the TS sim (fixtures/vectors.json.gz): RNG sequences,
/// dmath results, Number-to-string formatting, JSON string escaping and cyrb53 hashes.
/// </summary>
public class VectorTests
{
    private static readonly Lazy<JsonDocument> Doc = new(() => Fixtures.ReadJson("vectors.json.gz"));
    private static JsonElement V => Doc.Value.RootElement;

    [Fact]
    public void RngSequencesMatch()
    {
        int checkedSeeds = 0;
        foreach (var v in V.GetProperty("rng").EnumerateArray())
        {
            string seed = v.GetProperty("seed").GetString()!;
            var expected = v.GetProperty("state").Deserialize<RngState>(StateJson.Options)!;
            Assert.Equal(expected, Rng.SeedRng(seed));
            var r = Rng.FromSeed(seed);
            foreach (var u in v.GetProperty("u32").EnumerateArray()) Assert.Equal(u.GetUInt32(), r.NextU32());
            foreach (var f in v.GetProperty("floats").EnumerateArray()) Assert.Equal(f.GetString(), Fixtures.Bits(r.Float()));
            int i = 0;
            foreach (var n in v.GetProperty("ints").EnumerateArray())
            {
                Assert.Equal(n.GetInt32(), r.Int(-i * 7, i * 13));
                i++;
            }
            foreach (var g in v.GetProperty("gaussians").EnumerateArray()) Assert.Equal(g.GetString(), Fixtures.Bits(Rng.Gaussian(r)));
            Assert.Equal(v.GetProperty("end").Deserialize<RngState>(StateJson.Options), r.State());
            checkedSeeds++;
        }
        Assert.True(checkedSeeds >= 5);
    }

    [Fact]
    public void DMathMatchesBitForBit()
    {
        int n = 0;
        foreach (var v in V.GetProperty("dmath").EnumerateArray())
        {
            double x = Fixtures.FromBits(v.GetProperty("x").GetString()!);
            string fn = v.GetProperty("fn").GetString()!;
            double y = fn switch
            {
                "log" => DMath.Log(x),
                "cos" => DMath.Cos(x),
                "tanh" => DMath.Tanh(x),
                "exp" => DMath.Exp(x),
                _ => throw new InvalidOperationException(fn),
            };
            Assert.True(v.GetProperty("y").GetString() == Fixtures.Bits(y), $"{fn}({x:R}) = {y:R}, expected bits {v.GetProperty("y").GetString()}");
            n++;
        }
        Assert.True(n > 2000);
    }

    [Fact]
    public void DMathIsAccurate()
    {
        var r = Rng.FromSeed("accuracy");
        for (int i = 0; i < 20_000; i++)
        {
            double u = Math.Max(r.Float(), 1e-12);
            Assert.True(Math.Abs(DMath.Log(u) - Math.Log(u)) <= 1e-14 * Math.Abs(Math.Log(u)) + 1e-300);
            double x = 2 * Math.PI * r.Float();
            Assert.True(Math.Abs(DMath.Cos(x) - Math.Cos(x)) < 1e-15);
            double t = (r.Float() - 0.5) * 40;
            Assert.True(Math.Abs(DMath.Tanh(t) - Math.Tanh(t)) <= 1e-14 * Math.Abs(Math.Tanh(t)));
        }
    }

    [Fact]
    public void NumbersFormatLikeJavaScript()
    {
        int n = 0;
        foreach (var v in V.GetProperty("numbers").EnumerateArray())
        {
            double x = Fixtures.FromBits(v.GetProperty("bits").GetString()!);
            Assert.Equal(v.GetProperty("js").GetString(), JsJson.FormatNumber(x));
            n++;
        }
        Assert.True(n > 5000);
        Assert.Equal("0", JsJson.FormatNumber(-0.0));
        Assert.Equal("1e+21", JsJson.FormatNumber(1e21));
        Assert.Equal("100000000000000000000", JsJson.FormatNumber(1e20));
        Assert.Equal("1e-7", JsJson.FormatNumber(1e-7));
        Assert.Equal("0.000001", JsJson.FormatNumber(1e-6));
        Assert.Equal("-2.5", JsJson.FormatNumber(-2.5));
    }

    [Fact]
    public void StringsQuoteAndHashLikeJavaScript()
    {
        foreach (var v in V.GetProperty("strings").EnumerateArray())
        {
            var units = v.GetProperty("units").EnumerateArray().Select(u => (char)u.GetInt32()).ToArray();
            string s = new(units);
            Assert.Equal(v.GetProperty("json").GetString(), JsJson.Quote(s));
            Assert.Equal(v.GetProperty("hash").GetString(), Hash.HashString(JsJson.Quote(s)));
        }
    }

    [Fact]
    public void StableStringifySortsKeys()
    {
        foreach (var v in V.GetProperty("stableStringify").EnumerateArray())
        {
            string json = JsJson.StableStringify(v.GetProperty("value"));
            Assert.Equal(v.GetProperty("json").GetString(), json);
            Assert.Equal(v.GetProperty("hash").GetString(), Hash.HashString(json));
        }
    }

    [Fact]
    public void TheSimulationUsesOnlyPortableMath()
    {
        // Engine-dependent functions would break bit-for-bit parity with the TS sim.
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null && !File.Exists(Path.Combine(dir.FullName, "Panacea.Sim", "Panacea.Sim.csproj"))) dir = dir.Parent;
        Assert.NotNull(dir);
        var forbidden = new System.Text.RegularExpressions.Regex(
            @"\bMath(F)?\.(Log|Log2|Log10|Exp|Pow|Sin|Cos|Tan|Asin|Acos|Atan|Atan2|Sinh|Cosh|Tanh|Cbrt|Round|FusedMultiplyAdd)\b|\bRandom\b|CultureInfo\.CurrentCulture|string\.Compare\(|\.CompareTo\(|StringComparer\.(CurrentCulture|InvariantCulture)");
        var offenders = Directory.EnumerateFiles(Path.Combine(dir!.FullName, "Panacea.Sim"), "*.cs", SearchOption.AllDirectories)
            .Where(f => !f.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}"))
            .SelectMany(f => File.ReadAllLines(f).Select((line, i) => (f, i, line: line.Split("//")[0])))
            .Where(x => forbidden.IsMatch(x.line))
            .Select(x => $"{x.f}:{x.i + 1}: {x.line.Trim()}")
            .ToList();
        Assert.Empty(offenders);
    }

    [Theory]
    [InlineData(2.5, 3)]
    [InlineData(-2.5, -2)]
    [InlineData(0.49999999999999994, 0)]
    [InlineData(-0.5, 0)]
    [InlineData(1.5, 2)]
    [InlineData(-1.5, -1)]
    [InlineData(4503599627370495.5, 4503599627370496)]
    [InlineData(-7.000000001, -7)]
    public void JsRoundRoundsHalvesUp(double x, double expected) => Assert.Equal(expected, JsMath.Round(x));
}
