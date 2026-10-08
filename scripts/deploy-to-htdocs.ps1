$sourceDir = Join-Path $PSScriptRoot "..\web"
$targetDir = "C:\xampp\htdocs"

Write-Host "Deploying web portal from $sourceDir to $targetDir..." -ForegroundColor Cyan

# Ensure target directories exist
$dirsToCreate = @(
    "app\Models",
    "app\Http\Controllers\Web",
    "app\Http\Controllers\Api",
    "resources\views\layouts",
    "resources\views\auth",
    "resources\views\dashboard",
    "routes",
    "database\seeders",
    "database\migrations"
)

foreach ($d in $dirsToCreate) {
    $full = Join-Path $targetDir $d
    if (-not (Test-Path $full)) {
        New-Item -ItemType Directory -Path $full -Force | Out-Null
    }
}

# Copy files
Copy-Item (Join-Path $sourceDir "bootstrap\app.php") (Join-Path $targetDir "bootstrap\app.php") -Force
Copy-Item (Join-Path $sourceDir "app\Models\*") (Join-Path $targetDir "app\Models") -Force -Recurse
Copy-Item (Join-Path $sourceDir "app\Http\Controllers\Web\*") (Join-Path $targetDir "app\Http\Controllers\Web") -Force -Recurse
Copy-Item (Join-Path $sourceDir "app\Http\Controllers\Api\*") (Join-Path $targetDir "app\Http\Controllers\Api") -Force -Recurse
Copy-Item (Join-Path $sourceDir "resources\views\layouts\*") (Join-Path $targetDir "resources\views\layouts") -Force -Recurse
Copy-Item (Join-Path $sourceDir "resources\views\auth\*") (Join-Path $targetDir "resources\views\auth") -Force -Recurse
Copy-Item (Join-Path $sourceDir "resources\views\dashboard\*") (Join-Path $targetDir "resources\views\dashboard") -Force -Recurse
Copy-Item (Join-Path $sourceDir "routes\*") (Join-Path $targetDir "routes") -Force -Recurse
Copy-Item (Join-Path $sourceDir "database\seeders\*") (Join-Path $targetDir "database\seeders") -Force -Recurse
Copy-Item (Join-Path $sourceDir "database\migrations\*") (Join-Path $targetDir "database\migrations") -Force -Recurse

Write-Host "Files deployed successfully. Running migrations and seeders..." -ForegroundColor Green
& php (Join-Path $targetDir "artisan") db:seed --force
Write-Host "Database seeded with default super admin." -ForegroundColor Green
