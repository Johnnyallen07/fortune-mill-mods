using System.Numerics;
using FortuneMillPowerMod;

var tests = new (string Name, Action Run)[]
{
    ("positive BigInteger gains are scaled", () =>
    {
        AssertEqual(new BigInteger(200), PowerModMath.ScalePositive(new BigInteger(100), 2.0));
    }),
    ("negative BigInteger changes are not scaled", () =>
    {
        AssertEqual(new BigInteger(-100), PowerModMath.ScalePositive(new BigInteger(-100), 2.0));
    }),
    ("positive long gains are scaled", () =>
    {
        AssertEqual(20L, PowerModMath.ScalePositive(10L, 2.0));
    }),
    ("upgrade costs can be reduced to zero", () =>
    {
        AssertEqual(BigInteger.Zero, PowerModMath.ScaleCost(new BigInteger(999), 0.0));
    }),
    ("positive bonuses are scaled", () =>
    {
        AssertEqual(3.0, PowerModMath.ScaleBonus(1.5, 2.0));
    }),
    ("negative bonuses are not scaled", () =>
    {
        AssertEqual(-1.5, PowerModMath.ScaleBonus(-1.5, 2.0));
    }),
    ("zenith levels are forced to max when lower", () =>
    {
        AssertEqual(5, PowerModMath.EnsureMinimumZenithLevel(2, 5));
    }),
    ("zenith levels above max are preserved", () =>
    {
        AssertEqual(7, PowerModMath.EnsureMinimumZenithLevel(7, 5));
    }),
};

var failures = 0;
foreach (var test in tests)
{
    try
    {
        test.Run();
        Console.WriteLine($"PASS {test.Name}");
    }
    catch (Exception ex)
    {
        failures++;
        Console.WriteLine($"FAIL {test.Name}: {ex.Message}");
    }
}

if (failures > 0)
{
    Environment.Exit(1);
}

static void AssertEqual<T>(T expected, T actual)
{
    if (!EqualityComparer<T>.Default.Equals(expected, actual))
    {
        throw new InvalidOperationException($"expected {expected}, got {actual}");
    }
}
