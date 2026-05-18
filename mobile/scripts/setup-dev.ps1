# setup-dev.ps1 — one-shot MAUI dev environment bootstrap (Windows / PowerShell).
# Mirrors mobile/scripts/setup-dev.sh. Run once after cloning the repo and again
# whenever global.json bumps the SDK pin.

$ErrorActionPreference = 'Stop'
$RepoRoot = Resolve-Path (Join-Path $PSScriptRoot '..\..')
Set-Location $RepoRoot

$globalJson = Get-Content (Join-Path $RepoRoot 'global.json') -Raw | ConvertFrom-Json
$RequiredSdk = $globalJson.sdk.version

Write-Host "Repo:             $RepoRoot"
Write-Host "Pinned .NET SDK:  $RequiredSdk (per global.json)"
Write-Host ''

if (-not (Get-Command dotnet -ErrorAction SilentlyContinue)) {
    Write-Error "'dotnet' is not on PATH. Install .NET SDK $RequiredSdk from https://dot.net"
}

$installedSdks = (& dotnet --list-sdks) | ForEach-Object { ($_ -split ' ')[0] }
if ($installedSdks -notcontains $RequiredSdk) {
    Write-Warning "SDK $RequiredSdk is not installed locally. rollForward in global.json may pick a nearby patch, but for strict reproducibility install $RequiredSdk."
}

Write-Host "Active SDK:       $(& dotnet --version)"
Write-Host ''
Write-Host 'Restoring MAUI workloads against mobile/LotteryDetectionMobile/LotteryDetectionMobile.csproj ...'
& dotnet workload restore (Join-Path $RepoRoot 'mobile\LotteryDetectionMobile\LotteryDetectionMobile.csproj')

Write-Host ''
Write-Host 'Installed workloads:'
& dotnet workload list

Write-Host ''
Write-Host 'Done. You can now build:'
Write-Host '    dotnet build mobile/LotteryDetectionMobile -f net9.0-android'
