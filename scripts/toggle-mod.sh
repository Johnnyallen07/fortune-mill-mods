#!/usr/bin/env bash
set -euo pipefail

SCRIPT_DIR="$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")" && pwd)"
MOD_DIR="$(cd -- "${SCRIPT_DIR}/.." && pwd)"
GAME_ROOT="$(cd -- "${MOD_DIR}/.." && pwd)"
PATCHER_PROJECT="${MOD_DIR}/tools/FortuneMillPatcher/FortuneMillPatcher.csproj"
DRY_RUN=0

if [[ "${1:-}" == "--dry-run" ]]; then
  DRY_RUN=1
  shift
fi

COMMAND="${1:-status}"

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

run_or_print() {
  if [[ "$DRY_RUN" -eq 1 ]]; then
    printf '%s\n' "$*"
  else
    "$@"
  fi
}

case "$COMMAND" in
  on|enable)
    run_or_print "${MOD_DIR}/scripts/install-macos-direct-patch.sh"
    ;;
  off|disable)
    if [[ "$DRY_RUN" -eq 1 ]]; then
      echo "restore .bak-johnny-powermod backups for FortuneMill.dll targets"
      exit 0
    fi

    restored=0
    for target in "${existing_targets[@]}"; do
      backup="${target}.bak-johnny-powermod"
      if [[ -f "$backup" ]]; then
        cp "$backup" "$target"
        echo "Restored original: $target"
        restored=$((restored + 1))
      else
        echo "No backup found, skipping: $target"
      fi
    done

    if [[ "$restored" -eq 0 ]]; then
      echo "No backups were restored. The mod may already be off, or the app directory is blocked by macOS permissions." >&2
      exit 1
    fi
    ;;
  status)
    if [[ "$DRY_RUN" -eq 1 ]]; then
      echo "verify direct patch hooks with FortuneMillPatcher --verify-only"
      exit 0
    fi

    dotnet build "$PATCHER_PROJECT" --nologo >/dev/null
    if dotnet run --project "$PATCHER_PROJECT" -- --verify-only "${existing_targets[@]}"; then
      echo "Fortune Mill Power Mod: on"
    else
      echo "Fortune Mill Power Mod: off or partially installed"
      exit 1
    fi
    ;;
  *)
    echo "Usage: ./mod.sh [--dry-run] on|off|status" >&2
    exit 2
    ;;
esac
