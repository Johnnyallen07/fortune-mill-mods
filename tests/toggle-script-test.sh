#!/usr/bin/env bash
set -euo pipefail

SCRIPT_DIR="$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")" && pwd)"
MOD_DIR="$(cd -- "${SCRIPT_DIR}/.." && pwd)"

on_output="$("${MOD_DIR}/mod.sh" --dry-run on)"
off_output="$("${MOD_DIR}/mod.sh" --dry-run off)"
status_output="$("${MOD_DIR}/mod.sh" --dry-run status)"

grep -F "scripts/install-macos-direct-patch.sh" <<<"$on_output" >/dev/null
grep -F "restore .bak-johnny-powermod backups" <<<"$off_output" >/dev/null
grep -F "verify direct patch hooks" <<<"$status_output" >/dev/null

echo "PASS mod toggle script supports dry-run on/off/status"
