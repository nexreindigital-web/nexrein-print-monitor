$files = Get-ChildItem -Path "C:\Users\NEXREIN\OneDrive\Desktop", "C:\Users\Public\Desktop" -Filter "*.lnk"
foreach ($f in $files) {
    if ($f.Name -match "Print|Nexrein") {
        Write-Host "Found shortcut: $($f.FullName)"
    }
}
