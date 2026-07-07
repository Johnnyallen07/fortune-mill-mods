#!/usr/bin/env bash
set -euo pipefail

SCRIPT_DIR="$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")" && pwd)"
MOD_DIR="$(cd -- "${SCRIPT_DIR}/.." && pwd)"
LAUNCHER="${MOD_DIR}/launch-fortune-mill-modded.sh"

output="$("$LAUNCHER" --dry-run)"

grep -F 'scripts/install-macos-direct-patch.sh' <<<"$output" >/dev/null
grep -F 'open steam://rungameid/4731620' <<<"$output" >/dev/null

echo "PASS launch script dry-run patches assemblies and launches through Steam"
