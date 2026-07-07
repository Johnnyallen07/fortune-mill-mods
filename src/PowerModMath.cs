using System;
using System.Numerics;

namespace FortuneMillPowerMod;

public static class PowerModDefaults
{
    public const double CurrencyGainMultiplier = 2.0;
    public const double UpgradeCostMultiplier = 0.1;
    public const double BonusMultiplier = 100.0;
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

    public static double ScaleBonus(double value, double multiplier)
    {
        if (value <= 0.0 || multiplier <= 1.0)
        {
            return value;
        }

        return value * multiplier;
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
