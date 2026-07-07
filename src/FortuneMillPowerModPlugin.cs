using System;
using System.Numerics;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using BepInEx.NET.Common;
using HarmonyLib;

namespace FortuneMillPowerMod;

[BepInPlugin(PluginGuid, PluginName, PluginVersion)]
public sealed class FortuneMillPowerModPlugin : BasePlugin
{
    internal const string PluginGuid = "com.johnny.fortunemill.powermod";
    internal const string PluginName = "Fortune Mill Power Mod";
    internal const string PluginVersion = "1.0.0";

    private static ConfigEntry<double>? currencyGainMultiplier;
    private static ConfigEntry<double>? upgradeCostMultiplier;
    private static ConfigEntry<double>? bonusMultiplier;
    private static ConfigEntry<bool>? enableAllNgShopBonuses;
    private static ConfigEntry<int>? forcedZenithLevel;
    private static Harmony? harmony;

    internal static ManualLogSource? Logger { get; private set; }

    internal static double CurrencyGainMultiplier => currencyGainMultiplier?.Value ?? 2.0;
    internal static double UpgradeCostMultiplier => upgradeCostMultiplier?.Value ?? 0.0;
    internal static double BonusMultiplier => bonusMultiplier?.Value ?? 2.0;
    internal static bool EnableAllNgShopBonuses => enableAllNgShopBonuses?.Value ?? true;
    internal static int ForcedZenithLevel => forcedZenithLevel?.Value ?? 5;

    public override void Load()
    {
        Logger = Log;

        currencyGainMultiplier = Config.Bind("Multipliers", "CurrencyGainMultiplier", 2.0, "Multiplier applied to positive currency gains. 2.0 means +100%.");
        upgradeCostMultiplier = Config.Bind("Multipliers", "UpgradeCostMultiplier", 0.0, "Multiplier applied to upgrade costs. 0.0 means -100% cost.");
        bonusMultiplier = Config.Bind("Multipliers", "BonusMultiplier", 2.0, "Multiplier applied to positive AttributeModifier bonuses. 2.0 means +100%.");
        enableAllNgShopBonuses = Config.Bind("NGPlus", "EnableAllNgShopBonuses", true, "Treat NG+ Shop / Zenith bonuses as owned.");
        forcedZenithLevel = Config.Bind("NGPlus", "ForcedZenithLevel", 5, "Minimum level returned for every NG+ Shop / Zenith upgrade.");

        harmony = new Harmony(PluginGuid);
        harmony.PatchAll(typeof(FortuneMillPowerModPlugin).Assembly);

        Logger.LogInfo($"{PluginName} {PluginVersion} loaded.");
    }
}

[HarmonyPatch(typeof(PlayerDataManager), "AddCurrency", typeof(int), typeof(BigInteger))]
internal static class Patch_PlayerDataManager_AddCurrency
{
    private static void Prefix(ref BigInteger score)
    {
        score = PowerModMath.ScalePositive(score, FortuneMillPowerModPlugin.CurrencyGainMultiplier);
    }
}

[HarmonyPatch(typeof(PlayerDataManager), "AddSecretCurrency", typeof(int), typeof(BigInteger))]
internal static class Patch_PlayerDataManager_AddSecretCurrency
{
    private static void Prefix(ref BigInteger score)
    {
        score = PowerModMath.ScalePositive(score, FortuneMillPowerModPlugin.CurrencyGainMultiplier);
    }
}

[HarmonyPatch(typeof(PlayerDataManager), "AdjustPachiBallCount", typeof(BigInteger))]
internal static class Patch_PlayerDataManager_AdjustPachiBallCount
{
    private static void Prefix(ref BigInteger change)
    {
        change = PowerModMath.ScalePositive(change, FortuneMillPowerModPlugin.CurrencyGainMultiplier);
    }
}

[HarmonyPatch(typeof(PlayerDataManager), "AddTokens", typeof(long))]
internal static class Patch_PlayerDataManager_AddTokens
{
    private static void Prefix(ref long tokensToAdd)
    {
        tokensToAdd = PowerModMath.ScalePositive(tokensToAdd, FortuneMillPowerModPlugin.CurrencyGainMultiplier);
    }
}

[HarmonyPatch(typeof(PlayerDataManager), "AddFuel", typeof(long), typeof(long))]
internal static class Patch_PlayerDataManager_AddFuel
{
    private static void Prefix(ref long inc)
    {
        inc = PowerModMath.ScalePositive(inc, FortuneMillPowerModPlugin.CurrencyGainMultiplier);
    }
}

[HarmonyPatch(typeof(UpgradeContainer), "GetCost", typeof(long))]
internal static class Patch_UpgradeContainer_GetCost
{
    private static void Postfix(ref BigInteger __result)
    {
        __result = PowerModMath.ScaleCost(__result, FortuneMillPowerModPlugin.UpgradeCostMultiplier);
    }
}

[HarmonyPatch(typeof(AttributeModifier), "ComputeVal", typeof(long))]
internal static class Patch_AttributeModifier_ComputeVal
{
    private static void Postfix(ref double __result)
    {
        __result = PowerModMath.ScaleBonus(__result, FortuneMillPowerModPlugin.BonusMultiplier);
    }
}

[HarmonyPatch(typeof(PlayerDataManager), "GetNewGamePlus")]
internal static class Patch_PlayerDataManager_GetNewGamePlus
{
    private static void Postfix(ref long __result)
    {
        if (FortuneMillPowerModPlugin.EnableAllNgShopBonuses && __result < 1L)
        {
            __result = 1L;
        }
    }
}

[HarmonyPatch(typeof(PlayerDataManager), "Global_GetNewGamePlus")]
internal static class Patch_PlayerDataManager_GlobalGetNewGamePlus
{
    private static void Postfix(ref long __result)
    {
        if (FortuneMillPowerModPlugin.EnableAllNgShopBonuses && __result < 1L)
        {
            __result = 1L;
        }
    }
}

[HarmonyPatch(typeof(PlayerDataManager), "GetZenithUpgradeLevel", typeof(int))]
internal static class Patch_PlayerDataManager_GetZenithUpgradeLevel
{
    private static void Postfix(ref int __result)
    {
        if (FortuneMillPowerModPlugin.EnableAllNgShopBonuses)
        {
            __result = PowerModMath.EnsureMinimumZenithLevel(__result, FortuneMillPowerModPlugin.ForcedZenithLevel);
        }
    }
}
