$envPath = "C:\xampp\htdocs\.env"
$lines = Get-Content $envPath
$updated = @()
foreach ($line in $lines) {
    if ($line -match "^APP_NAME=") {
        $updated += 'APP_NAME="Nexrein Printer Monitor"'
    } elseif ($line -match "^APP_URL=") {
        $updated += 'APP_URL=https://printmonitor.nexreindigital.co.ke'
    } else {
        $updated += $line
    }
}
Set-Content -Path $envPath -Value $updated
Write-Host "Updated C:\xampp\htdocs\.env successfully." -ForegroundColor Green
Get-Content $envPath | Select-String "APP_NAME|APP_URL"
