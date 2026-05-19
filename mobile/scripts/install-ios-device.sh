#!/usr/bin/env bash
#
# install-ios-device.sh — build & deploy LotteryDetection.Mobile to a
# physically connected iPhone over USB (or paired wireless).
#
# Prerequisites:
#   - macOS with Xcode 26.3+ selected (xcode-select -p)
#   - iOS workloads installed (run scripts/setup-dev.sh first)
#   - The iPhone is plugged in, unlocked, and "Trust This Computer" was tapped
#   - Your Apple ID is signed into Xcode (Settings → Accounts) with a team that
#     can sign com.lotterydetection.mobile (automatic provisioning will handle it)
#   - On the device: Settings → Privacy & Security → Developer Mode = ON
#
# Usage:
#   bash mobile/scripts/install-ios-device.sh                  # auto-pick first device, Debug
#   bash mobile/scripts/install-ios-device.sh -c Release       # Release config
#   bash mobile/scripts/install-ios-device.sh -u <UDID>        # target a specific device
#   bash mobile/scripts/install-ios-device.sh -l               # list devices and exit
#   bash mobile/scripts/install-ios-device.sh --no-run         # install only, don't launch

set -euo pipefail

CONFIG="Debug"
UDID=""
LIST_ONLY=0
RUN_APP=1

usage() {
  grep -E '^# ' "$0" | sed -E 's/^# ?//'
  exit "${1:-0}"
}

while [[ $# -gt 0 ]]; do
  case "$1" in
    -c|--configuration) CONFIG="$2"; shift 2 ;;
    -u|--udid)          UDID="$2";   shift 2 ;;
    -l|--list)          LIST_ONLY=1; shift ;;
    --no-run)           RUN_APP=0;   shift ;;
    -h|--help)          usage 0 ;;
    *) echo "Unknown arg: $1" >&2; usage 1 ;;
  esac
done

if [[ "$(uname)" != "Darwin" ]]; then
  echo "ERROR: iOS device deployment only works on macOS." >&2
  exit 1
fi

REPO_ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/../.." && pwd)"
PROJECT="$REPO_ROOT/mobile/LotteryDetection.Mobile/LotteryDetection.Mobile.csproj"

if [[ ! -f "$PROJECT" ]]; then
  echo "ERROR: project not found: $PROJECT" >&2
  exit 1
fi

for cmd in dotnet xcrun; do
  if ! command -v "$cmd" >/dev/null 2>&1; then
    echo "ERROR: '$cmd' not found on PATH." >&2
    exit 1
  fi
done

# ─── Discover connected devices ────────────────────────────────────────────────
# xcrun devicectl (Xcode 15+) is the modern path; fall back to xctrace.
discover_devices() {
  if xcrun devicectl list devices --json-output /tmp/.devicectl.json >/dev/null 2>&1; then
    python3 - /tmp/.devicectl.json <<'PY'
import json, sys
with open(sys.argv[1]) as f:
    data = json.load(f)
for d in data.get("result", {}).get("devices", []):
    props = d.get("deviceProperties", {})
    hw = d.get("hardwareProperties", {})
    conn = d.get("connectionProperties", {})
    if hw.get("platform") not in ("iOS", "iPadOS"):
        continue
    state = conn.get("tunnelState") or conn.get("pairingState") or ""
    udid = hw.get("udid") or d.get("identifier", "")
    name = props.get("name", "?")
    osver = props.get("osVersionNumber", "?")
    print(f"{udid}\t{name}\tiOS {osver}\t{state}")
PY
  else
    xcrun xctrace list devices 2>&1 \
      | awk -F'[()]' '/iPhone|iPad/ && !/Simulator/ {gsub(/^ +| +$/,"",$0); print $0}'
  fi
}

DEVICES="$(discover_devices || true)"

if [[ "$LIST_ONLY" -eq 1 ]]; then
  echo "Connected iOS devices:"
  if [[ -z "$DEVICES" ]]; then
    echo "  (none found — is the iPhone plugged in and unlocked?)"
  else
    echo "$DEVICES"
  fi
  exit 0
fi

if [[ -z "$DEVICES" ]]; then
  echo "ERROR: no connected iOS device detected." >&2
  echo "  • Plug in the iPhone via USB" >&2
  echo "  • Unlock it and tap 'Trust This Computer' if prompted" >&2
  echo "  • Enable Developer Mode (Settings → Privacy & Security → Developer Mode)" >&2
  echo "  • Re-run with --list to see what Xcode sees." >&2
  exit 1
fi

if [[ -z "$UDID" ]]; then
  UDID="$(echo "$DEVICES" | head -1 | awk -F'\t' '{print $1}')"
  if [[ -z "$UDID" ]]; then
    # xctrace fallback formatting: "Name (iOS 17.5) (UDID)"
    UDID="$(echo "$DEVICES" | head -1 | sed -E 's/.*\(([0-9A-Fa-f-]{25,})\).*/\1/')"
  fi
fi

if [[ -z "$UDID" ]]; then
  echo "ERROR: could not determine target device UDID. Use -u <UDID> explicitly." >&2
  echo "Detected:" >&2
  echo "$DEVICES" >&2
  exit 1
fi

echo "Repo:          $REPO_ROOT"
echo "Project:       $PROJECT"
echo "Configuration: $CONFIG"
echo "Target UDID:   $UDID"
echo "Run after install: $([[ $RUN_APP -eq 1 ]] && echo yes || echo no)"
echo

# ─── Build ─────────────────────────────────────────────────────────────────────
# We deliberately avoid `-t:Run`: the bundled mlaunch installdev step hangs
# silently on iOS 17+/26 devices. Instead build only, then push with devicectl.
echo "Step 1/3: building .app …"
dotnet build "$PROJECT" \
  -c "$CONFIG" \
  -f net9.0-ios \
  -r ios-arm64 \
  -p:BuildIpa=false

APP_PATH="$REPO_ROOT/mobile/LotteryDetection.Mobile/bin/$CONFIG/net9.0-ios/ios-arm64/LotteryDetection.Mobile.app"
if [[ ! -d "$APP_PATH" ]]; then
  echo "ERROR: expected .app not found at: $APP_PATH" >&2
  exit 1
fi

# ─── Install via devicectl ─────────────────────────────────────────────────────
echo
echo "Step 2/3: installing to device …"
INSTALL_OUTPUT="$(xcrun devicectl device install app --device "$UDID" "$APP_PATH" 2>&1)"
echo "$INSTALL_OUTPUT"

BUNDLE_ID="$(/usr/libexec/PlistBuddy -c 'Print :CFBundleIdentifier' "$APP_PATH/Info.plist" 2>/dev/null || echo "com.lotterydetection.mobile")"

if [[ "$RUN_APP" -eq 1 ]]; then
  echo
  echo "Step 3/3: launching $BUNDLE_ID …"
  xcrun devicectl device process launch --device "$UDID" "$BUNDLE_ID"
else
  echo
  echo "Step 3/3: skipped (--no-run). Bundle id: $BUNDLE_ID"
fi

echo
echo "Done."
echo "If install/launch failed with a signing error, open the project in Xcode once"
echo "so it can provision com.lotterydetection.mobile against your team, then re-run."
