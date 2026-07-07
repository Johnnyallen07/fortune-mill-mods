#!/usr/bin/env bash
set -euo pipefail

SCRIPT_DIR="$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")" && pwd)"
GAME_ROOT="$(cd -- "${SCRIPT_DIR}/.." && pwd)"
APP_EXEC="${GAME_ROOT}/Fortune Mill.app/Contents/MacOS/Fortune Mill"
HOOK_DLL="${GAME_ROOT}/BepInEx.NET.CoreCLR.dll"
PLUGIN_DLL="${GAME_ROOT}/BepInEx/plugins/FortuneMillPowerMod.dll"

if [[ ! -x "$APP_EXEC" ]]; then
  echo "Fortune Mill executable not found or not executable: $APP_EXEC" >&2
  exit 1
fi

if [[ ! -f "$HOOK_DLL" ]]; then
  echo "BepInEx startup hook not found: $HOOK_DLL" >&2
  echo "Run ./scripts/install-bepinex-netcore.sh first." >&2
  exit 1
fi

if [[ ! -f "$PLUGIN_DLL" ]]; then
  echo "FortuneMillPowerMod plugin not found: $PLUGIN_DLL" >&2
  echo "Run dotnet build FortuneMillPowerMod.csproj first." >&2
  exit 1
fi

export DOTNET_STARTUP_HOOKS="$HOOK_DLL${DOTNET_STARTUP_HOOKS:+:$DOTNET_STARTUP_HOOKS}"

if [[ "${1:-}" == "--dry-run" ]]; then
  echo "DOTNET_STARTUP_HOOKS=$DOTNET_STARTUP_HOOKS"
  echo "cd $GAME_ROOT"
  echo "$APP_EXEC"
  exit 0
fi

cd "$GAME_ROOT"
exec "$APP_EXEC" "$@"
