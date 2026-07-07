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
    ("upgrade costs are not reduced at the final value anymore", () =>
    {
        AssertEqual(new BigInteger(1000), PowerModMath.ScaleCost(new BigInteger(1000), 1.0));
    }),
    ("positive general bonuses are zeroed", () =>
    {
        AssertEqual(0.0, PowerModMath.ScaleBonus(1.5, 0.0));
    }),
    ("positive zenith bonuses are scaled by ten", () =>
    {
        AssertEqual(15.0, PowerModMath.ScaleZenithBonus(1.5, 10.0));
    }),
    ("negative bonuses are not scaled", () =>
    {
        AssertEqual(-1.5, PowerModMath.ScaleBonus(-1.5, 2.0));
    }),
    ("default final upgrade cost multiplier is identity", () =>
    {
        AssertEqual(1.0, PowerModDefaults.UpgradeCostMultiplier);
    }),
    ("default upgrade cost growth is one point two five", () =>
    {
        AssertEqual(1.25, PowerModDefaults.UpgradeCostGrowthBase);
    }),
    ("default currency multiplier is five", () =>
    {
        AssertEqual(5.0, PowerModDefaults.CurrencyGainMultiplier);
    }),
    ("default general bonus multiplier is zero", () =>
    {
        AssertEqual(0.0, PowerModDefaults.BonusMultiplier);
    }),
    ("default zenith bonus multiplier is ten", () =>
    {
        AssertEqual(10.0, PowerModDefaults.ZenithBonusMultiplier);
    }),
    ("upgrade cost growth is capped to one point two five", () =>
    {
        AssertEqual(1.25, PowerModMath.ScaleUpgradeCostGrowth(2.0, PowerModDefaults.UpgradeCostGrowthBase));
        AssertEqual(1.1, PowerModMath.ScaleUpgradeCostGrowth(1.1, PowerModDefaults.UpgradeCostGrowthBase));
        AssertEqual(0.9, PowerModMath.ScaleUpgradeCostGrowth(0.9, PowerModDefaults.UpgradeCostGrowthBase));
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
