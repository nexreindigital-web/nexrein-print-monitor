$ErrorActionPreference = "Stop"

Write-Host "==================================================" -ForegroundColor Cyan
Write-Host "Initializing Git Repository for Nexrein Print Monitor..." -ForegroundColor Cyan
Write-Host "==================================================" -ForegroundColor Cyan

# 1. Initialize Git if not already initialized
if (-not (Test-Path ".git")) {
    & git init -b main
    Write-Host "Git repository initialized with default branch 'main'." -ForegroundColor Green
} else {
    Write-Host "Existing .git repository found." -ForegroundColor Yellow
}

# 2. Configure Git user locally if not set
$userName = & git config --get user.name
if (-not $userName) {
    & git config user.name "Nexrein Digital"
    & git config user.email "nexreindigital@users.noreply.github.com"
    Write-Host "Configured local git user: Nexrein Digital" -ForegroundColor Green
}

# 3. Stage files
Write-Host "Staging files (including installer release\PrintMonitor-Setup.exe)..." -ForegroundColor Yellow
& git add .

# 4. Verify staged files includes the installer
$stagedInstaller = & git status --porcelain | Select-String "PrintMonitor-Setup.exe"
Write-Host "Installer staged status:" -ForegroundColor Cyan
Write-Host $stagedInstaller

# 5. Commit
Write-Host "Creating commit..." -ForegroundColor Yellow
$commitMsg = @"
feat: initial release of Nexrein Print Monitor v1.0.0

- Real-time Win32 Spooler print job monitoring and page accounting
- Color vs. Black & White / Grayscale detection
- Document type classifier (DOCX, Publisher, PDF, Images, Excel, Test Pages)
- Super Admin password security for control panel and settings
- Laravel REST API integration with DeviceController, PrintJobController, and VersionController
- Self-contained Windows installer (release/PrintMonitor-Setup.exe) with auto-start and reboot survival
"@

& git commit -m "$commitMsg"

# 6. Tag release v1.0.0
& git tag -a v1.0.0 -m "Nexrein Print Monitor Release v1.0.0"

# 7. Check if remote exists or create on GitHub
$remotes = & git remote
$repoName = "nexrein-print-monitor"

if ($remotes -notcontains "origin") {
    Write-Host "Creating GitHub remote repository '$repoName' via GitHub CLI..." -ForegroundColor Yellow
    # Create private repository and push
    & gh repo create $repoName --private --source=. --remote=origin --push
    Write-Host "Repository created and pushed successfully to GitHub!" -ForegroundColor Green
} else {
    Write-Host "Remote 'origin' already exists. Pushing main branch and tags..." -ForegroundColor Yellow
    & git push -u origin main --tags
}

# 8. Create GitHub release with installer attached
Write-Host "Creating GitHub release v1.0.0 with installer asset..." -ForegroundColor Yellow
$installerPath = ".\release\PrintMonitor-Setup.exe"
if (Test-Path $installerPath) {
    & gh release create v1.0.0 "$installerPath#PrintMonitor-Setup.exe" `
        --title "Nexrein Print Monitor v1.0.0" `
        --notes "Official enterprise release of Nexrein Print Monitor suite for Windows 10/11. Includes self-contained Windows service agent and GUI management control panel." `
        --verify-tag
    Write-Host "GitHub release v1.0.0 created with installer attached!" -ForegroundColor Green
}

Write-Host "==================================================" -ForegroundColor Green
Write-Host "Git setup and push completed!" -ForegroundColor Green
Write-Host "==================================================" -ForegroundColor Green
