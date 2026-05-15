param(
    [string]$TaskName = "PriceImportPythonService",
    [string]$ServiceDirectory = $PSScriptRoot
)

$ErrorActionPreference = "Stop"

$serviceDirectoryPath = Resolve-Path -LiteralPath $ServiceDirectory
$runScript = Join-Path $serviceDirectoryPath "run_price_import_api_production.bat"

if (-not (Test-Path -LiteralPath $runScript)) {
    throw "Run script was not found: $runScript"
}

$action = New-ScheduledTaskAction `
    -Execute "cmd.exe" `
    -Argument "/c `"$runScript`"" `
    -WorkingDirectory $serviceDirectoryPath

$trigger = New-ScheduledTaskTrigger -AtStartup

$principal = New-ScheduledTaskPrincipal `
    -UserId "SYSTEM" `
    -LogonType ServiceAccount `
    -RunLevel Highest

$settings = New-ScheduledTaskSettingsSet `
    -AllowStartIfOnBatteries `
    -DontStopIfGoingOnBatteries `
    -ExecutionTimeLimit (New-TimeSpan -Days 3650) `
    -RestartCount 3 `
    -RestartInterval (New-TimeSpan -Minutes 1) `
    -StartWhenAvailable

Register-ScheduledTask `
    -TaskName $TaskName `
    -Action $action `
    -Trigger $trigger `
    -Principal $principal `
    -Settings $settings `
    -Force | Out-Null

Start-ScheduledTask -TaskName $TaskName

Write-Host "Installed and started scheduled task '$TaskName'."
Write-Host "Health check: http://127.0.0.1:8005/health"
