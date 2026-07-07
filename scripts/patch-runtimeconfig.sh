#!/usr/bin/env bash
set -euo pipefail

SCRIPT_DIR="$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")" && pwd)"
MOD_DIR="$(cd -- "${SCRIPT_DIR}/.." && pwd)"
GAME_ROOT="$(cd -- "${MOD_DIR}/.." && pwd)"
MAC_HOOK_PATH="${GAME_ROOT}/BepInEx.NET.CoreCLR.dll"
WINDOWS_HOOK_PATH="BepInEx.NET.CoreCLR.dll"

patch_file() {
  local file="$1"
  local hook="$2"
  if [[ ! -f "$file" ]]; then
    echo "skip missing: $file"
    return
  fi

  local backup="${file}.bak-johnny-powermod"
  if [[ ! -f "$backup" ]]; then
    if ! cp "$file" "$backup"; then
      echo "permission denied, could not back up: $file"
      echo "run this script from a terminal with permission to modify the app bundle, or patch this file manually."
      return
    fi
  fi

  if ! python3 - "$file" "$hook" <<'PY'
import json
import sys

path, hook = sys.argv[1], sys.argv[2]
with open(path, "r", encoding="utf-8") as f:
    data = json.load(f)

runtime_options = data.setdefault("runtimeOptions", {})
config = runtime_options.setdefault("configProperties", {})
config["STARTUP_HOOKS"] = hook

with open(path, "w", encoding="utf-8") as f:
    json.dump(data, f, indent=2)
    f.write("\n")
PY
  then
    echo "permission denied, could not patch: $file"
    echo "run this script from a terminal with permission to modify the app bundle, or patch this file manually."
    return
  fi

  echo "patched: $file"
}

patch_file "${GAME_ROOT}/data_FortuneMill_windows_x86_64/FortuneMill.runtimeconfig.json" "$WINDOWS_HOOK_PATH"
patch_file "${GAME_ROOT}/Fortune Mill.app/Contents/Resources/data_FortuneMill_macos_arm64/FortuneMill.runtimeconfig.json" "$MAC_HOOK_PATH"
patch_file "${GAME_ROOT}/Fortune Mill.app/Contents/Resources/data_FortuneMill_macos_x86_64/FortuneMill.runtimeconfig.json" "$MAC_HOOK_PATH"
