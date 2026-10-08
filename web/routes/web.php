<?php

use App\Http\Controllers\Web\AuthController;
use App\Http\Controllers\Web\DashboardController;
use Illuminate\Support\Facades\Route;

/*
|--------------------------------------------------------------------------
| Web Routes - Nexrein Printer Monitor Web Portal
| Domain: printmonitor.nexreindigital.co.ke
|--------------------------------------------------------------------------
*/

// Redirect root to dashboard or login
Route::get('/', function () {
    return redirect()->route('dashboard');
});

// Authentication & Password Recovery
Route::middleware('guest')->group(function () {
    Route::get('/login', [AuthController::class, 'showLogin'])->name('login');
    Route::post('/login', [AuthController::class, 'login'])->name('login.post');

    Route::get('/forgot-password', [AuthController::class, 'showForgotPassword'])->name('password.forgot');
    Route::post('/forgot-password', [AuthController::class, 'sendResetLink'])->name('password.forgot.post');

    Route::get('/reset-password/{token}', [AuthController::class, 'showResetPassword'])->name('password.reset');
    Route::post('/reset-password', [AuthController::class, 'resetPassword'])->name('password.reset.post');
});

// Authenticated Dashboard Routes
Route::middleware('auth')->group(function () {
    Route::post('/logout', [AuthController::class, 'logout'])->name('logout');

    Route::get('/force-change-password', [AuthController::class, 'showForceChangePassword'])->name('password.force_change');
    Route::get('/change-password', [AuthController::class, 'showChangePassword'])->name('password.change');
    Route::post('/change-password', [AuthController::class, 'updatePassword'])->name('password.update');

    Route::get('/dashboard', [DashboardController::class, 'index'])->name('dashboard');
    Route::get('/devices', [DashboardController::class, 'devices'])->name('devices');
    Route::post('/devices/update-software-password', [DashboardController::class, 'updateSoftwarePassword'])->name('devices.update_software_password');
    Route::get('/printers', [DashboardController::class, 'printers'])->name('printers');
    Route::post('/printers/{id}/update', [DashboardController::class, 'updatePrinter'])->name('printers.update');
    Route::get('/export/csv', [DashboardController::class, 'exportCsv'])->name('export.csv');
});
