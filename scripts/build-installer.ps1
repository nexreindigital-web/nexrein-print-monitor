param(
    [string]$Configuration = "Release",
    [string]$OutputDir = "release",
    [switch]$SkipPublish
)

$ErrorActionPreference = "Stop"

Write-Host "==================================================" -ForegroundColor Cyan
Write-Host "Building PrintMonitor Installer..." -ForegroundColor Cyan
Write-Host "==================================================" -ForegroundColor Cyan

# 1. Publish win-x64 self-contained build if not skipped
if (-not $SkipPublish) {
    $publishScript = Join-Path $PSScriptRoot "publish.ps1"
    & $publishScript -Configuration $Configuration -Runtime "win-x64" -SelfContained $true
}

# 2. Locate ISCC.exe
$isccCandidates = @(
    "C:\Users\NEXREIN\AppData\Local\Programs\Inno Setup 6\ISCC.exe",
    "$env:LOCALAPPDATA\Programs\Inno Setup 6\ISCC.exe",
    "C:\Program Files (x86)\Inno Setup 6\ISCC.exe",
    "C:\Program Files\Inno Setup 6\ISCC.exe"
)

$isccPath = $isccCandidates | Where-Object { Test-Path $_ } | Select-Object -First 1

if (-not $isccPath) {
    $cmd = Get-Command "ISCC.exe" -ErrorAction SilentlyContinue
    if ($cmd) {
        $isccPath = $cmd.Source
    }
}

if (-not $isccPath) {
    Write-Error "Inno Setup compiler (ISCC.exe) was not found. Please install Inno Setup 6."
    exit 1
}

Write-Host "Using Inno Setup compiler: $isccPath" -ForegroundColor Green

# 3. Ensure target directories exist
$targetReleaseDir = [System.IO.Path]::GetFullPath((Join-Path $PSScriptRoot "..\$OutputDir"))
$distDir = [System.IO.Path]::GetFullPath((Join-Path $PSScriptRoot "..\dist"))

if (-not (Test-Path $targetReleaseDir)) {
    New-Item -ItemType Directory -Path $targetReleaseDir -Force | Out-Null
}
if (-not (Test-Path $distDir)) {
    New-Item -ItemType Directory -Path $distDir -Force | Out-Null
}

# 4. Compile Installer
$issFile = Join-Path $PSScriptRoot "..\installer\PrintMonitor.iss"

Write-Host "Compiling $issFile into $targetReleaseDir..." -ForegroundColor Cyan
& "$isccPath" /O"$targetReleaseDir" "$issFile"

if ($LASTEXITCODE -ne 0) {
    Write-Error "Inno Setup compilation failed with exit code $LASTEXITCODE."
    exit $LASTEXITCODE
}

$outputExe = Join-Path $targetReleaseDir "PrintMonitor-Setup.exe"
if (Test-Path $outputExe) {
    # Keep dist mirrored as well
    $distExe = Join-Path $distDir "PrintMonitor-Setup.exe"
    Copy-Item -Path $outputExe -Destination $distExe -Force

    $sizeMb = [math]::Round((Get-Item $outputExe).Length / 1MB, 2)
    Write-Host "==================================================" -ForegroundColor Green
    Write-Host "SUCCESS! Installer executable created:" -ForegroundColor Green
    Write-Host "Release Path: $outputExe" -ForegroundColor Yellow
    Write-Host "Dist Path:    $distExe" -ForegroundColor Yellow
    Write-Host "Binary Size:  $sizeMb MB" -ForegroundColor Yellow
    Write-Host "==================================================" -ForegroundColor Green
}

