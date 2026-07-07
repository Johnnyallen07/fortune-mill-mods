# Fortune Mill Power Mod Design

Build a BepInEx 6 CoreCLR plugin in the current `mods` directory, matching the previous Rogue Legacy 2 C# / Harmony workflow as closely as the GodotSharp .NET 8 game allows.

The plugin patches Fortune Mill at runtime without modifying `FortuneMill.dll`. It scales positive currency entry points by `2x`, reduces `UpgradeContainer.GetCost()` results to `0x`, doubles positive `AttributeModifier.ComputeVal()` outputs, and makes NG+ / Zenith shop attributes apply by forcing at least NG+ 1 and Zenith level 5 when those queries run.

The project keeps pure scaling math in `src/PowerModMath.cs` so it can be tested without loading the game. The Harmony plugin lives in `src/FortuneMillPowerModPlugin.cs`. Build output copies to `../BepInEx/plugins`. Runtime activation uses BepInEx NET.CoreCLR as a startup hook through `FortuneMill.runtimeconfig.json`.

Risks: the macOS `.app` bundle may block runtimeconfig edits through App Management protection, and BepInEx NET.CoreCLR is a bleeding-edge package. The Windows data runtimeconfig is patched automatically; the macOS app runtimeconfigs may need the provided script to be run from a permissioned terminal.
