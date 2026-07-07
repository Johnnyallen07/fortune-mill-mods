# Fortune Mill Power Mod Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Build a BepInEx 6 CoreCLR / HarmonyX plugin that automatically applies Fortune Mill economy and bonus boosts.

**Architecture:** Keep deterministic scaling logic in `PowerModMath` and test it with a small console test harness. Patch game methods with Harmony from a BepInEx `BasePlugin`. Install BepInEx NET.CoreCLR and activate it via `STARTUP_HOOKS`.

**Tech Stack:** C#; .NET SDK; BepInEx.NET.CoreCLR 6.0.0-be; HarmonyX; FortuneMill.dll / GodotSharp references.

## Global Constraints

- Source stays in the current `mods` directory.
- Build output copies to `../BepInEx/plugins`.
- Defaults are currency `2x`, upgrade cost `0x`, bonus `2x`, all NG+ Shop bonuses enabled at level 5.
- Do not edit `FortuneMill.dll`.

---

### Task 1: Pure Scaling Logic

**Files:**
- Create: `src/PowerModMath.cs`
- Create: `tests/FortuneMillPowerMod.Tests.csproj`
- Create: `tests/Program.cs`

**Interfaces:**
- Produces: `PowerModMath.ScalePositive(BigInteger,double)`, `ScalePositive(long,double)`, `ScaleCost(BigInteger,double)`, `ScaleBonus(double,double)`, `EnsureMinimumZenithLevel(int,int)`.

- [x] Write failing tests for positive/negative gain scaling, zero-cost scaling, bonus scaling, and Zenith minimum levels.
- [x] Run `dotnet run --project tests/FortuneMillPowerMod.Tests.csproj` and confirm it fails before production code exists.
- [x] Implement `PowerModMath`.
- [x] Run the test command and confirm all checks pass.

### Task 2: BepInEx/Harmony Plugin

**Files:**
- Create: `FortuneMillPowerMod.csproj`
- Create: `NuGet.config`
- Create: `src/FortuneMillPowerModPlugin.cs`

**Interfaces:**
- Consumes: `PowerModMath`.
- Produces: BepInEx plugin `com.johnny.fortunemill.powermod`.

- [x] Create the plugin project with BepInEx.NET.CoreCLR and HarmonyX references.
- [x] Patch `PlayerDataManager` currency methods.
- [x] Patch `UpgradeContainer.GetCost`.
- [x] Patch `AttributeModifier.ComputeVal`.
- [x] Patch `GetNewGamePlus`, `Global_GetNewGamePlus`, and `GetZenithUpgradeLevel`.
- [x] Run `dotnet build FortuneMillPowerMod.csproj`.

### Task 3: Runtime Installation

**Files:**
- Create: `scripts/install-bepinex-netcore.sh`
- Create: `scripts/patch-runtimeconfig.sh`
- Modify: `../data_FortuneMill_windows_x86_64/FortuneMill.runtimeconfig.json`
- Create: `README.md`

**Interfaces:**
- Produces: Repeatable install flow and runtimeconfig startup hook.

- [x] Download and unpack BepInEx NET.CoreCLR BE 785 into the game root.
- [x] Confirm plugin DLL exists under `../BepInEx/plugins`.
- [x] Patch Windows runtimeconfig with `STARTUP_HOOKS`.
- [x] Add scripts for reinstall and runtimeconfig patching.
- [x] Document build, install, and config.
