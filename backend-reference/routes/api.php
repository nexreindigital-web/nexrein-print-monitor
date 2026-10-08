<?php

use App\Http\Controllers\DeviceController;
use App\Http\Controllers\PrintJobController;
use App\Http\Controllers\VersionController;
use Illuminate\Support\Facades\Route;

/*
|--------------------------------------------------------------------------
| PrintMonitor REST API Routes
|--------------------------------------------------------------------------
*/

Route::get('/health', function () {
    return response()->json([
        'status'  => 'ok',
        'service' => 'PrintMonitor-Backend',
        'time'    => now()->toIso8601String(),
    ]);
});

// Device Registration & Heartbeat
Route::post('/devices/register', [DeviceController::class, 'register']);
Route::post('/devices/heartbeat', [DeviceController::class, 'heartbeat']);

// Print Jobs Sync & Monitoring
Route::post('/print-jobs/sync', [PrintJobController::class, 'sync']);

// Dashboard Reporting & Analytics
Route::get('/dashboard/summary', [PrintJobController::class, 'dashboardSummary']);

// Version Controller & Installer Distribution
Route::get('/version/latest', [VersionController::class, 'latest']);
Route::post('/version/check', [VersionController::class, 'checkUpdate']);
Route::get('/version/installer', [VersionController::class, 'downloadInstaller']);
