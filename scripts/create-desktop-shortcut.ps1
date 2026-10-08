$wsh = New-Object -ComObject WScript.Shell
$desktopPaths = @(
    "$env:USERPROFILE\Desktop",
    "$env:USERPROFILE\OneDrive\Desktop"
)
foreach ($dp in $desktopPaths) {
    if (Test-Path $dp) {
        $shortcutPath = Join-Path $dp "Nexrein Print Monitor.lnk"
        $shortcut = $wsh.CreateShortcut($shortcutPath)
        $shortcut.TargetPath = "C:\Program Files\PrintMonitor\PrintMonitor.Manager.exe"
        $shortcut.Description = "Nexrein Print Monitor Control Panel & Manager"
        $shortcut.WorkingDirectory = "C:\Program Files\PrintMonitor"
        $shortcut.IconLocation = "C:\Program Files\PrintMonitor\app.ico"
        $shortcut.Save()

        $legacy = Join-Path $dp "PrintMonitor Manager.lnk"
        if (Test-Path $legacy) { Remove-Item $legacy -Force -ErrorAction SilentlyContinue }
    }
}
