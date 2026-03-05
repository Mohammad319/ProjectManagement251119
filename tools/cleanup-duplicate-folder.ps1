# تنظيف المشروع: حذف مجلد ProjectManagement251119 المتكرر + حذف bin/obj
# شغّل السكربت من جذر المشروع (حيث توجد مجلدات Domain/Persistence/...)

$ErrorActionPreference = 'Stop'

$root = (Get-Location).Path
$dup = Join-Path $root 'ProjectManagement251119'

Write-Host "Root: $root" -ForegroundColor Cyan

if (Test-Path $dup) {
    Write-Host "Found duplicate folder: $dup" -ForegroundColor Yellow
    Write-Host "Deleting..." -ForegroundColor Yellow
    Remove-Item -LiteralPath $dup -Recurse -Force
    Write-Host "Deleted duplicate folder." -ForegroundColor Green
} else {
    Write-Host "No duplicate folder found." -ForegroundColor Green
}

Write-Host "Cleaning bin/obj..." -ForegroundColor Cyan
Get-ChildItem -Path $root -Directory -Recurse -Force |
    Where-Object { $_.Name -in @('bin','obj') } |
    ForEach-Object {
        try {
            Remove-Item -LiteralPath $_.FullName -Recurse -Force -ErrorAction Stop
            Write-Host "Removed: $($_.FullName)" -ForegroundColor DarkGray
        } catch {
            Write-Host "Skipped (locked): $($_.FullName)" -ForegroundColor DarkYellow
        }
    }

Write-Host "Done." -ForegroundColor Green
