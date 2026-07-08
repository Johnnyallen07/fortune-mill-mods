# Fortune Mill Power Mod

Fortune Mill 的倍率 mod，保留 BepInEx 6 / HarmonyX 插件源码结构，同时提供 macOS 可用的直接程序集 patch 方案。

## 功能

- 所有正向货币获取默认 `5x`。
- 普通升级路线 / 普通 `AttributeModifier` 增益保持游戏原始数值，mod 不再提供普通属性倍率。
- Zenith Shop / NG+ Shop 不强制视为拥有，也不强制最低等级；等级保持游戏原本从 0 开始。买到的每一级正向增益单独默认 `10x`。
- Secret / Gem Shop 的每一级正向增益默认 `5x`，只作用在 `SecretDatabase` 的宝石商店升级上。
- 升级价格不再做最终 `0.1x` 硬折扣；普通升级的指数增长底数默认压到 `1.25x`，用于替代原本接近 `2x` 的增长速率。
- 投掷挑战倍率 `trialMulti` 会限制在 `1x..10000x`，避免旧倍率写入过大数值后让第一投掷区域记分板溢出。
- Dart 靶区属性会做安全限制：`DARTS_BULLSEYE_SIZE <= 165.39594025728948x`、`DARTS_BULLSEYE_COUNT <= 20`。Size 上限相当于 20 级 Bigger Bullseye，Count 上限对应游戏成就/靶区支持的 20 层，避免高等级靶心区域在点击/判定时进入无效状态。

## 构建

```bash
dotnet build FortuneMillPowerMod.csproj
```

构建后会复制到：

```text
../BepInEx/plugins/FortuneMillPowerMod.dll
```

## macOS 推荐安装

```bash
./scripts/install-macos-direct-patch.sh
```

这个脚本会构建 `tools/FortuneMillPatcher`，直接 patch 游戏里的 `FortuneMill.dll`，并在旁边保留：

```text
FortuneMill.dll.bak-johnny-powermod
```

原因：当前 macOS 版 Fortune Mill 是 Godot 4 导出的 .NET 游戏，实测没有执行 `DOTNET_STARTUP_HOOKS`，所以 BepInEx CoreCLR startup hook 不会正常注入。直接 patch 游戏程序集后，从 Steam 正常打开游戏即可自动生效。

macOS 的 `.app` 目录可能被系统 App Management 权限保护。如果脚本提示无法修改 `.app` 内部文件，请在有权限的终端里重新运行：

```bash
./scripts/install-macos-direct-patch.sh
```

## macOS 启动

可以直接从 Steam 打开游戏，或者用这个脚本先确保 patch 已应用，再通过 Steam AppID 启动：

```bash
./launch-fortune-mill-modded.sh
```

脚本会执行：

```text
./scripts/install-macos-direct-patch.sh
open steam://rungameid/4731620
```

不要直接运行 `../Fortune Mill.app/Contents/MacOS/Fortune Mill`；macOS 上这会绕过 Steam 正常启动路径，并可能触发 Steam client dylib 签名错误。

## 临时启停

在当前 `mods` 目录运行：

```bash
./mod.sh off
./mod.sh on
./mod.sh status
```

`off` 会从 `FortuneMill.dll.bak-johnny-powermod` 恢复原始 DLL，临时关闭 mod。`on` 会重新应用当前 direct patch。macOS 如果阻止写入 `.app` 内资源目录，需要先在 System Settings > Privacy & Security > App Management 允许你的终端应用。

## 存档修复

如果 Dart / 第一投掷区域已经因为旧数值变成 invalid，请先关闭游戏，再运行：

```bash
./scripts/repair-dart-board-save.sh
```

脚本会先备份 `save_game.sav`，然后只把 `DARTS_BULLSEYE_SIZE` 对应升级调到 `20`、`DARTS_BULLSEYE_COUNT` 对应升级调到 `20`。

## BepInEx 结构

项目仍包含 BepInEx 6 CoreCLR + HarmonyX 插件源码：

```bash
./scripts/install-bepinex-netcore.sh
```

这个脚本会下载 BepInEx NET.CoreCLR BE 785，安装到游戏根目录，然后 patch `FortuneMill.runtimeconfig.json` 的 `STARTUP_HOOKS`。在当前 macOS 版游戏里，这条注入路径不可靠；推荐使用上面的 direct patch。

如果 BepInEx 成功加载，会生成：

```text
../BepInEx/config/com.johnny.fortunemill.powermod.cfg
```

关键配置：

```ini
[Multipliers]
CurrencyGainMultiplier = 5
UpgradeCostMultiplier = 1
UpgradeCostGrowthBase = 1.25
ZenithBonusMultiplier = 10
SecretShopBonusMultiplier = 5
```

## 测试

```bash
dotnet run --project tests/FortuneMillPowerMod.Tests.csproj
./tests/patcher-test.sh
./tests/patcher-runtime-test.sh
./tests/launch-script-test.sh
./tests/toggle-script-test.sh
```
