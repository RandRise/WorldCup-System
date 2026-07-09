# Starts the WorldCup API after freeing port 5055 and ensuring PostgreSQL is up.
# Usage (from repo WorldCup-System folder or anywhere):
#   .\scripts\run-api.ps1
#   .\scripts\run-api.ps1 -Port 5055

param(
    [int]$Port = 5055,
    [string]$PostgresServiceName = "postgresql-x64-16"
)

$ErrorActionPreference = "Stop"

function Get-PidsListeningOnPort {
    param([int]$ListenPort)

    $pids = @()
    $lines = netstat -ano | Select-String ":$ListenPort\s+\S+\s+LISTENING"
    foreach ($line in $lines) {
        $parts = ($line.ToString() -split '\s+') | Where-Object { $_ -ne "" }
        if ($parts.Length -ge 5) {
            [int]$processId = 0
            if ([int]::TryParse($parts[-1], [ref]$processId) -and $processId -gt 0) {
                $pids += $processId
            }
        }
    }

    return ($pids | Select-Object -Unique)
}

function Stop-ListenersOnPort {
    param([int]$ListenPort)

    $pids = Get-PidsListeningOnPort -ListenPort $ListenPort
    if ($pids.Count -eq 0) {
        Write-Host "Port $ListenPort is free."
        return
    }

    foreach ($processId in $pids) {
        $process = Get-Process -Id $processId -ErrorAction SilentlyContinue
        $name = if ($null -ne $process) { $process.ProcessName } else { "unknown" }
        Write-Host "Stopping PID $processId ($name) on port $ListenPort..."
        Stop-Process -Id $processId -Force -ErrorAction SilentlyContinue
    }

    Start-Sleep -Seconds 1

    $remaining = Get-PidsListeningOnPort -ListenPort $ListenPort
    if ($remaining.Count -gt 0) {
        throw "Port $ListenPort is still in use by PID(s): $($remaining -join ', '). Stop them manually, then retry."
    }

    Write-Host "Port $ListenPort is free."
}

function Ensure-PostgresRunning {
    param([string]$ServiceName)

    $service = Get-Service -Name $ServiceName -ErrorAction SilentlyContinue
    if ($null -eq $service) {
        Write-Host "PostgreSQL service '$ServiceName' not found — skipping auto-start. Ensure Postgres is running on 5432."
        return
    }

    if ($service.Status -eq "Running") {
        Write-Host "PostgreSQL ($ServiceName) is running."
        return
    }

    Write-Host "Starting PostgreSQL ($ServiceName)..."
    try {
        Start-Service -Name $ServiceName
    }
    catch {
        Write-Host "Could not start '$ServiceName' without elevation. Starting elevated prompt..."
        $arg = "-NoProfile -Command `"Start-Service -Name '$ServiceName'`""
        Start-Process -FilePath "powershell.exe" -ArgumentList $arg -Verb RunAs -Wait | Out-Null
    }

    Start-Sleep -Seconds 2
    $service.Refresh()
    if ($service.Status -ne "Running") {
        throw "PostgreSQL service '$ServiceName' is not running. Start it from Services.msc, then retry."
    }

    Write-Host "PostgreSQL ($ServiceName) is running."
}

$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$solutionRoot = Split-Path -Parent $scriptDir
$projectPath = Join-Path $solutionRoot "WorldCup-System\WorldCup-System.csproj"

if (-not (Test-Path $projectPath)) {
    throw "API project not found at $projectPath"
}

Ensure-PostgresRunning -ServiceName $PostgresServiceName
Stop-ListenersOnPort -ListenPort $Port

Write-Host "Starting API: dotnet run --project `"$projectPath`""
Set-Location $solutionRoot
dotnet run --project $projectPath
