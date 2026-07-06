param(
    [string]$ApiBaseUrl = "http://localhost:5055",
    [string]$AdminEmail = "admin@localhost",
    [string]$AdminPassword = "Admin123!"
)

$ErrorActionPreference = "Stop"

function Wait-ForApi {
    param([string]$Url, [int]$MaxAttempts = 30)
    for ($attempt = 1; $attempt -le $MaxAttempts; $attempt++) {
        try {
            $response = Invoke-WebRequest -Uri "$Url/health" -UseBasicParsing -TimeoutSec 5
            if ($response.StatusCode -eq 200) {
                Write-Host "API is ready at $Url"
                return
            }
        }
        catch {
            Write-Host "Waiting for API ($attempt/$MaxAttempts)..."
            Start-Sleep -Seconds 2
        }
    }
    throw "API did not become ready at $Url"
}

Wait-ForApi -Url $ApiBaseUrl

$loginBody = @{
    email    = $AdminEmail
    password = $AdminPassword
} | ConvertTo-Json

$loginResponse = Invoke-RestMethod -Uri "$ApiBaseUrl/User/Login" -Method Post -Body $loginBody -ContentType "application/json"
$token = $loginResponse.token

if ([string]::IsNullOrWhiteSpace($token)) {
    throw "Login succeeded but no token was returned."
}

$headers = @{
    Authorization = "Bearer $token"
}

$seedResult = Invoke-RestMethod -Uri "$ApiBaseUrl/Seed/LoadWorldCup2026Demo" -Method Post -Headers $headers
Write-Host "Demo seed completed:"
$seedResult | ConvertTo-Json -Depth 5
