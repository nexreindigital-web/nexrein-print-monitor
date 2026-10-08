param(
    [string]$Tag = "v2.0.0",
    [string]$Title = "Nexrein Printer Monitor v2.0.0",
    [string]$NotesFile = "release-notes-v2.0.0.md",
    [string]$ExePath = "release\PrintMonitor-Setup.exe"
)

$ErrorActionPreference = "Stop"

$repoRoot = (Resolve-Path (Join-Path $PSScriptRoot "..")).Path
$fullExePath = Join-Path $repoRoot $ExePath
if (-not (Test-Path $fullExePath)) {
    Write-Error "Installer executable not found at: $fullExePath"
    exit 1
}

$fullNotesPath = Join-Path $repoRoot $NotesFile
if (-not (Test-Path $fullNotesPath)) {
    Write-Error "Release notes file not found at: $fullNotesPath"
    exit 1
}

Write-Host "Updating GitHub Release $Tag..." -ForegroundColor Cyan

# 1. Update release metadata (title and notes)
gh release edit $Tag --title $Title --notes-file $fullNotesPath

# 2. Upload latest installer asset (clobber existing)
Write-Host "Uploading latest installer asset: $fullExePath..." -ForegroundColor Cyan
gh release upload $Tag "$fullExePath#PrintMonitor-Setup.exe" --clobber

Write-Host "GitHub release $Tag updated successfully!" -ForegroundColor Green
