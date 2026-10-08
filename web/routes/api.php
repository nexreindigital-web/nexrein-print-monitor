<?php

use App\Http\Controllers\Api\AgentApiController;
use Illuminate\Support\Facades\Route;

/*
|--------------------------------------------------------------------------
| API Routes - Nexrein Printer Monitor Agent Sync
|--------------------------------------------------------------------------
*/

Route::get('/health', function () {
    return response()->json([
        'status'  => 'ok',
        'service' => 'Nexrein Printer Monitor Web API',
        'domain'  => 'printmonitor.nexreindigital.co.ke',
        'time'    => now()->toIso8601String(),
    ]);
});

// Device Registration & Heartbeat
Route::post('/devices/register', [AgentApiController::class, 'register']);
Route::post('/devices/heartbeat', [AgentApiController::class, 'heartbeat']);
Route::post('/devices/acknowledge-password', [AgentApiController::class, 'acknowledgePassword']);

// Aliases for printer-monitor namespace
Route::post('/printer-monitor/register', [AgentApiController::class, 'register']);
Route::post('/printer-monitor/heartbeat', [AgentApiController::class, 'heartbeat']);

// Print Jobs & Printers Sync
Route::post('/print-jobs/sync', [AgentApiController::class, 'syncJobs']);
Route::post('/printers/sync', [AgentApiController::class, 'syncPrinters']);

// Version Controller & Installer
Route::get('/version/latest', [AgentApiController::class, 'versionLatest']);
