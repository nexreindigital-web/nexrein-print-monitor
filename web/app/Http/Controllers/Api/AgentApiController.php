<?php

namespace App\Http\Controllers\Api;

use App\Http\Controllers\Controller;
use App\Models\Device;
use App\Models\Printer;
use App\Models\PrintJob;
use App\Models\User;
use Carbon\Carbon;
use Illuminate\Http\JsonResponse;
use Illuminate\Http\Request;
use Illuminate\Support\Facades\Hash;

class AgentApiController extends Controller
{
    public function register(Request $request): JsonResponse
    {
        $validated = $request->validate([
            'device_id'     => 'required|string',
            'computer_name' => 'required|string',
            'shop_name'     => 'nullable|string',
            'user_email'    => 'required|email',
            'ip_address'    => 'nullable|string',
            'mac_address'   => 'nullable|string',
            'os_version'    => 'nullable|string',
            'app_version'   => 'nullable|string',
        ]);

        $email = strtolower(trim($validated['user_email']));
        $shopName = $validated['shop_name'] ?: 'Main Shop - ' . $validated['computer_name'];

        // Automatically create or link User account with default password 'admin'
        $user = User::where('email', $email)->first();
        $isNewUser = false;

        if (!$user) {
            $user = User::create([
                'name'                 => $shopName,
                'email'                => $email,
                'password'             => Hash::make('admin'), // Default password 'admin'
                'shop_name'            => $shopName,
                'must_change_password' => true,
            ]);
            $isNewUser = true;
        }

        // Register or update device record
        $device = Device::updateOrCreate(
            ['device_id' => $validated['device_id']],
            [
                'computer_name'     => $validated['computer_name'],
                'shop_name'         => $shopName,
                'user_email'        => $email,
                'ip_address'        => $validated['ip_address'] ?? $request->ip(),
                'mac_address'       => $validated['mac_address'] ?? null,
                'os_version'        => $validated['os_version'] ?? 'Windows',
                'app_version'       => $validated['app_version'] ?? '2.0.0',
                'status'            => 'Online',
                'last_heartbeat_at' => now(),
            ]
        );

        return response()->json([
            'success'       => true,
            'device_id'     => $device->device_id,
            'shop_name'     => $device->shop_name,
            'is_new_user'   => $isNewUser,
            'dashboard_url' => 'https://printmonitor.nexreindigital.co.ke',
            'default_password_note' => $isNewUser ? 'Log in at https://printmonitor.nexreindigital.co.ke with your email and default password "admin". You will be prompted to reset it upon first login.' : null,
        ]);
    }

    public function heartbeat(Request $request): JsonResponse
    {
        $validated = $request->validate([
            'device_id' => 'required|string',
            'status'    => 'nullable|string',
        ]);

        $device = Device::where('device_id', $validated['device_id'])->first();

        if (!$device) {
            return response()->json(['status' => 'not_found'], 404);
        }

        $device->status = $validated['status'] ?? 'Online';
        $device->last_heartbeat_at = now();
        $device->save();

        $response = [
            'status' => 'ok',
            'server_time' => now()->toIso8601String(),
        ];

        // Check if there is a pending remote password update for this desktop software
        if (!empty($device->pending_password_update)) {
            $response['command'] = 'update_software_password';
            $response['new_password'] = $device->pending_password_update;
        }

        return response()->json($response);
    }

    public function acknowledgePassword(Request $request): JsonResponse
    {
        $validated = $request->validate([
            'device_id' => 'required|string',
        ]);

        $device = Device::where('device_id', $validated['device_id'])->first();
        if ($device) {
            $device->software_password = $device->pending_password_update;
            $device->pending_password_update = null;
            $device->save();
        }

        return response()->json(['status' => 'acknowledged']);
    }

    public function syncJobs(Request $request): JsonResponse
    {
        $deviceId = $request->input('device_id');
        $jobs = $request->input('jobs', []);

        $device = Device::where('device_id', $deviceId)->first();
        $shopName = $device ? $device->shop_name : 'Unknown Shop';
        $computerName = $device ? $device->computer_name : 'Unknown PC';
        $userEmail = $device ? $device->user_email : null;

        $syncedCount = 0;

        foreach ($jobs as $jobData) {
            if (empty($jobData['job_uid'])) {
                continue;
            }

            $submittedAt = !empty($jobData['submitted_at'])
                ? Carbon::parse($jobData['submitted_at'])
                : now();

            $pages = (int) ($jobData['pages'] ?? 1);
            $copies = (int) ($jobData['copies'] ?? 1);
            $calculatedTotal = (int) ($jobData['total_pages_calculated'] ?? ($pages * $copies));

            PrintJob::updateOrCreate(
                ['job_uid' => $jobData['job_uid']],
                [
                    'device_id'               => $deviceId,
                    'shop_name'               => $shopName,
                    'computer_name'           => $computerName,
                    'user_email'              => $userEmail,
                    'job_id'                  => (int) ($jobData['job_id'] ?? 0),
                    'printer_name'            => $jobData['printer_name'] ?? 'Default Printer',
                    'document_name'           => $jobData['document_name'] ?? 'Untitled Document',
                    'username'                => $jobData['username'] ?? 'User',
                    'pages'                   => $pages,
                    'pages_printed'           => (int) ($jobData['pages_printed'] ?? $pages),
                    'copies'                  => $copies,
                    'total_pages_calculated'  => $calculatedTotal,
                    'color_mode'              => $jobData['color_mode'] ?? 'Unknown',
                    'duplex'                  => $jobData['duplex'] ?? 'Unknown',
                    'paper_size'              => $jobData['paper_size'] ?? 'Unknown',
                    'status'                  => $jobData['status'] ?? 'Completed',
                    'submitted_at'            => $submittedAt,
                ]
            );

            $syncedCount++;
        }

        return response()->json([
            'success' => true,
            'synced'  => $syncedCount,
        ]);
    }

    public function syncPrinters(Request $request): JsonResponse
    {
        $deviceId = $request->input('device_id');
        $printers = $request->input('printers', []);

        foreach ($printers as $p) {
            if (empty($p['name'])) continue;

            Printer::updateOrCreate(
                ['device_id' => $deviceId, 'name' => $p['name']],
                [
                    'server_name'  => $p['server_name'] ?? null,
                    'port_name'    => $p['port_name'] ?? null,
                    'driver_name'  => $p['driver_name'] ?? null,
                    'status'       => $p['status'] ?? 'Ready',
                    'job_count'    => (int) ($p['job_count'] ?? 0),
                    'is_default'   => !empty($p['is_default']),
                    'is_network'   => !empty($p['is_network']),
                    'last_seen_at' => now(),
                ]
            );
        }

        return response()->json(['success' => true, 'count' => count($printers)]);
    }

    public function versionLatest(): JsonResponse
    {
        return response()->json([
            'app_name'                  => 'Nexrein Printer Monitor',
            'latest_version'            => '2.0.0',
            'minimum_supported_version' => '1.0.0',
            'release_date'              => date('Y-m-d'),
            'release_notes'             => 'Nexrein Printer Monitor v2.0.0 with Remote Laravel Cloud Portal, Shop & Computer Accounting, and Centralized Password Management.',
            'installer' => [
                'filename'     => 'PrintMonitor-Setup.exe',
                'download_url' => 'https://github.com/nexreindigital-web/nexrein-print-monitor/releases/latest/download/PrintMonitor-Setup.exe',
            ],
        ]);
    }
}
