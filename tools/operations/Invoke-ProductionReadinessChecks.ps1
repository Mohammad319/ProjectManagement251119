[CmdletBinding()]
param(
    [switch] $SkipTests,
    [switch] $SkipVulnerabilityScan
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

    Write-Host "Production readiness checks completed."
}
finally {
    Pop-Location
}
