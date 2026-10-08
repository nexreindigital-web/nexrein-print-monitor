$wsh = New-Object -ComObject WScript.Shell

$publicShortcut = "C:\Users\Public\Desktop\Nexrein Print Monitor.lnk"
$userShortcut = "$env:USERPROFILE\OneDrive\Desktop\Nexrein Print Monitor.lnk"
$altUserShortcut = "$env:USERPROFILE\Desktop\Nexrein Print Monitor.lnk"

# Clean legacy shortcuts
$legacyShortcuts = @(
    "C:\Users\Public\Desktop\PrintMonitor Manager.lnk",
    "$env:USERPROFILE\Desktop\PrintMonitor Manager.lnk",
    "$env:USERPROFILE\OneDrive\Desktop\PrintMonitor Manager.lnk"
)
foreach ($ls in $legacyShortcuts) {
    if (Test-Path $ls) { Remove-Item $ls -Force -ErrorAction SilentlyContinue }
}

# If Public Desktop shortcut already exists, remove the user-specific duplicate
if (Test-Path $publicShortcut) {
    if (Test-Path $userShortcut) {
        Remove-Item $userShortcut -Force -ErrorAction SilentlyContinue
    }
    Write-Host "Public desktop shortcut verified at: $publicShortcut" -ForegroundColor Green
} else {
    # Otherwise ensure user desktop has the single canonical shortcut
    $shortcut = $wsh.CreateShortcut($userShortcut)
    $shortcut.TargetPath = "C:\Program Files\PrintMonitor\PrintMonitor.Manager.exe"
    $shortcut.WorkingDirectory = "C:\Program Files\PrintMonitor"
    $shortcut.IconLocation = "C:\Program Files\PrintMonitor\app.ico"
    $shortcut.Description = "Nexrein Print Monitor Control Panel & Manager"
    $shortcut.Save()
    Write-Host "User desktop shortcut verified at: $userShortcut" -ForegroundColor Green
}
