#!/usr/bin/env bash
#
# setup-gcp.sh — one-time GCP/IAM/WIF bootstrap for the Cloud Run deploy
# workflow (.github/workflows/deploy-backend.yml).
#
# Creates (idempotently): Artifact Registry repo, runtime + deployer service
# accounts with the right IAM roles, and a Workload Identity Federation pool
# scoped to a single GitHub repo. Prints the GitHub Variables/Secrets values
# you need to paste into the repo settings at the end.
#
# Usage:
#   ./setup-gcp.sh \
#     --project-id <PROJECT_ID> \
#     --github-repo <OWNER>/<REPO> \
#     [--region asia-southeast1]
#
# Prerequisites:
#   - gcloud CLI authenticated as a user with project Owner / IAM Admin
#     (run `gcloud auth login` first).
#   - The project exists and billing is enabled.

set -euo pipefail

REGION="asia-southeast1"
PROJECT_ID=""
GITHUB_REPO=""

while [[ $# -gt 0 ]]; do
  case "$1" in
    --project-id)  PROJECT_ID="$2";  shift 2 ;;
    --github-repo) GITHUB_REPO="$2"; shift 2 ;;
    --region)      REGION="$2";      shift 2 ;;
    -h|--help)
      grep -E '^# ' "$0" | sed -E 's/^# ?//'
      exit 0
      ;;
    *) echo "Unknown arg: $1" >&2; exit 1 ;;
  esac
done

if [[ -z "$PROJECT_ID" || -z "$GITHUB_REPO" ]]; then
  echo "ERROR: --project-id and --github-repo are required." >&2
  echo "  Example: $0 --project-id lottery-detection-dev --github-repo acme/lottery" >&2
  exit 1
fi

if ! command -v gcloud >/dev/null 2>&1; then
  echo "ERROR: gcloud CLI not found. Install: https://cloud.google.com/sdk/docs/install" >&2
  exit 1
fi

ARTIFACT_REPO="lottery-detection"
RUNTIME_SA="lottery-runtime"
DEPLOYER_SA="lottery-deployer"
WIF_POOL="github"
WIF_PROVIDER="github-provider"

RUNTIME_SA_EMAIL="${RUNTIME_SA}@${PROJECT_ID}.iam.gserviceaccount.com"
DEPLOYER_SA_EMAIL="${DEPLOYER_SA}@${PROJECT_ID}.iam.gserviceaccount.com"

echo "Project:      $PROJECT_ID"
echo "Region:       $REGION"
echo "GitHub repo:  $GITHUB_REPO"
echo

gcloud config set project "$PROJECT_ID" --quiet >/dev/null

# ─── 1. Enable APIs ──────────────────────────────────────────────────────────
echo "[1/6] Enabling APIs (run, aiplatform, artifactregistry, iamcredentials, sqladmin, cloudscheduler) …"
gcloud services enable \
  run.googleapis.com \
  aiplatform.googleapis.com \
  artifactregistry.googleapis.com \
  iamcredentials.googleapis.com \
  sts.googleapis.com \
  sqladmin.googleapis.com \
  serviceusage.googleapis.com \
  cloudscheduler.googleapis.com \
  --quiet

# ─── 2. Artifact Registry repo ───────────────────────────────────────────────
echo "[2/6] Ensuring Artifact Registry repo '${ARTIFACT_REPO}' in ${REGION} …"
if ! gcloud artifacts repositories describe "$ARTIFACT_REPO" --location="$REGION" >/dev/null 2>&1; then
  gcloud artifacts repositories create "$ARTIFACT_REPO" \
    --repository-format=docker --location="$REGION" \
    --description="LotteryDetection container images" --quiet
else
  echo "  (already exists)"
fi

# ─── 3. Runtime SA + Vertex AI access ────────────────────────────────────────
echo "[3/6] Ensuring runtime SA '${RUNTIME_SA_EMAIL}' …"
if ! gcloud iam service-accounts describe "$RUNTIME_SA_EMAIL" >/dev/null 2>&1; then
  gcloud iam service-accounts create "$RUNTIME_SA" \
    --display-name="LotteryDetection Cloud Run runtime" --quiet
else
  echo "  (already exists)"
fi

