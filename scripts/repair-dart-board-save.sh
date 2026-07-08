#!/usr/bin/env bash
set -euo pipefail

force=0
if [[ "${1:-}" == "--force" ]]; then
  force=1
fi

if [[ "$force" -ne 1 ]] && pgrep -f "Fortune Mill.app/Contents/MacOS/Fortune Mill" >/dev/null; then
  cat >&2 <<'EOF'
Fortune Mill is still running. Close the game first so Steam/Godot does not overwrite the repaired save.

Then run:
  ./scripts/repair-dart-board-save.sh
EOF
  exit 1
fi

SAVE_PATH="${SAVE_PATH:-${HOME}/Library/Application Support/Godot/app_userdata/Fortune Mill/save_game.sav}"
if [[ ! -f "$SAVE_PATH" ]]; then
  echo "Save file not found: $SAVE_PATH" >&2
  exit 1
fi

backup="${SAVE_PATH}.bak-before-dart-board-fix-$(date +%Y%m%d-%H%M%S)"
cp "$SAVE_PATH" "$backup"

SAVE_PATH="$SAVE_PATH" node <<'NODE'
const fs = require("fs");

const savePath = process.env.SAVE_PATH;
const data = fs.readFileSync(savePath);
let offset = 0;

function readU32() {
  const value = data.readUInt32LE(offset);
  offset += 4;
  return value;
}

function skipBigInteger() {
  const length = readU32();
  offset += length;
}

function skipBigIntegerArray(length) {
  for (let i = 0; i < length; i++) {
    skipBigInteger();
  }
}

function storedLong(value) {
  return BigInt(value) + 65536n;
}

readU32();
skipBigIntegerArray(6);
skipBigIntegerArray(6);

const upgradeOffset = offset;
const sizeIndex = 14;
const countIndex = 15;
const sizeOffset = upgradeOffset + sizeIndex * 8;
const countOffset = upgradeOffset + countIndex * 8;
const oldSize = data.readBigUInt64LE(sizeOffset) - 65536n;
const oldCount = data.readBigUInt64LE(countOffset) - 65536n;

data.writeBigUInt64LE(storedLong(0), sizeOffset);
data.writeBigUInt64LE(storedLong(20), countOffset);

fs.writeFileSync(savePath, data);

console.log(`DARTS_BULLSEYE_SIZE upgrade ${oldSize} -> 0`);
console.log(`DARTS_BULLSEYE_COUNT upgrade ${oldCount} -> 20`);
NODE

echo "Backup: $backup"
