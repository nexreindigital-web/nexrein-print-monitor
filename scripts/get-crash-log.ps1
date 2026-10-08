$e = Get-WinEvent -FilterHashtable @{LogName='Application'; ProviderName='.NET Runtime'} -MaxEvents 1
$lines = $e.Message -split "`n"
$lines[0..30] -join "`n"
