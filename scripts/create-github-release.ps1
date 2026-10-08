$notes = @"
## Nexrein Printer Monitor v2.0.0 Release Notes

### 🌐 Cloud Remote Monitoring & Management
- **Domain Integration:** Full portal integration with domain: `https://printmonitor.nexreindigital.co.ke/`
- **Laravel Remote Web Portal:** Live dashboard in `C:\xampp\htdocs\` with overview KPIs, printer inventory, job details, and real-time print metrics.
- **Shop / Workstation Identification:** Every workstation reports under its Computer / Shop Name (e.g. `Shop Counter 1`).
- **User Account Association:** Desktop installer associates installations with registered user email addresses.
- **Authentication & Security:** Secure login, password reset request / token reset flow, and mandatory password reset on first login for default `admin` accounts.
- **Centralized Password Push:** Change the desktop software super admin password remotely from the web dashboard; desktop agents automatically synchronize on heartbeat.

### 💻 Windows Desktop Agent & Control Panel (WPF)
- **Product Name:** Standardized across all binaries as **Nexrein Printer Monitor**.
- **Window Caption Controls:** Dedicated Minimize (`🗕`), Maximize/Restore (`🗖`/`🗗`), and Super Admin protected Close (`✕`) buttons with draggable title bar and dual-click maximize.
- **Live Remote Updates:** "Software Updates & Version Control" center checks server and GitHub for updates, displaying notifications and direct installer download links.
- **Data Preservation Guarantee:** Upgrading never overwrites existing print jobs, databases (`printmonitor.db`), or custom configurations.
- **Direct Web Access:** Quick launcher button in Control Panel to open `https://printmonitor.nexreindigital.co.ke` directly in default browser.

### 📦 Setup Wizard
- Prompt for user email and computer/shop name during installation.
- Post-install instructions with dashboard URL and default login instructions.
- Auto-registers Windows Service (`PrintMonitor`) with automatic failure recovery.
"@

$releaseExe = ".\release\PrintMonitor-Setup.exe#PrintMonitor-Setup.exe"
Write-Host "Creating GitHub Release v2.0.0..." -ForegroundColor Cyan
gh release create v2.0.0 $releaseExe --title "Nexrein Printer Monitor v2.0.0" --notes $notes

if ($LASTEXITCODE -eq 0) {
    Write-Host "GitHub release v2.0.0 created successfully!" -ForegroundColor Green
} else {
    Write-Error "Failed to create GitHub release."
}
