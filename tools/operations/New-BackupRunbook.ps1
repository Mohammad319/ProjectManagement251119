[CmdletBinding()]
param(
    [Parameter(Mandatory)] [string[]] $DatabaseName,
    [Parameter(Mandatory)] [string] $BackupDirectory,
    [string] $OutputFile = "artifacts/backup/backup-plan.sql"
)

$ErrorActionPreference = "Stop"
$repoRoot = Resolve-Path (Join-Path $PSScriptRoot "..\..")
$outputPath = Join-Path $repoRoot $OutputFile
$outputDir = Split-Path -Parent $outputPath
New-Item -ItemType Directory -Force -Path $outputDir | Out-Null

$timestamp = Get-Date -Format "yyyyMMdd-HHmmss"
$escapedBackupDirectory = $BackupDirectory.TrimEnd("\").Replace("'", "''")

$lines = @(
    "-- Generated backup plan. Review paths and retention before running in production.",
    "-- Generated at: $(Get-Date -Format o)",
    "DECLARE @BackupDirectory nvarchar(4000) = N'$escapedBackupDirectory';",
    ""
)

foreach ($db in $DatabaseName) {
    $safeDb = $db.Replace("]", "]]")
    $safeFile = ($db -replace '[^\w.-]', '_')
    $lines += "BACKUP DATABASE [$safeDb]"
    $lines += "TO DISK = @BackupDirectory + N'\$safeFile-$timestamp.bak'"
    $lines += "WITH COPY_ONLY, COMPRESSION, CHECKSUM, STATS = 10;"
    $lines += "RESTORE VERIFYONLY"
    $lines += "FROM DISK = @BackupDirectory + N'\$safeFile-$timestamp.bak'"
    $lines += "WITH CHECKSUM;"
    $lines += ""
}

$lines | Set-Content -Path $outputPath -Encoding UTF8
Write-Host "Backup SQL plan written to $outputPath"
Write-Host "Remember to back up DataProtection keys and uploaded/imported files separately."
