#!/usr/bin/env bash
#
# setup-external-auth.sh — register Microsoft + Google + Facebook client credentials
# with Google Secret Manager so the Cloud Run deploy can inject them as
# env vars. The values themselves come from app registrations you create
# in the respective developer consoles (see README block at the bottom).
#
# Usage (interactive — prompts for each value):
#   ./setup-external-auth.sh --project-id <PROJECT_ID>
#
# Or pass values directly (useful for re-runs / CI):
#   ./setup-external-auth.sh --project-id <PID> \
#     --ms-client-id <ID> --ms-client-secret <SECRET> \
#     --google-client-id <ID> --google-client-secret <SECRET> \
#     --facebook-app-id <ID> --facebook-app-secret <SECRET>
#
# Re-running is safe — each `gcloud secrets create` is guarded; existing
# secrets get a new version via `gcloud secrets versions add`.

set -euo pipefail

PROJECT_ID=""
MS_CLIENT_ID=""
MS_CLIENT_SECRET=""
GOOGLE_CLIENT_ID=""
GOOGLE_CLIENT_SECRET=""
FACEBOOK_APP_ID=""
FACEBOOK_APP_SECRET=""
RUNTIME_SA="lottery-runtime"

while [[ $# -gt 0 ]]; do
  case "$1" in
    --project-id)          PROJECT_ID="$2";           shift 2 ;;
    --ms-client-id)        MS_CLIENT_ID="$2";         shift 2 ;;
    --ms-client-secret)    MS_CLIENT_SECRET="$2";     shift 2 ;;
    --google-client-id)    GOOGLE_CLIENT_ID="$2";     shift 2 ;;
    --google-client-secret) GOOGLE_CLIENT_SECRET="$2"; shift 2 ;;
    --facebook-app-id)     FACEBOOK_APP_ID="$2";      shift 2 ;;
    --facebook-app-secret) FACEBOOK_APP_SECRET="$2";  shift 2 ;;
    -h|--help)
      grep -E '^# ' "$0" | sed -E 's/^# ?//'
      exit 0
      ;;
    *) echo "Unknown arg: $1" >&2; exit 1 ;;
  esac
done

if [[ -z "$PROJECT_ID" ]]; then
  echo "ERROR: --project-id is required." >&2
  exit 1
fi

if ! command -v gcloud >/dev/null 2>&1; then
  echo "ERROR: gcloud CLI not found." >&2
  exit 1
fi

prompt_if_empty() {
  local var_name="$1" prompt="$2" silent="${3:-0}"
  local value="${!var_name}"
  if [[ -z "$value" ]]; then
    if [[ "$silent" == "1" ]]; then
      read -rs -p "$prompt: " value
      echo
    else
      read -r -p "$prompt: " value
    fi
    printf -v "$var_name" '%s' "$value"
  fi
}

prompt_if_empty MS_CLIENT_ID      "Microsoft Application (client) ID"
prompt_if_empty MS_CLIENT_SECRET  "Microsoft client secret" 1
prompt_if_empty GOOGLE_CLIENT_ID  "Google OAuth client ID"
prompt_if_empty GOOGLE_CLIENT_SECRET "Google OAuth client secret" 1
prompt_if_empty FACEBOOK_APP_ID     "Facebook App ID"
prompt_if_empty FACEBOOK_APP_SECRET "Facebook App Secret" 1

upsert_secret() {
  local name="$1" value="$2"
  if gcloud secrets describe "$name" --project="$PROJECT_ID" >/dev/null 2>&1; then
    printf '%s' "$value" | gcloud secrets versions add "$name" \
      --data-file=- --project="$PROJECT_ID" --quiet >/dev/null
    echo "  updated: $name"
  else
    printf '%s' "$value" | gcloud secrets create "$name" \
      --data-file=- --replication-policy=automatic \
      --project="$PROJECT_ID" --quiet >/dev/null
    echo "  created: $name"
  fi

  gcloud secrets add-iam-policy-binding "$name" \
    --member="serviceAccount:${RUNTIME_SA}@${PROJECT_ID}.iam.gserviceaccount.com" \
    --role=roles/secretmanager.secretAccessor \
    --project="$PROJECT_ID" --quiet >/dev/null
}

echo "Storing secrets in project $PROJECT_ID …"
upsert_secret auth-microsoft-client-id     "$MS_CLIENT_ID"
upsert_secret auth-microsoft-client-secret "$MS_CLIENT_SECRET"
upsert_secret auth-google-client-id        "$GOOGLE_CLIENT_ID"
upsert_secret auth-google-client-secret    "$GOOGLE_CLIENT_SECRET"
upsert_secret auth-facebook-app-id         "$FACEBOOK_APP_ID"
upsert_secret auth-facebook-app-secret     "$FACEBOOK_APP_SECRET"

cat <<EOF

══════════════════════════════════════════════════════════════════════════════
 Done. The next backend deploy will pick these up via Secret Manager.
══════════════════════════════════════════════════════════════════════════════

App-registration cheat sheet (one-time per provider):

Microsoft (Entra ID)
  1. https://entra.microsoft.com → Identity → Applications → App registrations
     → New registration
       Name:        LotteryDetection
       Supported account types: Personal Microsoft accounts + work/school
                                (multi-tenant + MSA)
       Redirect URI: Mobile and desktop → msauth://com.lotterydetection.mobile/MSAL_REDIRECT
  2. Copy "Application (client) ID" → MS_CLIENT_ID
  3. Certificates & secrets → New client secret → copy value → MS_CLIENT_SECRET
  4. Authentication → Add platform → iOS/macOS → Bundle ID com.lotterydetection.mobile
     (Entra autofills the msauth:// redirect)

Google Cloud (OAuth 2.0)
  1. https://console.cloud.google.com/apis/credentials → CREATE CREDENTIALS
     → OAuth client ID
       Application type: iOS
       Bundle ID:        com.lotterydetection.mobile
  2. Copy Client ID → GOOGLE_CLIENT_ID (Google does NOT issue a secret for
     iOS clients — leave GOOGLE_CLIENT_SECRET empty if prompted again, OR
     create a separate "Web application" client and use its secret here)
  3. Configure the OAuth consent screen if you haven't already (External,
     scope: openid + email + profile is enough).

Facebook
  1. https://developers.facebook.com/apps → Create App → Consumer
       Name: DòVéSố AI
  2. Add product "Facebook Login for iOS"
       Bundle ID: com.lotterydetection.mobile
  3. Copy the numeric App ID → FACEBOOK_APP_ID
  4. Settings → Basic → Show App Secret → copy → FACEBOOK_APP_SECRET
EOF
