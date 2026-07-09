$ApiBaseUrl = "http://localhost:5055"
$AdminEmail = "admin@localhost"
$AdminPassword = "Admin123!"

$quarterFinalTeams = @(
    @{ Name = "Switzerland"; CountryId = 167; Group = "QF1" },
    @{ Name = "Argentina"; CountryId = 6; Group = "QF1" },
    @{ Name = "England"; CountryId = 193; Group = "QF2" },
    @{ Name = "France"; CountryId = 59; Group = "QF2" },
    @{ Name = "Morocco"; CountryId = 118; Group = "QF3" },
    @{ Name = "Norway"; CountryId = 129; Group = "QF3" },
    @{ Name = "Spain"; CountryId = 161; Group = "QF4" },
    @{ Name = "Belgium"; CountryId = 14; Group = "QF4" }
)

$ErrorActionPreference = "Stop"

$loginBody = @{ email = $AdminEmail; password = $AdminPassword } | ConvertTo-Json
$loginResponse = Invoke-RestMethod -Uri "$ApiBaseUrl/User/Login" -Method Post -Body $loginBody -ContentType "application/json"
$headers = @{ Authorization = "Bearer $($loginResponse.token)" }

$worldCups = Invoke-RestMethod -Uri "$ApiBaseUrl/WorldCup/GetWorldCups" -Headers $headers
$worldCupId = $worldCups[0].id
Write-Host "WorldCup ID: $worldCupId"

$existingGroups = Invoke-RestMethod -Uri "$ApiBaseUrl/Group/GetGroups" -Headers $headers
$groupIds = @{}
foreach ($groupName in @("QF1", "QF2", "QF3", "QF4")) {
    $existing = $existingGroups | Where-Object { $_.name -eq $groupName -and $_.worldCupId -eq $worldCupId }
    if ($existing) {
        $groupIds[$groupName] = $existing.id
        Write-Host "Group $groupName already exists (id $($existing.id))"
    } else {
        $body = @{ name = $groupName; worldCupId = $worldCupId } | ConvertTo-Json
        Invoke-RestMethod -Uri "$ApiBaseUrl/Group/AddGroup" -Method Post -Body $body -ContentType "application/json" -Headers $headers | Out-Null
        $existingGroups = Invoke-RestMethod -Uri "$ApiBaseUrl/Group/GetGroups" -Headers $headers
        $created = $existingGroups | Where-Object { $_.name -eq $groupName -and $_.worldCupId -eq $worldCupId }
        $groupIds[$groupName] = $created.id
        Write-Host "Created group $groupName (id $($created.id))"
    }
}

$existingTeams = Invoke-RestMethod -Uri "$ApiBaseUrl/Team/GetTeams" -Headers $headers
foreach ($team in $quarterFinalTeams) {
    $already = $existingTeams | Where-Object { $_.countryId -eq $team.CountryId }
    if ($already) {
        Write-Host "Team $($team.Name) already exists (id $($already.id))"
        continue
    }

    $body = @{
        countryId = $team.CountryId
        groupId = $groupIds[$team.Group]
    } | ConvertTo-Json

    Invoke-RestMethod -Uri "$ApiBaseUrl/Team/AddTeam" -Method Post -Body $body -ContentType "application/json" -Headers $headers | Out-Null
    Write-Host "Added team $($team.Name) to $($team.Group)"
}

$finalTeams = Invoke-RestMethod -Uri "$ApiBaseUrl/Team/GetTeams" -Headers $headers
Write-Host "`nAll teams:"
$finalTeams | Sort-Object countryName | ForEach-Object { Write-Host "  $($_.countryName) (group $($_.groupName))" }
