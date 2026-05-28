#!/bin/bash

set -e

echo "=== Cài đặt Google Cloud Scheduler và API Key ==="

# 1. Các biến mặc định
PROJECT_ID=$(gcloud config get-value project)
REGION="asia-southeast1"
SERVICE_NAME="lottery-detection-api"
JOB_NAME="lottery-result-checker"
SECRET_NAME="cloud-scheduler-job-key"

# Khởi tạo một chuỗi ngẫu nhiên dài 32 ký tự làm khóa bảo mật
JOB_KEY=$(openssl rand -hex 16)

echo "Dự án hiện tại: $PROJECT_ID"
echo "API Key được tạo: $JOB_KEY"
echo "---------------------------------------------------"

echo "[1/4] Tạo Secret trên Secret Manager: $SECRET_NAME"
# Tạo mới hoặc cập nhật version nếu đã tồn tại
echo -n "$JOB_KEY" | gcloud secrets create "$SECRET_NAME" --data-file=- 2>/dev/null || \
echo -n "$JOB_KEY" | gcloud secrets versions add "$SECRET_NAME" --data-file=-

echo "[2/4] Cấp quyền đọc Secret cho Service Account của Cloud Run"
RUNTIME_SA="lottery-runtime@${PROJECT_ID}.iam.gserviceaccount.com"
gcloud secrets add-iam-policy-binding "$SECRET_NAME" \
  --member="serviceAccount:$RUNTIME_SA" \
  --role="roles/secretmanager.secretAccessor" > /dev/null

echo "[3/4] Lấy địa chỉ của Cloud Run API"
SERVICE_URL=$(gcloud run services describe "$SERVICE_NAME" --region "$REGION" --format='value(status.url)')
if [ -z "$SERVICE_URL" ]; then
  echo "Lỗi: Không tìm thấy dịch vụ Cloud Run '$SERVICE_NAME'."
  exit 1
fi
API_URL="${SERVICE_URL}/api/services/app/LotteryAnalysis/CheckPendingResults"
echo "Đường dẫn API: $API_URL"

echo "[4/4] Tạo Cloud Scheduler Job"
# Chạy mỗi 5 phút (12 lần/giờ) từ 16h đến 18h59 giờ Việt Nam — bám sát livestream xổ số
gcloud scheduler jobs create http "$JOB_NAME" \
  --schedule="*/5 16-18 * * *" \
  --time-zone="Asia/Ho_Chi_Minh" \
  --uri="$API_URL" \
  --http-method=POST \
  --headers="X-CloudScheduler-Job-Key=$JOB_KEY" \
  --location="$REGION" \
  --message-body="{}" 2>/dev/null || \
gcloud scheduler jobs update http "$JOB_NAME" \
  --schedule="*/5 16-18 * * *" \
  --time-zone="Asia/Ho_Chi_Minh" \
  --uri="$API_URL" \
  --http-method=POST \
  --update-headers="X-CloudScheduler-Job-Key=$JOB_KEY" \
  --location="$REGION" \
  --message-body="{}"

echo "---------------------------------------------------"
echo "=== HOÀN TẤT CÀI ĐẶT GCP! ==="
echo "Tôi đã tự động cập nhật file deploy GitHub Actions để Cloud Run map cấu hình CloudScheduler__JobKey."
echo "Bạn chỉ cần commit file deploy lên Github, sau đó copy toàn bộ nội dung file này chạy trên Cloud Shell là xong."
