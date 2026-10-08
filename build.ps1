param(
    [string]$Configuration = "Release",
    [switch]$SkipTests,
    [switch]$SkipInstaller
)

$ErrorActionPreference = "Stop"

Write-Host "================================================================" -ForegroundColor Cyan
Write-Host "   NEXREIN PRINT MONITOR AUTOMATED BUILD & PACKAGING PIPELINE   " -ForegroundColor Cyan
Write-Host "================================================================" -ForegroundColor Cyan
Write-Host "Configuration: $Configuration" -ForegroundColor Gray
Write-Host "Timestamp:     $(Get-Date -Format 'yyyy-MM-dd HH:mm:ss')" -ForegroundColor Gray
Write-Host ""

$rootDir = $PSScriptRoot
$srcProj = Join-Path $rootDir "src\PrintMonitor\PrintMonitor.csproj"
$testProj = Join-Path $rootDir "tests\PrintMonitor.Tests\PrintMonitor.Tests.csproj"
$scriptsDir = Join-Path $rootDir "scripts"
$releaseDir = Join-Path $rootDir "release"

# 1. Restore
Write-Host "[1/4] Restoring NuGet packages..." -ForegroundColor Yellow
dotnet restore "$rootDir\PrintMonitor.sln"

# 2. Build Solution
Write-Host "[2/4] Building solution ($Configuration)..." -ForegroundColor Yellow
dotnet build "$rootDir\PrintMonitor.sln" -c $Configuration --no-restore

# 3. Run Automated Tests
if (-not $SkipTests) {
    Write-Host "[3/4] Running automated test suite..." -ForegroundColor Yellow
    dotnet test "$testProj" -c $Configuration --no-build
} else {
    Write-Host "[3/4] Tests skipped as requested." -ForegroundColor DarkGray
}

# 4. Publish self-contained win-x64 and compile installer
if (-not $SkipInstaller) {
    Write-Host "[4/4] Publishing self-contained win-x64 and compiling installer..." -ForegroundColor Yellow
    $publishScript = Join-Path $scriptsDir "publish.ps1"
    & $publishScript -Configuration $Configuration -Runtime "win-x64" -SelfContained $true

    $buildInstallerScript = Join-Path $scriptsDir "build-installer.ps1"
    & $buildInstallerScript -Configuration $Configuration -SkipPublish

    $targetExe = Join-Path $releaseDir "PrintMonitor-Setup.exe"
    if (Test-Path $targetExe) {
        $fileInfo = Get-Item $targetExe
        $sizeMb = [math]::Round($fileInfo.Length / 1MB, 2)
        $sha256 = (Get-FileHash -Path $targetExe -Algorithm SHA256).Hash

        Write-Host ""
        Write-Host "================================================================" -ForegroundColor Green
        Write-Host "  BUILD AND PACKAGING COMPLETED SUCCESSFULLY!                   " -ForegroundColor Green
        Write-Host "================================================================" -ForegroundColor Green
        Write-Host "Executable: release\PrintMonitor-Setup.exe" -ForegroundColor White
        Write-Host "Full Path:  $targetExe" -ForegroundColor Yellow
        Write-Host "Size:       $sizeMb MB ($($fileInfo.Length) bytes)" -ForegroundColor Cyan
        Write-Host "SHA256:     $sha256" -ForegroundColor Gray
        Write-Host "================================================================" -ForegroundColor Green
    } else {
        Write-Error "Build finished but $targetExe was not found!"
        exit 1
    }
}
