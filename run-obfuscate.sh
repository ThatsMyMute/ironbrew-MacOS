#!/usr/bin/env bash
set -euo pipefail

usage() {
  echo "Usage: $0 [--tag \"text\"]... <input.lua> [output.lua]"
  echo "Example: $0 --tag \"ironbrew on mac\" --tag \"made by thatsmymute\" hello.lua hello.obf.lua"
}

TAGS=()
POSITIONAL=()

while [[ $# -gt 0 ]]; do
  case "$1" in
    -h|--help)
      usage
      exit 0
      ;;
    --tag)
      if [[ $# -lt 2 ]]; then
        echo "Missing value for --tag" >&2
        usage
        exit 1
      fi
      TAGS+=("$2")
      shift 2
      ;;
    -*)
      echo "Unknown option: $1" >&2
      usage
      exit 1
      ;;
    *)
      POSITIONAL+=("$1")
      shift
      ;;
  esac
done

if [[ ${#POSITIONAL[@]} -lt 1 || ${#POSITIONAL[@]} -gt 2 ]]; then
  usage
  exit 1
fi

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
INPUT_PATH="${POSITIONAL[0]}"
OUTPUT_PATH="${POSITIONAL[1]:-}"

if [[ ! -f "$INPUT_PATH" ]]; then
  echo "Input file not found: $INPUT_PATH" >&2
  exit 1
fi

if [[ "$INPUT_PATH" = /* ]]; then
  INPUT_ABS="$INPUT_PATH"
else
  INPUT_ABS="$PWD/$INPUT_PATH"
fi

if [[ -z "$OUTPUT_PATH" ]]; then
  if [[ "$INPUT_ABS" == *.lua ]]; then
    OUTPUT_ABS="${INPUT_ABS%.lua}.obf.lua"
  else
    OUTPUT_ABS="${INPUT_ABS}.obf.lua"
  fi
else
  if [[ "$OUTPUT_PATH" = /* ]]; then
    OUTPUT_ABS="$OUTPUT_PATH"
  else
    OUTPUT_ABS="$PWD/$OUTPUT_PATH"
  fi
fi

TOOLS_DIR="$SCRIPT_DIR/.local-tools"
LUA_SRC_DIR="$TOOLS_DIR/lua-5.1.5/src"
LUA_BIN="$LUA_SRC_DIR/lua"
LUAC_BIN="$LUA_SRC_DIR/luac"

if [[ ! -x "$LUA_BIN" || ! -x "$LUAC_BIN" ]]; then
  mkdir -p "$TOOLS_DIR"
  if [[ ! -d "$TOOLS_DIR/lua-5.1.5" ]]; then
    echo "Downloading Lua 5.1.5..."
    curl -L -o "$TOOLS_DIR/lua-5.1.5.tar.gz" https://www.lua.org/ftp/lua-5.1.5.tar.gz
    tar -xzf "$TOOLS_DIR/lua-5.1.5.tar.gz" -C "$TOOLS_DIR"
  fi
  echo "Building local Lua tools..."
  (cd "$TOOLS_DIR/lua-5.1.5" && make macosx)
fi

echo "Building IronBrew2 CLI..."
dotnet build "$SCRIPT_DIR/IronBrew2 CLI/IronBrew2 CLI.csproj" -c Debug

CLI_DIR="$SCRIPT_DIR/IronBrew2 CLI/bin/Debug/netcoreapp3.1"
CLI_DLL="$CLI_DIR/IronBrew2 CLI.dll"
OUT_FILE="$CLI_DIR/out.lua"

if [[ ! -f "$CLI_DLL" ]]; then
  echo "CLI binary not found after build: $CLI_DLL" >&2
  exit 1
fi

WATERMARK_TEXT=""
if [[ ${#TAGS[@]} -gt 0 ]]; then
  for tag in "${TAGS[@]}"; do
    if [[ -n "$WATERMARK_TEXT" ]]; then
      WATERMARK_TEXT+=$'\n'
    fi
    WATERMARK_TEXT+="$tag"
  done
fi

echo "Obfuscating $INPUT_ABS..."
(
  cd "$CLI_DIR"
  DOTNET_ROLL_FORWARD=Major \
  IB2_LUAC="$LUAC_BIN" \
  IB2_LUA="$LUA_BIN" \
  IB2_WATERMARK="$WATERMARK_TEXT" \
  dotnet "$CLI_DLL" "$INPUT_ABS"
)

if [[ ! -f "$OUT_FILE" ]]; then
  echo "Expected output not found: $OUT_FILE" >&2
  exit 1
fi

mkdir -p "$(dirname "$OUTPUT_ABS")"
cp "$OUT_FILE" "$OUTPUT_ABS"

echo "Done: $OUTPUT_ABS"
