# Fortune Mill Power Mod Design

Build a BepInEx 6 CoreCLR plugin in the current `mods` directory, matching the previous Rogue Legacy 2 C# / Harmony workflow as closely as the GodotSharp .NET 8 game allows.

The plugin patches Fortune Mill at runtime without modifying `FortuneMill.dll`. It scales positive currency entry points by `2x`, reduces `UpgradeContainer.GetCost()` results to `0.1x`, and multiplies positive `AttributeModifier.ComputeVal()` outputs by `100x`. Zenith / NG+ Shop levels are not forced upward; they still start at 0, and each earned level receives the same `100x` positive bonus scaling.

The project keeps pure scaling math in `src/PowerModMath.cs` so it can be tested without loading the game. The Harmony plugin lives in `src/FortuneMillPowerModPlugin.cs`. Build output copies to `../BepInEx/plugins`. Runtime activation uses BepInEx NET.CoreCLR as a startup hook through `FortuneMill.runtimeconfig.json`.

Risks: the macOS `.app` bundle may block runtimeconfig edits through App Management protection, and BepInEx NET.CoreCLR is a bleeding-edge package. The Windows data runtimeconfig is patched automatically; the macOS app runtimeconfigs may need the provided script to be run from a permissioned terminal.
