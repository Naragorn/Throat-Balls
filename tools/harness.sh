#!/usr/bin/env bash
# Builds the mod and runs the in-game scenarios headless through BasHarness (Linux + Proton only).
#   tools/harness.sh                      # all harness/*.txt, in name order
#   tools/harness.sh harness/smoke.txt
# Every run starts from a fresh harness prefix (default options). Only one harness run
# at a time (BasHarness holds a lock). BAS_HARNESS_DIR overrides where BasHarness lives
# (default: ../BasHarness next to this repo). Template: AchillesBAS tools/harness.sh.
set -euo pipefail
ROOT=$(cd "$(dirname "$0")/.." && pwd)
HARNESS=${BAS_HARNESS_DIR:-$ROOT/../BasHarness}
export DOTNET_ROOT=${DOTNET_ROOT:-$HOME/.dotnet}
export PATH="$DOTNET_ROOT:$PATH" DOTNET_CLI_TELEMETRY_OPTOUT=1 DOTNET_NOLOGO=1
dotnet build "$ROOT/src/ThroatBalls/ThroatBalls.csproj" -c Release -nologo -v q
if [ $# -eq 0 ]; then
  set -- "$ROOT"/harness/*.txt
fi
exec python3 "$HARNESS/runner/bas_harness.py" \
  --mod "$ROOT/dist/ThroatBalls" \
  --artifacts "$ROOT/artifacts/harness" --fresh-prefix "$@"
