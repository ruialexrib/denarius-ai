param(
    [Parameter(Mandatory = $true)]
    [string]$Commit,

    [Parameter(Mandatory = $true)]
    [string]$RepositoryRoot
)

$ErrorActionPreference = 'Stop'

function Get-GitText {
    param(
        [Parameter(Mandatory = $true)]
        [string[]]$Arguments
    )

    $result = & git -C $RepositoryRoot @Arguments 2>$null
    if ($LASTEXITCODE -ne 0) {
        return $null
    }

    return ($result -join "`n").Trim()
}

function Get-VersionFromCommit {
    $projectPaths = @(
        'src/DenariusAI.Web/DenariusAI.Web.csproj',
        'Directory.Build.props'
    )

    foreach ($path in $projectPaths) {
        $xmlText = Get-GitText -Arguments @('show', "$Commit`:$path")
        if ([string]::IsNullOrWhiteSpace($xmlText)) {
            continue
        }

        try {
            [xml]$xml = $xmlText
            $versionNode = $xml.Project.PropertyGroup.Version | Select-Object -First 1
            if ($null -ne $versionNode -and -not [string]::IsNullOrWhiteSpace([string]$versionNode)) {
                return [string]$versionNode
            }
        }
        catch {
            continue
        }
    }

    return 'Not available'
}

function Get-GitHubRepository {
    $origin = Get-GitText -Arguments @('remote', 'get-url', 'origin')
    if ([string]::IsNullOrWhiteSpace($origin)) {
        return $null
    }

    if ($origin -match 'github\.com[:/](?<repository>[^/]+/[^/]+?)(?:\.git)?$') {
        return $Matches.repository
    }

    return $null
}

function Get-ClosingIssueNumbers {
    param(
        [AllowNull()]
        [string]$Body
    )

    if ([string]::IsNullOrWhiteSpace($Body)) {
        return @()
    }

    $numbers = [System.Collections.Generic.HashSet[int]]::new()
    foreach ($line in ($Body -split "`r?`n")) {
        if ($line -notmatch '(?i)\b(?:close[sd]?|fix(?:e[sd])?|resolve[sd]?)\b') {
            continue
        }

        foreach ($match in [regex]::Matches($line, '#(?<number>\d+)')) {
            [void]$numbers.Add([int]$match.Groups['number'].Value)
        }
    }

    return @($numbers | Sort-Object)
}

function Invoke-GitHubRequest {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Uri,

        [Parameter(Mandatory = $true)]
        [hashtable]$Headers
    )

    $request = @{
        Uri = $Uri
        Headers = $Headers
        TimeoutSec = 5
        ErrorAction = 'Stop'
    }

    return Invoke-RestMethod @request
}

function Get-GitHubProvenance {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Repository
    )

    $result = [ordered]@{
        PullRequest = 'Not available'
        Issues = 'Not available'
    }

    $headers = @{
        Accept = 'application/vnd.github+json'
        'User-Agent' = 'DenariusAI-start'
        'X-GitHub-Api-Version' = '2022-11-28'
    }

    try {
        $pullRequests = Invoke-GitHubRequest -Uri "https://api.github.com/repos/$Repository/commits/$Commit/pulls" -Headers $headers
        $pullRequest = $pullRequests |
            Where-Object { $null -ne $_.merged_at } |
            Sort-Object merged_at -Descending |
            Select-Object -First 1

        if ($null -eq $pullRequest) {
            return $result
        }

        $result.PullRequest = "#$($pullRequest.number) - $($pullRequest.title)"
        $issueNumbers = Get-ClosingIssueNumbers -Body $pullRequest.body
        if ($issueNumbers.Count -eq 0) {
            return $result
        }

        $issues = @()
        foreach ($issueNumber in $issueNumbers) {
            try {
                $issue = Invoke-GitHubRequest -Uri "https://api.github.com/repos/$Repository/issues/$issueNumber" -Headers $headers
                if ($null -eq $issue.pull_request) {
                    $issues += "#$issueNumber - $($issue.title)"
                }
            }
            catch {
                $issues += "#$issueNumber"
            }
        }

        if ($issues.Count -gt 0) {
            $result.Issues = $issues -join '; '
        }
    }
    catch {
        return $result
    }

    return $result
}

$version = Get-VersionFromCommit
$shortCommit = Get-GitText -Arguments @('rev-parse', '--short=7', $Commit)
if ([string]::IsNullOrWhiteSpace($shortCommit)) {
    $shortCommit = if ($Commit.Length -gt 7) { $Commit.Substring(0, 7) } else { $Commit }
}

$pullRequest = 'Not available'
$issues = 'Not available'
$repository = Get-GitHubRepository
if (-not [string]::IsNullOrWhiteSpace($repository)) {
    $provenance = Get-GitHubProvenance -Repository $repository
    $pullRequest = $provenance.PullRequest
    $issues = $provenance.Issues
}

Write-Host '============================================================'
Write-Host 'Denarius AI deployment ready'
Write-Host "Version: $version"
Write-Host "Commit: $shortCommit"
Write-Host "PR: $pullRequest"
Write-Host "Issue: $issues"
Write-Host '============================================================'
