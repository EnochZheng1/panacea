namespace Panacea.Sim;

/// <summary>The generator's whole state: four 32-bit unsigned integers (sfc32).</summary>
public sealed record RngState(uint A, uint B, uint C, uint D);

/// <summary>
/// Seeded, serialisable pseudo-random number generator (sfc32), bit-identical to
/// packages/sim/src/core/rng.ts. Never use System.Random in the simulation.
/// </summary>
public sealed class Rng
{
    private uint _a, _b, _c, _d;

    public Rng(RngState state)
    {
        _a = state.A;
        _b = state.B;
        _c = state.C;
        _d = state.D;
    }

    public static Rng FromSeed(string seed) => new(SeedRng(seed));

    /// <summary>Derives an initial generator state from a string seed (cyrb128-style hash).</summary>
    public static RngState SeedRng(string str)
    {
        unchecked
        {
            uint h1 = 1779033703u, h2 = 3144134277u, h3 = 1013904242u, h4 = 2773480762u;
            for (int i = 0; i < str.Length; i++)
            {
                uint k = str[i];
                h1 = h2 ^ ((h1 ^ k) * 597399067u);
                h2 = h3 ^ ((h2 ^ k) * 2869860233u);
                h3 = h4 ^ ((h3 ^ k) * 951274213u);
                h4 = h1 ^ ((h4 ^ k) * 2716044179u);
            }
            h1 = (h3 ^ (h1 >> 18)) * 597399067u;
            h2 = (h4 ^ (h2 >> 22)) * 2869860233u;
            h3 = (h1 ^ (h3 >> 17)) * 951274213u;
            h4 = (h2 ^ (h4 >> 19)) * 2716044179u;
            var rng = new Rng(new RngState(h1 ^ h2 ^ h3 ^ h4, h2 ^ h1, h3 ^ h1, h4 ^ h1));
            for (int i = 0; i < 12; i++) rng.NextU32();
            return rng.State();
        }
    }

    public RngState State() => new(_a, _b, _c, _d);

    /// <summary>Uniform 32-bit unsigned integer.</summary>
    public uint NextU32()
    {
        unchecked
        {
            uint t0 = _a + _b;
            _a = _b ^ (_b >> 9);
            _b = _c + (_c << 3);
            _c = (_c << 21) | (_c >> 11);
            _d = _d + 1;
            uint t = t0 + _d;
            _c = _c + t;
            return t;
        }
    }

    /// <summary>Uniform float in [0, 1).</summary>
    public double Float() => NextU32() / 4294967296.0;

    /// <summary>Uniform integer in [min, max] (inclusive).</summary>
    public int Int(int min, int max)
    {
        if (max < min) throw new ArgumentOutOfRangeException(nameof(max), $"Rng.int: invalid range [{min}, {max}]");
        return min + (int)Math.Floor(Float() * ((double)max - min + 1));
    }

    /// <summary>True with probability p.</summary>
    public bool Chance(double p) => Float() < p;

    public T Pick<T>(IReadOnlyList<T> items)
    {
        if (items.Count == 0) throw new ArgumentException("Rng.pick: empty array");
        return items[Int(0, items.Count - 1)];
    }

    /// <summary>Picks an item with probability proportional to its weight; default if all weights are 0.</summary>
    public T? Weighted<T>(IReadOnlyList<(T Item, double Weight)> items)
    {
        double total = 0;
        foreach (var x in items) total = total + Math.Max(0, x.Weight);
        if (total <= 0) return default;
        double roll = Float() * total;
        foreach (var x in items)
        {
            roll -= Math.Max(0, x.Weight);
            if (roll < 0) return x.Item;
        }
        return items.Count > 0 ? items[^1].Item : default;
    }

    /// <summary>Standard normal sample (Box–Muller), using the portable DMath functions.</summary>
    public static double Gaussian(Rng rng)
    {
        double u = Math.Max(rng.Float(), 1e-12);
        double v = rng.Float();
        return Math.Sqrt(-2 * DMath.Log(u)) * DMath.Cos(2 * Math.PI * v);
    }
}
