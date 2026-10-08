$body = @{
    device_id = "test_pc_1"
    jobs = @(
        @{
            job_uid = "job_12345"
            job_id = 1
            printer_name = "HP LaserJet Pro M404"
            document_name = "Quarterly_Tax_Report.pdf"
            username = "alice"
            pages = 5
            pages_printed = 5
            copies = 2
            total_pages_calculated = 10
            color_mode = "Monochrome"
            status = "Completed"
            submitted_at = (Get-Date).ToString("o")
        },
        @{
            job_uid = "job_12346"
            job_id = 2
            printer_name = "Epson L805 Photo"
            document_name = "Flyer_Design_FullColor.docx"
            username = "marketing"
            pages = 4
            pages_printed = 4
            copies = 5
            total_pages_calculated = 20
            color_mode = "Color"
            status = "Completed"
            submitted_at = (Get-Date).ToString("o")
        }
    )
} | ConvertTo-Json -Depth 5

$response = Invoke-RestMethod -Uri "http://localhost/api/print-jobs/sync" -Method Post -Body $body -ContentType "application/json"
Write-Host "Sync Result: $($response | ConvertTo-Json)" -ForegroundColor Green

# Test heartbeat
$hb = Invoke-RestMethod -Uri "http://localhost/api/devices/heartbeat" -Method Post -Body (@{device_id="test_pc_1"; status="Online"} | ConvertTo-Json) -ContentType "application/json"
Write-Host "Heartbeat Result: $($hb | ConvertTo-Json)" -ForegroundColor Cyan
