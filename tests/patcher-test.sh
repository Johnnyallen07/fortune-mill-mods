#!/usr/bin/env bash
set -euo pipefail

SCRIPT_DIR="$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")" && pwd)"
MOD_DIR="$(cd -- "${SCRIPT_DIR}/.." && pwd)"
GAME_ROOT="$(cd -- "${MOD_DIR}/.." && pwd)"
SOURCE_DLL="${GAME_ROOT}/data_FortuneMill_windows_x86_64/FortuneMill.dll"
BACKUP_DLL="${SOURCE_DLL}.bak-johnny-powermod"
TMP_DIR="$(mktemp -d)"

cleanup() {
  rm -rf "$TMP_DIR"
}
trap cleanup EXIT

if [[ -f "$BACKUP_DLL" ]]; then
  cp "$BACKUP_DLL" "${TMP_DIR}/FortuneMill.dll"
else
  cp "$SOURCE_DLL" "${TMP_DIR}/FortuneMill.dll"
fi

dotnet run --project "${MOD_DIR}/tools/FortuneMillPatcher/FortuneMillPatcher.csproj" -- "${TMP_DIR}/FortuneMill.dll"

dotnet run --project "${MOD_DIR}/tools/FortuneMillPatcher/FortuneMillPatcher.csproj" -- --verify-only "${TMP_DIR}/FortuneMill.dll" >/tmp/fortune-mill-patcher-verify.log
grep -F "verified direct patch hooks" /tmp/fortune-mill-patcher-verify.log >/dev/null
grep -F "currency=5x general-bonus=1x zenith-bonus=10x secret-shop-bonus=5x upgrade-growth=1.25x trial-multi<=10000x dart-bullseye-size<=165.39594025728948x dart-bullseye-count<=20" /tmp/fortune-mill-patcher-verify.log >/dev/null

dotnet run --project "${MOD_DIR}/tools/FortuneMillPatcher/FortuneMillPatcher.csproj" -- "${TMP_DIR}/FortuneMill.dll" >/tmp/fortune-mill-patcher-idempotent.log
grep -F "already patched" /tmp/fortune-mill-patcher-idempotent.log >/dev/null

echo "PASS patcher injects and detects direct FortuneMill.dll patches"
