Add-Type -AssemblyName System.Drawing

$imgSrc = 'C:\Users\NEXREIN\.gemini\antigravity-ide\brain\0388850f-1428-41eb-b793-13aa88d1c8de\printmonitor_icon_1791446898934.jpg'
$bmp = [System.Drawing.Bitmap]::FromFile($imgSrc)

$assetsDir = Join-Path $PSScriptRoot "..\installer\assets"
if (-not (Test-Path $assetsDir)) { New-Item -ItemType Directory -Path $assetsDir -Force | Out-Null }
$resDir = Join-Path $PSScriptRoot "..\src\PrintMonitor.Manager\Resources"
if (-not (Test-Path $resDir)) { New-Item -ItemType Directory -Path $resDir -Force | Out-Null }

# Resize to high quality 256x256
$pngBmp = New-Object System.Drawing.Bitmap 256, 256
$g = [System.Drawing.Graphics]::FromImage($pngBmp)
$g.InterpolationMode = [System.Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
$g.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::HighQuality
$g.DrawImage($bmp, 0, 0, 256, 256)
$g.Dispose()

$pngPath1 = Join-Path $assetsDir "app.png"
$pngPath2 = Join-Path $resDir "app.png"
$pngBmp.Save($pngPath1, [System.Drawing.Imaging.ImageFormat]::Png)
$pngBmp.Save($pngPath2, [System.Drawing.Imaging.ImageFormat]::Png)

# Convert to standard Windows ICO format
function Save-AsIco([System.Drawing.Bitmap]$sourceBitmap, [string]$outputPath) {
    $sizes = @(16, 32, 48, 64, 128, 256)
    $images = @()

    foreach ($sz in $sizes) {
        $resized = New-Object System.Drawing.Bitmap $sz, $sz
        $gr = [System.Drawing.Graphics]::FromImage($resized)
        $gr.InterpolationMode = [System.Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
        $gr.DrawImage($sourceBitmap, 0, 0, $sz, $sz)
        $gr.Dispose()

        $ms = New-Object System.IO.MemoryStream
        $resized.Save($ms, [System.Drawing.Imaging.ImageFormat]::Png)
        $resized.Dispose()
        $images += @{ Size = $sz; Data = $ms.ToArray() }
        $ms.Dispose()
    }

    $fs = [System.IO.File]::Create($outputPath)
    $bw = New-Object System.IO.BinaryWriter $fs

    # ICONDIR structure
    $bw.Write([uint16]0) # Reserved
    $bw.Write([uint16]1) # Resource type (1 for icon)
    $bw.Write([uint16]$images.Count) # Image count

    $offset = 6 + (16 * $images.Count)
    foreach ($img in $images) {
        $bWidth = if ($img.Size -ge 256) { [byte]0 } else { [byte]$img.Size }
        $bHeight = if ($img.Size -ge 256) { [byte]0 } else { [byte]$img.Size }
        $bw.Write($bWidth)
        $bw.Write($bHeight)
        $bw.Write([byte]0)   # Color count
        $bw.Write([byte]0)   # Reserved
        $bw.Write([uint16]1)  # Color planes
        $bw.Write([uint16]32) # Bits per pixel
        $bw.Write([uint32]$img.Data.Length) # Image size in bytes
        $bw.Write([uint32]$offset) # Offset to image data
        $offset += $img.Data.Length
    }

    # Write PNG image payloads
    foreach ($img in $images) {
        $bw.Write($img.Data)
    }

    $bw.Flush()
    $bw.Close()
    $fs.Close()
}

$icoPath1 = Join-Path $assetsDir "app.ico"
$icoPath2 = Join-Path $resDir "app.ico"
Save-AsIco $pngBmp $icoPath1
Save-AsIco $pngBmp $icoPath2

$pngBmp.Dispose()
$bmp.Dispose()

Write-Host "Icons generated successfully at:" -ForegroundColor Green
Write-Host "  $icoPath1" -ForegroundColor Yellow
Write-Host "  $icoPath2" -ForegroundColor Yellow
