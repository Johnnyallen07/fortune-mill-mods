#!/usr/bin/env bash
set -euo pipefail

SCRIPT_DIR="$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")" && pwd)"
MOD_DIR="$(cd -- "${SCRIPT_DIR}/.." && pwd)"
LAUNCHER="${MOD_DIR}/launch-fortune-mill-modded.sh"

output="$("$LAUNCHER" --dry-run)"

grep -F 'DOTNET_STARTUP_HOOKS=' <<<"$output" >/dev/null
grep -F 'BepInEx.NET.CoreCLR.dll' <<<"$output" >/dev/null
grep -F 'Fortune Mill.app/Contents/MacOS/Fortune Mill' <<<"$output" >/dev/null

echo "PASS launch script dry-run includes startup hook and app executable"
