using System;
using System.Numerics;

namespace FortuneMillPowerMod;

public static class PowerModDefaults
{
    public const double CurrencyGainMultiplier = 5.0;
    public const double UpgradeCostMultiplier = 1.0;
    public const double UpgradeCostGrowthBase = 1.25;
    public const double ZenithBonusMultiplier = 10.0;
    public const double MaxTrialMultiplier = 10_000.0;
}

public static class PowerModMath
{
    public static BigInteger ScalePositive(BigInteger value, double multiplier)
    {
        if (value.Sign <= 0 || multiplier <= 1.0)
        {
            return value;
        }

        return ScaleBigInteger(value, multiplier);
    }

    public static long ScalePositive(long value, double multiplier)
    {
        if (value <= 0 || multiplier <= 1.0)
        {
            return value;
        }

        return checked((long)Math.Round(value * multiplier));
    }

    public static BigInteger ScaleCost(BigInteger value, double multiplier)
    {
        if (value.Sign <= 0)
        {
            return value;
        }

        if (multiplier <= 0.0)
        {
            return BigInteger.Zero;
        }

        if (multiplier == 1.0)
        {
            return value;
        }

        return ScaleBigInteger(value, multiplier);
    }

    public static double KeepGeneralBonus(double value)
    {
        return value;
    }

    public static double ScaleZenithBonus(double value, double multiplier)
    {
        if (value <= 0.0)
        {
            return value;
        }

        return value * multiplier;
    }

    public static double ScaleUpgradeCostGrowth(double value, double growthBase)
    {
        if (value <= 1.0 || growthBase <= 1.0)
        {
            return value;
        }

        return Math.Min(value, growthBase);
    }

    public static double ClampTrialMultiplier(double value)
    {
        if (double.IsNaN(value) || value < 1.0)
        {
            return 1.0;
        }

        return Math.Min(value, PowerModDefaults.MaxTrialMultiplier);
    }

    public static int KeepZenithLevel(int currentLevel)
    {
        return currentLevel;
    }

    private static BigInteger ScaleBigInteger(BigInteger value, double multiplier)
    {
        var percent = checked((long)Math.Round(multiplier * 100.0));
        return value * percent / 100;
    }
}
