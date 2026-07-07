# Fortune Mill Power Mod

Fortune Mill 的 BepInEx 6 / HarmonyX mod，按 Rogue Legacy 2 的项目方式组织。

## 功能

- 所有正向货币获取默认 `2x`。
- 所有升级价格默认 `0.1x`，也就是 -90% 成本。
- 所有正向 `AttributeModifier` 增益默认 `100x`。
- Zenith Shop / NG+ Shop 不再强制等级；等级仍从 0 开始，已获得的每一级增益会跟随 `AttributeModifier` 默认 `100x`。

## 构建

```bash
dotnet build FortuneMillPowerMod.csproj
```

构建后会复制到：

```text
../BepInEx/plugins/FortuneMillPowerMod.dll
```

## 安装 BepInEx 和自动加载

```bash
./scripts/install-bepinex-netcore.sh
```

脚本会下载 BepInEx NET.CoreCLR BE 785，安装到游戏根目录，然后 patch `FortuneMill.runtimeconfig.json` 的 `STARTUP_HOOKS`。

macOS 的 `.app` 目录可能被系统 App Management 权限保护。如果脚本提示无法修改 `.app` 内部文件，请在有权限的终端里重新运行：

```bash
./scripts/patch-runtimeconfig.sh
```

## 配置

第一次成功加载后，BepInEx 会生成：

```text
../BepInEx/config/com.johnny.fortunemill.powermod.cfg
```

关键配置：

```ini
[Multipliers]
CurrencyGainMultiplier = 2
UpgradeCostMultiplier = 0.1
BonusMultiplier = 100
```
