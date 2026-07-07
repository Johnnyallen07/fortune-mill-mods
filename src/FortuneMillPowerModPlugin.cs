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
    internal const string PluginVersion = "1.1.0";

    private static ConfigEntry<double>? currencyGainMultiplier;
    private static ConfigEntry<double>? upgradeCostMultiplier;
    private static ConfigEntry<double>? bonusMultiplier;
    private static Harmony? harmony;

    internal static ManualLogSource? Logger { get; private set; }

    internal static double CurrencyGainMultiplier => currencyGainMultiplier?.Value ?? PowerModDefaults.CurrencyGainMultiplier;
    internal static double UpgradeCostMultiplier => upgradeCostMultiplier?.Value ?? PowerModDefaults.UpgradeCostMultiplier;
    internal static double BonusMultiplier => bonusMultiplier?.Value ?? PowerModDefaults.BonusMultiplier;

    public override void Load()
    {
        Logger = Log;

        currencyGainMultiplier = Config.Bind("Multipliers", "CurrencyGainMultiplier", PowerModDefaults.CurrencyGainMultiplier, "Multiplier applied to positive currency gains. 2.0 means +100%.");
        upgradeCostMultiplier = Config.Bind("Multipliers", "UpgradeCostMultiplier", PowerModDefaults.UpgradeCostMultiplier, "Multiplier applied to upgrade costs. 0.1 means -90% cost.");
        bonusMultiplier = Config.Bind("Multipliers", "BonusMultiplier", PowerModDefaults.BonusMultiplier, "Multiplier applied to positive AttributeModifier bonuses, including Zenith Shop per-level bonuses. 100.0 means 100x.");

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
