namespace Panacea.Sim;

/// <summary>
/// Portable transcendental functions: a line-for-line port of packages/sim/src/core/dmath.ts.
/// <para>
/// <c>Math.Log</c>/<c>Math.Cos</c>/<c>Math.Tanh</c> are not correctly rounded and differ between
/// .NET and V8 in the last bit. These use only + − × ÷, <c>Math.Sqrt</c> and <c>Math.Floor</c>
/// (exact in IEEE 754), in exactly the same order as the TypeScript, so both produce identical
/// bits. Keep the two files in lockstep.
/// </para>
/// </summary>
public static class DMath
{
    private const double LN2 = 0.6931471805599453;
    private const double LN2_HI = 6.9314718036912381649e-1;
    private const double LN2_LO = 1.9082149292705877e-10;
    private const double INV_LN2 = 1.4426950408889634;
    private const double SQRT_HALF = 0.7071067811865476;
    private const double SQRT_TWO = 1.4142135623730951;
    private const double PIO2_HI = 1.5707963267341256;
    private const double PIO2_LO = 6.077100506506192e-11;
    private const double INV_PIO2 = 0.6366197723675814;
    private const double TWO_64 = 18446744073709551616.0;
    private const double TWO_M64 = 5.421010862427522e-20;

    /// <summary>Natural logarithm.</summary>
    public static double Log(double x)
    {
        if (double.IsNaN(x) || x < 0) return double.NaN;
        if (x == 0) return double.NegativeInfinity;
        if (double.IsPositiveInfinity(x)) return x;
        double m = x;
        double e = 0;
        while (m < TWO_M64)
        {
            m = m * TWO_64;
            e = e - 64;
        }
        while (m >= TWO_64)
        {
            m = m * TWO_M64;
            e = e + 64;
        }
        while (m < SQRT_HALF)
        {
            m = m * 2;
            e = e - 1;
        }
        while (m >= SQRT_TWO)
        {
            m = m / 2;
            e = e + 1;
        }
        double s = (m - 1) / (m + 1);
        double s2 = s * s;
        double term = s;
        double sum = s;
        for (int k = 3; k <= 31; k += 2)
        {
            term = term * s2;
            sum = sum + term / k;
        }
        return e * LN2 + 2 * sum;
    }

    private static double Expm1Small(double x)
    {
        double term = x;
        double sum = x;
        for (int i = 2; i <= 18; i++)
        {
            term = (term * x) / i;
            sum = sum + term;
        }
        return sum;
    }

    /// <summary>e^x.</summary>
    public static double Exp(double x)
    {
        if (double.IsNaN(x)) return x;
        if (x > 709.782712893384) return double.PositiveInfinity;
        if (x < -745.1332191019412) return 0;
        double k = Math.Floor(x * INV_LN2 + 0.5);
        double r = x - k * LN2_HI - k * LN2_LO;
        double y = 1 + Expm1Small(r);
        double n = k;
        while (n > 0)
        {
            y = y * 2;
            n = n - 1;
        }
        while (n < 0)
        {
            y = y / 2;
            n = n + 1;
        }
        return y;
    }

    /// <summary>Hyperbolic tangent.</summary>
    public static double Tanh(double x)
    {
        if (double.IsNaN(x)) return x;
        double ax = x < 0 ? -x : x;
        if (ax > 22) return x < 0 ? -1 : 1;
        double y = 2 * ax;
        double em1 = y <= 0.34657359027997264 ? Expm1Small(y) : Exp(y) - 1;
        double t = em1 / (em1 + 2);
        return x < 0 ? -t : t;
    }

    private static double SinReduced(double r)
    {
        double r2 = r * r;
        double term = r;
        double sum = r;
        for (int i = 1; i <= 10; i++)
        {
            term = (term * -r2) / (double)(2 * i * (2 * i + 1));
            sum = sum + term;
        }
        return sum;
    }

    private static double CosReduced(double r)
    {
        double r2 = r * r;
        double term = 1;
        double sum = 1;
        for (int i = 1; i <= 10; i++)
        {
            term = (term * -r2) / (double)((2 * i - 1) * (2 * i));
            sum = sum + term;
        }
        return sum;
    }

    /// <summary>Cosine. Accurate for |x| &lt; 2^20·π/2; the sim only needs [0, 2π).</summary>
    public static double Cos(double x)
    {
        if (!double.IsFinite(x)) return double.NaN;
        double ax = x < 0 ? -x : x;
        double k = Math.Floor(ax * INV_PIO2 + 0.5);
        double r = ax - k * PIO2_HI - k * PIO2_LO;
        double quadrant = k % 4;
        if (quadrant == 0) return CosReduced(r);
        if (quadrant == 1) return -SinReduced(r);
        if (quadrant == 2) return -CosReduced(r);
        return SinReduced(r);
    }
}

/// <summary>JavaScript <c>Math</c> semantics where they differ from .NET's.</summary>
public static class JsMath
{
    /// <summary>
    /// JavaScript <c>Math.round</c>: nearest integer, halves toward +∞ (Math.round(-2.5) = -2).
    /// .NET's Math.Round is banker's rounding.
    /// </summary>
    public static double Round(double x)
    {
        if (!double.IsFinite(x)) return x;
        double f = Math.Floor(x);
        return x - f >= 0.5 ? f + 1 : f;
    }

    /// <summary>JavaScript <c>Number.isInteger</c>.</summary>
    public static bool IsInteger(double x) => double.IsFinite(x) && Math.Floor(x) == x;
}
