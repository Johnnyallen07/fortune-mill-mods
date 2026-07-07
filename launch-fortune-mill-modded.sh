#!/usr/bin/env bash
set -euo pipefail

SCRIPT_DIR="$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")" && pwd)"
GAME_ROOT="$(cd -- "${SCRIPT_DIR}/.." && pwd)"
PATCH_SCRIPT="${SCRIPT_DIR}/scripts/install-macos-direct-patch.sh"
STEAM_APP_ID="${FORTUNE_MILL_STEAM_APPID:-4731620}"
STEAM_URL="steam://rungameid/${STEAM_APP_ID}"

if [[ ! -x "$PATCH_SCRIPT" ]]; then
  echo "Patch script not found or not executable: $PATCH_SCRIPT" >&2
  echo "Run: chmod +x scripts/install-macos-direct-patch.sh" >&2
  exit 1
fi

if [[ "${1:-}" == "--dry-run" ]]; then
  echo "cd $GAME_ROOT"
  echo "$PATCH_SCRIPT"
  echo "open $STEAM_URL"
  exit 0
fi

cd "$GAME_ROOT"
"$PATCH_SCRIPT"
open "$STEAM_URL"
