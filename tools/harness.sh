#!/usr/bin/env bash
# Builds the mod and runs the in-game scenarios headless through BasHarness (Linux + Proton only).
#   tools/harness.sh                      # all harness/*.txt, in name order
#   tools/harness.sh harness/smoke.txt
#   tools/harness.sh --as-shipped harness/films/sound.txt   # the mod as it ships, without the test sounds
# Every run starts from a fresh harness prefix (default options). Only one harness run
# at a time (BasHarness holds a lock). BAS_HARNESS_DIR overrides where BasHarness lives
# (default: ../BasHarness next to this repo). Template: AchillesBAS tools/harness.sh.
set -euo pipefail
ROOT=$(cd "$(dirname "$0")/.." && pwd)
HARNESS=${BAS_HARNESS_DIR:-$ROOT/../BasHarness}
export DOTNET_ROOT=${DOTNET_ROOT:-$HOME/.dotnet}
export PATH="$DOTNET_ROOT:$PATH" DOTNET_CLI_TELEMETRY_OPTOUT=1 DOTNET_NOLOGO=1
FIXTURES=true
if [ "${1:-}" = "--as-shipped" ]; then
  FIXTURES=false
  shift
fi
dotnet build "$ROOT/src/ThroatBalls/ThroatBalls.csproj" -c Release -nologo -v q
if [ $# -eq 0 ]; then
  set -- "$ROOT"/harness/*.txt
fi
# The runs use a copy of the mod with the test sounds of harness/fixtures added to Sounds/, next to the shipped
# throat-choke.wav (--as-shipped: without them).
STAGE="$ROOT/artifacts/harness-mod/ThroatBalls"
rm -rf "$STAGE"
mkdir -p "$(dirname "$STAGE")"
cp -r "$ROOT/dist/ThroatBalls" "$STAGE"
if [ "$FIXTURES" = true ]; then
  cp "$ROOT"/harness/fixtures/Sounds/* "$STAGE/Sounds/"
fi
exec python3 "$HARNESS/runner/bas_harness.py" \
  --mod "$STAGE" \
  --artifacts "$ROOT/artifacts/harness" --fresh-prefix "$@"
