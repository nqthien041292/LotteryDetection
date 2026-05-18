#!/usr/bin/env bash
#
# setup-dev.sh — one-shot MAUI dev environment bootstrap.
# Run this once after cloning the repo (and again whenever global.json bumps the SDK).
#
# What it does:
#   1. Verifies the .NET SDK pinned in repo-root global.json is installed.
#   2. Restores MAUI workloads (android, ios, maccatalyst) at the version that ships with the pinned SDK.
#   3. (macOS) Confirms Xcode is selected and visible to the workload.
#
# Why pin workloads:
#   MAUI iOS workload version dictates the minimum Xcode it accepts. Mismatch yields
#   "requires Xcode 26.X" errors. The workload-set version pinned in global.json
#   (sdk.workloadVersion) ships iOS manifest 26.2.10233 — the latest pre-26.4 build
#   that works with Xcode 26.3. Bumping to 10.0.107.1 or beyond drags iOS up to
#   26.4.10259 which requires Xcode 26.4 (not yet installed).

set -euo pipefail

REPO_ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/../.." && pwd)"
cd "$REPO_ROOT"

REQUIRED_SDK="$(node -e "console.log(require('$REPO_ROOT/global.json').sdk.version)" 2>/dev/null \
  || python3 -c "import json; print(json.load(open('$REPO_ROOT/global.json'))['sdk']['version'])" 2>/dev/null \
  || grep -oE '"version"\s*:\s*"[^"]+"' "$REPO_ROOT/global.json" | head -1 | sed -E 's/.*"([^"]+)"$/\1/')"

echo "Repo:      $REPO_ROOT"
echo "Pinned .NET SDK: $REQUIRED_SDK (per global.json)"
echo

# 1. Verify SDK presence.
if ! command -v dotnet >/dev/null 2>&1; then
  echo "ERROR: 'dotnet' is not on PATH. Install .NET SDK $REQUIRED_SDK from https://dot.net" >&2
  exit 1
fi

if ! dotnet --list-sdks | awk '{print $1}' | grep -qx "$REQUIRED_SDK"; then
  echo "WARNING: SDK $REQUIRED_SDK is not installed locally. rollForward in global.json may pick"
  echo "         a nearby patch, but for strict reproducibility install $REQUIRED_SDK."
fi

CURRENT_SDK="$(dotnet --version)"
echo "Active SDK: $CURRENT_SDK"
echo

# 2. Restore workloads (this honors global.json — keeps workload-set in sync with the SDK).
echo "Restoring MAUI workloads against mobile/LotteryDetectionMobile/LotteryDetectionMobile.csproj ..."
dotnet workload restore mobile/LotteryDetectionMobile/LotteryDetectionMobile.csproj

echo
echo "Installed workloads:"
dotnet workload list

# 3. macOS: surface Xcode version (lets devs confirm 26.3+ is selected).
if [[ "$(uname)" == "Darwin" ]]; then
  echo
  if command -v xcodebuild >/dev/null 2>&1; then
    echo "Xcode: $(xcodebuild -version | head -1)"
    echo "Path:  $(xcode-select -p)"
  else
    echo "WARNING: xcodebuild not found. Run 'sudo xcode-select -s /Applications/Xcode.app' after installing Xcode."
  fi
fi

echo
echo "Done. You can now build:"
echo "    dotnet build mobile/LotteryDetectionMobile -f net9.0-ios"
echo "    dotnet build mobile/LotteryDetectionMobile -f net9.0-android"
