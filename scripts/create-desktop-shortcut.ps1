$wsh = New-Object -ComObject WScript.Shell

$publicShortcut = "C:\Users\Public\Desktop\Nexrein Printer Monitor.lnk"
$userShortcut = "$env:USERPROFILE\OneDrive\Desktop\Nexrein Printer Monitor.lnk"
$altUserShortcut = "$env:USERPROFILE\Desktop\Nexrein Printer Monitor.lnk"

# Clean all legacy shortcut names to ensure no duplicate launchers exist
$legacyShortcuts = @(
    "C:\Users\Public\Desktop\PrintMonitor Manager.lnk",
    "$env:USERPROFILE\Desktop\PrintMonitor Manager.lnk",
    "$env:USERPROFILE\OneDrive\Desktop\PrintMonitor Manager.lnk",
    "C:\Users\Public\Desktop\Nexrein Print Monitor.lnk",
    "$env:USERPROFILE\Desktop\Nexrein Print Monitor.lnk",
    "$env:USERPROFILE\OneDrive\Desktop\Nexrein Print Monitor.lnk"
)
foreach ($ls in $legacyShortcuts) {
    if (Test-Path $ls) { 
        Remove-Item $ls -Force -ErrorAction SilentlyContinue 
        Write-Host "Removed legacy shortcut: $ls" -ForegroundColor Yellow
    }
}

# Create single canonical public desktop shortcut
try {
    $shortcut = $wsh.CreateShortcut($publicShortcut)
    $shortcut.TargetPath = "C:\Program Files\PrintMonitor\PrintMonitor.Manager.exe"
    $shortcut.WorkingDirectory = "C:\Program Files\PrintMonitor"
    $shortcut.IconLocation = "C:\Program Files\PrintMonitor\app.ico"
    $shortcut.Description = "Nexrein Printer Monitor Control Panel & Manager"
    $shortcut.Save()
    Write-Host "Canonical desktop shortcut verified at: $publicShortcut" -ForegroundColor Green

    # Remove user duplicates if public shortcut exists
    if (Test-Path $userShortcut) { Remove-Item $userShortcut -Force -ErrorAction SilentlyContinue }
    if (Test-Path $altUserShortcut) { Remove-Item $altUserShortcut -Force -ErrorAction SilentlyContinue }
} catch {
    # Fallback to current user desktop
    $targetUser = if (Test-Path "$env:USERPROFILE\OneDrive\Desktop") { $userShortcut } else { $altUserShortcut }
    $shortcut = $wsh.CreateShortcut($targetUser)
    $shortcut.TargetPath = "C:\Program Files\PrintMonitor\PrintMonitor.Manager.exe"
    $shortcut.WorkingDirectory = "C:\Program Files\PrintMonitor"
    $shortcut.IconLocation = "C:\Program Files\PrintMonitor\app.ico"
    $shortcut.Description = "Nexrein Printer Monitor Control Panel & Manager"
    $shortcut.Save()
    Write-Host "User desktop shortcut verified at: $targetUser" -ForegroundColor Green
}
