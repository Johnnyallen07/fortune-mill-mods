using System.Numerics;
using FortuneMillPowerMod;

var tests = new (string Name, Action Run)[]
{
    ("positive BigInteger gains are scaled by five", () =>
    {
        AssertEqual(new BigInteger(500), PowerModMath.ScalePositive(new BigInteger(100), 5.0));
    }),
    ("negative BigInteger changes are not scaled", () =>
    {
        AssertEqual(new BigInteger(-100), PowerModMath.ScalePositive(new BigInteger(-100), 2.0));
    }),
    ("positive long gains are scaled by five", () =>
    {
        AssertEqual(50L, PowerModMath.ScalePositive(10L, 5.0));
    }),
    ("upgrade costs are reduced by ninety percent", () =>
    {
        AssertEqual(new BigInteger(100), PowerModMath.ScaleCost(new BigInteger(1000), 0.1));
    }),
    ("positive bonuses are scaled by five", () =>
    {
        AssertEqual(7.5, PowerModMath.ScaleBonus(1.5, 5.0));
    }),
    ("negative bonuses are not scaled", () =>
    {
        AssertEqual(-1.5, PowerModMath.ScaleBonus(-1.5, 2.0));
    }),
    ("default upgrade cost multiplier reduces costs by ninety percent", () =>
    {
        AssertEqual(0.1, PowerModDefaults.UpgradeCostMultiplier);
    }),
    ("default currency multiplier is five", () =>
    {
        AssertEqual(5.0, PowerModDefaults.CurrencyGainMultiplier);
    }),
    ("default bonus multiplier is five", () =>
    {
        AssertEqual(5.0, PowerModDefaults.BonusMultiplier);
    }),
    ("zenith levels stay at their actual value", () =>
    {
        AssertEqual(0, PowerModMath.KeepZenithLevel(0));
        AssertEqual(3, PowerModMath.KeepZenithLevel(3));
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
