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
    private static ConfigEntry<double>? upgradeCostGrowthBase;
    private static ConfigEntry<double>? zenithBonusMultiplier;
    private static Harmony? harmony;

    internal static ManualLogSource? Logger { get; private set; }

    internal static double CurrencyGainMultiplier => currencyGainMultiplier?.Value ?? PowerModDefaults.CurrencyGainMultiplier;
    internal static double UpgradeCostMultiplier => upgradeCostMultiplier?.Value ?? PowerModDefaults.UpgradeCostMultiplier;
    internal static double UpgradeCostGrowthBase => upgradeCostGrowthBase?.Value ?? PowerModDefaults.UpgradeCostGrowthBase;
    internal static double ZenithBonusMultiplier => zenithBonusMultiplier?.Value ?? PowerModDefaults.ZenithBonusMultiplier;

    public override void Load()
    {
        Logger = Log;

        currencyGainMultiplier = Config.Bind("Multipliers", "CurrencyGainMultiplier", PowerModDefaults.CurrencyGainMultiplier, "Multiplier applied to positive currency gains. 5.0 means 5x.");
        upgradeCostMultiplier = Config.Bind("Multipliers", "UpgradeCostMultiplier", PowerModDefaults.UpgradeCostMultiplier, "Legacy final upgrade cost multiplier. 1.0 leaves final costs unchanged.");
        upgradeCostGrowthBase = Config.Bind("Multipliers", "UpgradeCostGrowthBase", PowerModDefaults.UpgradeCostGrowthBase, "Caps positive upgrade cost growth bases. 1.25 keeps exponential growth to about 1.25x per level.");
        zenithBonusMultiplier = Config.Bind("Multipliers", "ZenithBonusMultiplier", PowerModDefaults.ZenithBonusMultiplier, "Multiplier applied to positive Zenith / NG+ Shop AttributeModifier bonuses. 10.0 means 10x.");

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
    private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
    {
        var codes = instructions.ToList();
        var scaleGrowth = AccessTools.Method(typeof(Patch_UpgradeContainer_GetCost), nameof(ScaleGrowth));

        for (var i = 0; i < codes.Count - 3; i++)
        {
            if (codes[i].opcode == System.Reflection.Emit.OpCodes.Ldloc_3
                && codes[i + 1].opcode == System.Reflection.Emit.OpCodes.Ldarg_1
                && codes[i + 2].opcode == System.Reflection.Emit.OpCodes.Conv_R8
                && codes[i + 3].Calls(AccessTools.Method(typeof(Math), nameof(Math.Pow), new[] { typeof(double), typeof(double) })))
            {
                codes.Insert(i + 1, new CodeInstruction(System.Reflection.Emit.OpCodes.Call, scaleGrowth));
                break;
            }
        }

        return codes;
    }

    private static void Postfix(ref BigInteger __result)
    {
        __result = PowerModMath.ScaleCost(__result, FortuneMillPowerModPlugin.UpgradeCostMultiplier);
    }

    private static double ScaleGrowth(double value)
    {
        return PowerModMath.ScaleUpgradeCostGrowth(value, FortuneMillPowerModPlugin.UpgradeCostGrowthBase);
    }
}

[HarmonyPatch(typeof(PlayerDataManager), "GetTrialMulti")]
internal static class Patch_PlayerDataManager_GetTrialMulti
{
    private static void Postfix(ref double __result)
    {
        __result = PowerModMath.ClampTrialMultiplier(__result);
    }
}

[HarmonyPatch(typeof(PlayerDataManager), "MaybeUpdateTrialMulti")]
internal static class Patch_PlayerDataManager_MaybeUpdateTrialMulti
{
    private static void Prefix(ref double realMulti)
    {
        realMulti = PowerModMath.ClampTrialMultiplier(realMulti);
    }
}

[HarmonyPatch(typeof(PlayerDataManager), "GetAttribute")]
internal static class Patch_PlayerDataManager_GetAttribute
{
    private static void Postfix(AttributeIndex attAttributeIndex, ref double __result)
    {
        __result = PowerModMath.ClampDartBoardAttribute((int)attAttributeIndex, __result);
    }
}