gcloud projects add-iam-policy-binding "$PROJECT_ID" \
  --member="serviceAccount:${RUNTIME_SA_EMAIL}" \
  --role="roles/aiplatform.user" \
  --condition=None --quiet >/dev/null

# ─── 4. Deployer SA + deploy roles ───────────────────────────────────────────
echo "[4/6] Ensuring deployer SA '${DEPLOYER_SA_EMAIL}' …"
if ! gcloud iam service-accounts describe "$DEPLOYER_SA_EMAIL" >/dev/null 2>&1; then
  gcloud iam service-accounts create "$DEPLOYER_SA" \
    --display-name="LotteryDetection GitHub Actions deployer" --quiet
else
  echo "  (already exists)"
fi

for ROLE in roles/run.admin roles/artifactregistry.writer roles/iam.serviceAccountUser roles/serviceusage.serviceUsageViewer; do
  gcloud projects add-iam-policy-binding "$PROJECT_ID" \
    --member="serviceAccount:${DEPLOYER_SA_EMAIL}" \
    --role="$ROLE" --condition=None --quiet >/dev/null
done

# ─── 5. WIF pool + provider ──────────────────────────────────────────────────
echo "[5/6] Ensuring Workload Identity pool '${WIF_POOL}' + provider '${WIF_PROVIDER}' …"
if ! gcloud iam workload-identity-pools describe "$WIF_POOL" --location=global >/dev/null 2>&1; then
  gcloud iam workload-identity-pools create "$WIF_POOL" \
    --location=global --display-name="GitHub Actions" --quiet
else
  echo "  pool already exists"
fi

if ! gcloud iam workload-identity-pools providers describe "$WIF_PROVIDER" \
      --location=global --workload-identity-pool="$WIF_POOL" >/dev/null 2>&1; then
  gcloud iam workload-identity-pools providers create-oidc "$WIF_PROVIDER" \
    --location=global \
    --workload-identity-pool="$WIF_POOL" \
    --issuer-uri="https://token.actions.githubusercontent.com" \
    --attribute-mapping="google.subject=assertion.sub,attribute.repository=assertion.repository,attribute.repository_owner=assertion.repository_owner" \
    --attribute-condition="assertion.repository==\"${GITHUB_REPO}\"" \
    --quiet
else
  echo "  provider already exists (attribute-condition not updated; remove + re-run to change repo binding)"
fi

# ─── 6. Bind WIF principalSet → deployer SA ──────────────────────────────────
echo "[6/6] Binding GitHub repo '${GITHUB_REPO}' to deployer SA …"
PROJECT_NUMBER=$(gcloud projects describe "$PROJECT_ID" --format='value(projectNumber)')
PRINCIPAL_SET="principalSet://iam.googleapis.com/projects/${PROJECT_NUMBER}/locations/global/workloadIdentityPools/${WIF_POOL}/attribute.repository/${GITHUB_REPO}"

gcloud iam service-accounts add-iam-policy-binding "$DEPLOYER_SA_EMAIL" \
  --role=roles/iam.workloadIdentityUser \
  --member="$PRINCIPAL_SET" --quiet >/dev/null

# ─── Summary ─────────────────────────────────────────────────────────────────
WIF_PROVIDER_RESOURCE="projects/${PROJECT_NUMBER}/locations/global/workloadIdentityPools/${WIF_POOL}/providers/${WIF_PROVIDER}"

cat <<EOF

══════════════════════════════════════════════════════════════════════════════
 Done. Now paste these into GitHub → Settings → Secrets and variables → Actions.
══════════════════════════════════════════════════════════════════════════════

Variables:
  GCP_PROJECT_ID   = ${PROJECT_ID}
  GCP_LOCATION     = ${REGION}

Secrets:
  GCP_WIF_PROVIDER = ${WIF_PROVIDER_RESOURCE}
  GCP_DEPLOYER_SA  = ${DEPLOYER_SA_EMAIL}
  GCP_RUNTIME_SA   = ${RUNTIME_SA_EMAIL}

Next steps:
  - Push to main (or run the 'Deploy backend to Cloud Run' workflow manually).
  - First run will build the image and deploy. The service will start but ABP
    will crash on DB connect until a real ConnectionStrings:Default is wired
    (Cloud SQL or similar).
EOF
