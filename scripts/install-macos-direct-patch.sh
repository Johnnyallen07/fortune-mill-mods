#!/usr/bin/env bash
set -euo pipefail

SCRIPT_DIR="$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")" && pwd)"
MOD_DIR="$(cd -- "${SCRIPT_DIR}/.." && pwd)"
GAME_ROOT="$(cd -- "${MOD_DIR}/.." && pwd)"
PATCHER_PROJECT="${MOD_DIR}/tools/FortuneMillPatcher/FortuneMillPatcher.csproj"

targets=(
  "${GAME_ROOT}/data_FortuneMill_windows_x86_64/FortuneMill.dll"
  "${GAME_ROOT}/Fortune Mill.app/Contents/Resources/data_FortuneMill_macos_arm64/FortuneMill.dll"
  "${GAME_ROOT}/Fortune Mill.app/Contents/Resources/data_FortuneMill_macos_x86_64/FortuneMill.dll"
)

existing_targets=()
for target in "${targets[@]}"; do
  if [[ -f "$target" ]]; then
    existing_targets+=("$target")
  fi
done

if [[ ${#existing_targets[@]} -eq 0 ]]; then
  echo "No FortuneMill.dll targets were found under: $GAME_ROOT" >&2
  exit 1
fi

blocked_dirs=()
for target in "${existing_targets[@]}"; do
  target_dir="$(dirname -- "$target")"
  probe="${target_dir}/.johnny-powermod-write-test.$$"
  if ! (: > "$probe") 2>/dev/null; then
    blocked_dirs+=("$target_dir")
  else
    rm -f "$probe"
  fi
done

if [[ ${#blocked_dirs[@]} -gt 0 ]]; then
  cat >&2 <<EOF
macOS is blocking writes to the game .app resource directories, so the mod cannot be installed yet.

Blocked directories:
EOF
  printf '  %s\n' "${blocked_dirs[@]}" >&2
  cat >&2 <<EOF

Fix:
1. Close Fortune Mill.
2. Open System Settings > Privacy & Security > App Management.
3. Allow the terminal app you use to run this script.
4. Run this again:
   ./scripts/install-macos-direct-patch.sh

You can open the setting directly with:
open "x-apple.systempreferences:com.apple.preference.security?Privacy_AppManagement"
EOF
  exit 1
fi

echo "Building FortuneMill direct patcher..."
dotnet build "$PATCHER_PROJECT" --nologo

echo "Patching FortuneMill.dll targets..."
if ! dotnet run --project "$PATCHER_PROJECT" -- "${existing_targets[@]}"; then
  cat >&2 <<EOF

Patch failed. If macOS reports Operation not permitted for the .app files:
1. Close Fortune Mill.
2. Open System Settings > Privacy & Security > App Management.
3. Allow your terminal app, then run this script again.

Existing backups use the suffix:
.bak-johnny-powermod
EOF
  exit 1
fi

echo "Done. Launch Fortune Mill normally from Steam so Steamworks loads correctly."
