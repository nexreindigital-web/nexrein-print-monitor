$exe = ".\src\PrintMonitor.Manager\bin\Release\net10.0-windows\PrintMonitor.Manager.exe"
Write-Host "Launching $exe..."
$proc = Start-Process -FilePath $exe -PassThru
Start-Sleep -Seconds 3

if ($proc -and -not $proc.HasExited) {
    Write-Host "SUCCESS! PrintMonitor.Manager process is alive and running with PID: $($proc.Id)" -ForegroundColor Green
    # Gracefully stop the test process
    Stop-Process -Id $proc.Id -Force
} else {
    Write-Host "FAILED! Process exited with ExitCode: $($proc.ExitCode)" -ForegroundColor Red
    $err = Get-WinEvent -FilterHashtable @{LogName='Application'; ProviderName='.NET Runtime'} -MaxEvents 1 -ErrorAction SilentlyContinue
    Write-Host $err.Message
}
