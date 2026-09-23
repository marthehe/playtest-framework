[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [string]$Repository,

    [Parameter(Mandatory = $true)]
    [string]$Token,

    [string]$Workflow = "quality.yml",

    [long]$CurrentRunId = 0,

    [ValidateRange(1, 50)]
    [int]$MaxRuns = 10,

    [string]$Destination = ".test-history",

    [string]$ApiBaseUrl = "https://api.github.com"
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$headers = @{
    Accept                 = "application/vnd.github+json"
    "User-Agent"           = "PlayTest-CI"
    "X-GitHub-Api-Version" = "2022-11-28"
}
$secureToken = ConvertTo-SecureString $Token -AsPlainText -Force

if (Test-Path $Destination) {
    Remove-Item $Destination -Recurse -Force
}
New-Item -ItemType Directory -Path $Destination | Out-Null

$encodedWorkflow = [Uri]::EscapeDataString($Workflow)
$requestedRuns = [Math]::Min($MaxRuns * 3, 100)
$runsUri =
    "$ApiBaseUrl/repos/$Repository/actions/workflows/$encodedWorkflow/runs" +
    "?branch=main&status=completed&per_page=$requestedRuns"
$runsResponse = Invoke-RestMethod `
    -Uri $runsUri `
    -Headers $headers `
    -Authentication Bearer `
    -Token $secureToken `
    -Method Get
$runs = @($runsResponse.workflow_runs) |
    Where-Object {
        $_.event -eq "push" -and
        [long]$_.id -ne $CurrentRunId
    }

$downloadedRuns = 0
$downloadedTrxFiles = 0

foreach ($run in $runs) {
    if ($downloadedRuns -ge $MaxRuns) {
        break
    }

    $artifactsUri = "$ApiBaseUrl/repos/$Repository/actions/runs/$($run.id)/artifacts?per_page=100"
    $artifactsResponse = Invoke-RestMethod `
        -Uri $artifactsUri `
        -Headers $headers `
        -Authentication Bearer `
        -Token $secureToken `
        -Method Get
    $artifact = @($artifactsResponse.artifacts) |
        Where-Object {
            $_.name -eq "test-results" -and
            -not $_.expired
        } |
        Sort-Object -Property created_at -Descending |
        Select-Object -First 1

    if ($null -eq $artifact) {
        Write-Output "Run $($run.id) has no retained test-results artifact; skipping."
        continue
    }

    $archivePath = Join-Path ([IO.Path]::GetTempPath()) "playtest-history-$($run.id).zip"
    $runDestination = Join-Path $Destination ([string]$run.id)

    try {
        Invoke-WebRequest `
            -Uri $artifact.archive_download_url `
            -Headers $headers `
            -Authentication Bearer `
            -Token $secureToken `
            -OutFile $archivePath
        Expand-Archive -Path $archivePath -DestinationPath $runDestination -Force

        $trxCount = @(
            Get-ChildItem -Path $runDestination -Filter "*.trx" -File -Recurse
        ).Count
        if ($trxCount -eq 0) {
            Write-Output "Run $($run.id) artifact contained no TRX files; skipping."
            Remove-Item $runDestination -Recurse -Force
            continue
        }

        $downloadedRuns++
        $downloadedTrxFiles += $trxCount
        Write-Output "Downloaded $trxCount TRX file(s) from run $($run.id)."
    }
    finally {
        Remove-Item $archivePath -Force -ErrorAction SilentlyContinue
    }
}

Write-Output (
    "Historical test collection complete: " +
    "$downloadedTrxFiles TRX file(s) from $downloadedRuns run(s)."
)
