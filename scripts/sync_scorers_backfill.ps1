# Scorers-only backfill via Admin SyncScorersForWorldCup (Phase 11 Task 6).
# Does not change FT scores or re-resolve bets. Requires mapped ExternalMatchId (+ stage).
#
# Usage:
#   .\scripts\sync_scorers_backfill.ps1
#   .\scripts\sync_scorers_backfill.ps1 -WorldCupId 1
#   .\scripts\sync_scorers_backfill.ps1 -ApiBaseUrl http://localhost:5055 -MatchId 42
#
# Ops runbook: map ExternalMatchId/ExternalStageId → run this (or Admin SyncScorers) → verify fixtures Recent events.

param(
    [string]$ApiBaseUrl = "http://localhost:5055",
    [string]$AdminEmail = "admin@localhost",
    [string]$AdminPassword = "Admin123!",
    [int]$WorldCupId = 0,
    [int]$MatchId = 0
)

$ErrorActionPreference = "Stop"

$loginBody = @{ email = $AdminEmail; password = $AdminPassword } | ConvertTo-Json
$loginResponse = Invoke-RestMethod -Uri "$ApiBaseUrl/User/Login" -Method Post -Body $loginBody -ContentType "application/json"
$headers = @{ Authorization = "Bearer $($loginResponse.token)" }

if ($MatchId -gt 0) {
    Write-Host "SyncScorers for match $MatchId ..."
    $result = Invoke-RestMethod -Uri "$ApiBaseUrl/Match/SyncScorers/$MatchId" -Method Post -Headers $headers
    $result | ConvertTo-Json -Depth 6
    exit 0
}

if ($WorldCupId -le 0) {
    $worldCups = Invoke-RestMethod -Uri "$ApiBaseUrl/WorldCup/GetWorldCups" -Headers $headers
    $WorldCupId = $worldCups[0].id
    Write-Host "Using WorldCup ID: $WorldCupId"
}

Write-Host "SyncScorersForWorldCup worldCupId=$WorldCupId ..."
$batch = Invoke-RestMethod `
    -Uri "$ApiBaseUrl/Match/SyncScorersForWorldCup?worldCupId=$WorldCupId" `
    -Method Post `
    -Headers $headers

Write-Host $batch.message
$batch | ConvertTo-Json -Depth 6
