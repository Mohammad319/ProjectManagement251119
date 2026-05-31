[CmdletBinding()]
param(
    [switch] $SkipTests,
    [switch] $SkipVulnerabilityScan,
    [string] $PublishDirectory
)

$ErrorActionPreference = "Stop"
$repoRoot = Resolve-Path (Join-Path $PSScriptRoot "..\..")
Push-Location $repoRoot

try {
    dotnet restore "ProjectManagement.slnx"
    dotnet build "ProjectManagement.slnx" --no-restore

    if (-not $SkipTests) {
        dotnet test "ProjectManagement.slnx" --no-build
    }

    if (-not $SkipVulnerabilityScan) {
        dotnet list "ProjectManagement.slnx" package --vulnerable --include-transitive
    }

    if (-not [string]::IsNullOrWhiteSpace($PublishDirectory)) {
        & (Join-Path $PSScriptRoot "Test-PublishedWebAssets.ps1") -PublishDirectory $PublishDirectory
    }

    Write-Host "Production readiness checks completed."
}
finally {
    Pop-Location
}
