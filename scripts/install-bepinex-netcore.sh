#!/usr/bin/env bash
set -euo pipefail

SCRIPT_DIR="$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")" && pwd)"
MOD_DIR="$(cd -- "${SCRIPT_DIR}/.." && pwd)"
GAME_ROOT="$(cd -- "${MOD_DIR}/.." && pwd)"
BUILD_ID="785"
BUILD_HASH="6abdba4"
ZIP_NAME="BepInEx-NET.CoreCLR-net6.0-win-x64-6.0.0-be.${BUILD_ID}+${BUILD_HASH}.zip"
ZIP_URL="https://builds.bepinex.dev/projects/bepinex_be/${BUILD_ID}/BepInEx-NET.CoreCLR-net6.0-win-x64-6.0.0-be.${BUILD_ID}%2B${BUILD_HASH}.zip"
TMP_DIR="$(mktemp -d)"

cleanup() {
  rm -rf "$TMP_DIR"
}
trap cleanup EXIT

echo "Downloading ${ZIP_NAME}"
curl -L -o "${TMP_DIR}/${ZIP_NAME}" "$ZIP_URL"

echo "Installing BepInEx NET CoreCLR into ${GAME_ROOT}"
unzip -o "${TMP_DIR}/${ZIP_NAME}" -d "$GAME_ROOT"
mkdir -p "${GAME_ROOT}/BepInEx/plugins"

echo "Building and copying FortuneMillPowerMod"
dotnet build "${MOD_DIR}/FortuneMillPowerMod.csproj"

echo "Patching runtimeconfig files"
"${SCRIPT_DIR}/patch-runtimeconfig.sh"
