param(
    [string]$Configuration = "Release",
    [string]$Runtime = "win-x64",
    [bool]$SelfContained = $true
)

$ErrorActionPreference = "Stop"

Write-Host "==================================================" -ForegroundColor Cyan
Write-Host "Publishing PrintMonitor Suite..." -ForegroundColor Cyan
Write-Host "  Configuration:  $Configuration" -ForegroundColor Gray
Write-Host "  Runtime:        $Runtime" -ForegroundColor Gray
Write-Host "  Self-Contained: $SelfContained" -ForegroundColor Gray
Write-Host "==================================================" -ForegroundColor Cyan

$serviceProj = Join-Path $PSScriptRoot "..\src\PrintMonitor\PrintMonitor.csproj"
$managerProj = Join-Path $PSScriptRoot "..\src\PrintMonitor.Manager\PrintMonitor.Manager.csproj"
$targetPublishDir = [System.IO.Path]::GetFullPath((Join-Path $PSScriptRoot "..\src\PrintMonitor\bin\$Configuration\net10.0\$Runtime\publish"))

Write-Host "[1/2] Publishing Windows Service Agent..." -ForegroundColor Yellow
dotnet publish "$serviceProj" `
    -c $Configuration `
    -r $Runtime `
    --self-contained $SelfContained `
    -p:SkipInstallerBuild=true

Write-Host "[2/2] Publishing Control Panel & GUI Manager..." -ForegroundColor Yellow
dotnet publish "$managerProj" `
    -c $Configuration `
    -r $Runtime `
    --self-contained $SelfContained `
    -o "$targetPublishDir"

Write-Host "Publish finished successfully. Both binaries placed in: $targetPublishDir" -ForegroundColor Green
