$installer = ".\release\PrintMonitor-Setup.exe"
Write-Host "Running installer: $installer..." -ForegroundColor Cyan

$proc = Start-Process -FilePath $installer -ArgumentList "/VERYSILENT /SUPPRESSMSGBOXES /NORESTART" -Wait -PassThru
Write-Host "Installer finished with ExitCode: $($proc.ExitCode)" -ForegroundColor Green

Start-Sleep -Seconds 2

# Check Service
Write-Host "`nChecking Service Status..." -ForegroundColor Cyan
& sc.exe query PrintMonitor
& sc.exe qc PrintMonitor

# Also create/update desktop shortcut for current user
Write-Host "`nEnsuring Desktop Shortcuts..." -ForegroundColor Cyan
& ".\scripts\create-desktop-shortcut.ps1"

$desktopShortcuts = @(
    "$env:USERPROFILE\Desktop\Nexrein Print Monitor.lnk",
    "$env:USERPROFILE\OneDrive\Desktop\Nexrein Print Monitor.lnk"
)

foreach ($sc in $desktopShortcuts) {
    if (Test-Path $sc) {
        Write-Host "Shortcut verified at: $sc" -ForegroundColor Green
    }
}
