$installedExe = "C:\Program Files\PrintMonitor\PrintMonitor.Manager.exe"
Write-Host "Testing installed executable: $installedExe"
$proc = Start-Process -FilePath $installedExe -PassThru
Start-Sleep -Seconds 4

if ($proc -and -not $proc.HasExited) {
    Write-Host "VERIFIED SUCCESS! Installed PrintMonitor.Manager.exe is RUNNING with PID: $($proc.Id)" -ForegroundColor Green
    Stop-Process -Id $proc.Id -Force
} else {
    Write-Host "FAILED! Installed executable exited immediately with code: $($proc.ExitCode)" -ForegroundColor Red
}
