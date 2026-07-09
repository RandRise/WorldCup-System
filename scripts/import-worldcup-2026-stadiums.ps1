$ApiBaseUrl = "http://localhost:5055"
$AdminEmail = "admin@localhost"
$AdminPassword = "Admin123!"
$StadiumCsvPath = Join-Path $PSScriptRoot "..\Core\StadiumCsvData\Stadiums.csv"

$ErrorActionPreference = "Stop"

if (-not (Test-Path $StadiumCsvPath)) {
    throw "Stadium CSV not found at $StadiumCsvPath"
}

$loginBody = @{ email = $AdminEmail; password = $AdminPassword } | ConvertTo-Json
$loginResponse = Invoke-RestMethod -Uri "$ApiBaseUrl/User/Login" -Method Post -Body $loginBody -ContentType "application/json"
$headers = @{ Authorization = "Bearer $($loginResponse.token)" }

$existing = Invoke-RestMethod -Uri "$ApiBaseUrl/Stadium/GetStadiums" -Headers $headers
if ($existing.Count -gt 0) {
    Write-Host "Stadiums already present ($($existing.Count)). Skipping import."
    $existing | Sort-Object name | ForEach-Object { Write-Host "  $($_.name)" }
    return
}

$token = $loginResponse.token
$curlResult = curl.exe -s -X POST "$ApiBaseUrl/Stadium/ImportStadiumsFromCsv" `
    -H "Authorization: Bearer $token" `
    -F "File=@$StadiumCsvPath"
Write-Host $curlResult

$stadiums = Invoke-RestMethod -Uri "$ApiBaseUrl/Stadium/GetStadiums" -Headers $headers
Write-Host "`n$($stadiums.Count) stadiums in database:"
$stadiums | Sort-Object name | ForEach-Object { Write-Host "  $($_.name)" }
