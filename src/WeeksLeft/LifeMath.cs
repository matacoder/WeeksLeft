namespace WeeksLeft;

/// <summary>
/// Derives a full life table from a country's life expectancy at birth using a
/// Brass relational logit model (beta = 1), then reads remaining life expectancy
/// e(x) at the person's current age.
///
/// Why: life expectancy AT BIRTH is the wrong number for an adult. A 45-year-old
/// has already survived all the infant and young-adult mortality baked into e0,
/// so their expected final age is materially higher than e0. e(x) fixes that.
/// </summary>
public static class LifeMath
{
    public const int MaxAge = 115;

    // Standard survivorship curve l(x), radix 1.0, modern low-mortality shape.
    // Anchors are abridged (0,1,5,10,...,110); interpolated to a 1-year grid below.
    private static readonly int[] StdAges =
    {
        0, 1, 5, 10, 15, 20, 25, 30, 35, 40, 45, 50, 55, 60,
        65, 70, 75, 80, 85, 90, 95, 100, 105, 110, MaxAge
    };

    private static readonly double[] StdL =
    {
        1.00000, 0.99500, 0.99400, 0.99350, 0.99300, 0.99200, 0.99000, 0.98750,
        0.98450, 0.98050, 0.97500, 0.96700, 0.95500, 0.93700, 0.91000, 0.87000,
        0.81000, 0.72000, 0.58500, 0.40000, 0.21000, 0.07500, 0.01600, 0.00150,
        0.00000
    };

    /// <summary>logit(l_s(x)) on a 1-year grid, index 0..MaxAge. Index 0 is +inf-ish, handled specially.</summary>
    private static readonly double[] _stdLogit = BuildStandardLogit();

    private static double[] BuildStandardLogit()
    {
        var l = new double[MaxAge + 1];
        for (int seg = 0; seg < StdAges.Length - 1; seg++)
        {
            int a0 = StdAges[seg], a1 = StdAges[seg + 1];
            double l0 = StdL[seg], l1 = StdL[seg + 1];
            for (int x = a0; x < a1 && x <= MaxAge; x++)
            {
                double t = (double)(x - a0) / (a1 - a0);
                l[x] = l0 + (l1 - l0) * t;
            }
        }
        l[MaxAge] = StdL[^1];

        var logit = new double[MaxAge + 1];
        for (int x = 0; x <= MaxAge; x++)
        {
            double v = Math.Clamp(l[x], 1e-7, 1 - 1e-7);
            logit[x] = 0.5 * Math.Log((1 - v) / v);
        }
        return logit;
    }

    /// <summary>Survivorship curve for a given Brass alpha. Higher alpha = higher mortality.</summary>
    private static double[] Survivorship(double alpha)
    {
        var l = new double[MaxAge + 1];
        l[0] = 1.0;
        for (int x = 1; x <= MaxAge; x++)
        {
            double y = alpha + _stdLogit[x];
            l[x] = 1.0 / (1.0 + Math.Exp(2.0 * y));
            if (l[x] > l[x - 1]) l[x] = l[x - 1]; // enforce monotonicity
        }
        l[MaxAge] = 0.0;
        return l;
    }

    /// <summary>Person-years lived above exact age x, from a 1-year survivorship grid.</summary>
    private static double TotalYearsAbove(double[] l, int x)
    {
        double sum = 0;
        for (int i = x; i < MaxAge; i++) sum += (l[i] + l[i + 1]) / 2.0;
        return sum;
    }

    private static double E0(double[] l) => TotalYearsAbove(l, 0) / l[0];

    /// <summary>Finds the Brass alpha whose life table reproduces the target e0.</summary>
    private static double FitAlpha(double targetE0)
    {
        double lo = -4.0, hi = 4.0; // e0 is monotonically decreasing in alpha
        for (int i = 0; i < 60; i++)
        {
            double mid = (lo + hi) / 2.0;
            if (E0(Survivorship(mid)) > targetE0) lo = mid; else hi = mid;
        }
        return (lo + hi) / 2.0;
    }

    private static readonly Dictionary<double, double[]> _tableCache = new();

    private static double[] TableFor(double targetE0)
    {
        double key = Math.Round(targetE0, 2);
        lock (_tableCache)
        {
            if (_tableCache.TryGetValue(key, out var cached)) return cached;
            var t = Survivorship(FitAlpha(key));
            _tableCache[key] = t;
            return t;
        }
    }

    /// <summary>Remaining life expectancy e(x) at exact age <paramref name="age"/> (may be fractional).</summary>
    public static double RemainingAt(double targetE0, double age)
    {
        var l = TableFor(targetE0);
        if (age <= 0) return E0(l);
        if (age >= MaxAge - 1) return 0.5;

        int x = (int)Math.Floor(age);
        double frac = age - x;

        double lx = l[x] + (l[x + 1] - l[x]) * frac;
        if (lx <= 1e-9) return 0.5;

        // person-years above exact fractional age
        double tail = TotalYearsAbove(l, x + 1) + (lx + l[x + 1]) / 2.0 * (1 - frac);
        return tail / lx;
    }

    /// <summary>Expected age at death for someone currently aged <paramref name="age"/>.</summary>
    public static double ExpectedFinalAge(double targetE0, double age)
        => age + RemainingAt(targetE0, age);
}
