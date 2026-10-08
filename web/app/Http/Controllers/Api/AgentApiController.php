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
use Illuminate\Support\Facades\DB;
use Illuminate\Support\Facades\Hash;
use Illuminate\Support\Str;

class AgentApiController extends Controller
{
    /**
     * Authenticate and retrieve device securely.
     */
    private function resolveDevice(Request $request): ?Device
    {
        $deviceId = $request->header('X-Device-Id') ?? $request->input('device_id');
        if (empty($deviceId)) {
            return null;
        }

        $device = Device::where('device_id', $deviceId)->first();
        if (!$device) {
            return null;
        }

        // Validate API Key / Bearer token if device has established token
        $providedToken = $request->bearerToken()
            ?? $request->header('X-Api-Key')
            ?? $request->input('api_key');

        if (!empty($device->api_token) && !empty($providedToken)) {
            if (!hash_equals($device->api_token, (string) $providedToken)) {
                return null;
            }
        }

        return $device;
    }

    /**
     * POST /api/devices/register
     */
    public function register(Request $request): JsonResponse
    {
        $validated = $request->validate([
            'device_id'     => 'required|string|max:100',
            'computer_name' => 'required|string|max:100',
            'shop_name'     => 'nullable|string|max:150',
            'user_email'    => 'required|email|max:150',
            'ip_address'    => 'nullable|string|max:45',
            'mac_address'   => 'nullable|string|max:50',
            'os_version'    => 'nullable|string|max:100',
            'app_version'   => 'nullable|string|max:50',
        ]);

        $email = strtolower(trim($validated['user_email']));
        $computerName = trim($validated['computer_name']);
        $shopName = !empty($validated['shop_name']) ? trim($validated['shop_name']) : 'Main Shop - ' . $computerName;

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

        // Establish secure device token
        $device = Device::where('device_id', $validated['device_id'])->first();
        $token = $device?->api_token ?: Str::random(48);

        $device = Device::updateOrCreate(
            ['device_id' => $validated['device_id']],
            [
                'api_token'         => $token,
                'computer_name'     => $computerName,
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
            'success'               => true,
            'device_id'             => $device->device_id,
            'api_key'               => $device->api_token,
            'token'                 => $device->api_token,
            'shop_name'             => $device->shop_name,
            'is_new_user'           => $isNewUser,
            'dashboard_url'         => 'https://printmonitor.nexreindigital.co.ke',
            'default_password_note' => $isNewUser
                ? 'Log in at https://printmonitor.nexreindigital.co.ke with your email and default password "admin". You will be prompted to reset it upon first login.'
                : null,
        ]);
    }

    /**
     * POST /api/devices/heartbeat
     */
    public function heartbeat(Request $request): JsonResponse
    {
        $deviceId = $request->header('X-Device-Id') ?? $request->input('device_id');
        if (empty($deviceId)) {
            return response()->json(['error' => 'Device ID is required'], 400);
        }

        $device = $this->resolveDevice($request) ?? Device::where('device_id', $deviceId)->first();

        if (!$device) {
            return response()->json(['status' => 'not_found', 'error' => 'Device not registered'], 404);
        }

        $device->status = $request->input('status', 'Online');
        $device->last_heartbeat_at = now();
        $device->save();

        $response = [
            'status'      => 'ok',
            'server_time' => now()->toIso8601String(),
        ];

        // Check for pending remote software password changes
        if (!empty($device->pending_password_update)) {
            $response['command'] = 'update_software_password';
            $response['new_password'] = $device->pending_password_update;
        }

        return response()->json($response);
    }

    /**
     * POST /api/devices/acknowledge-password
     */
    public function acknowledgePassword(Request $request): JsonResponse
    {
        $deviceId = $request->header('X-Device-Id') ?? $request->input('device_id');
        if (empty($deviceId)) {
            return response()->json(['error' => 'Device ID is required'], 400);
        }

        $device = Device::where('device_id', $deviceId)->first();
        if ($device) {
            $device->software_password = $device->pending_password_update;
            $device->pending_password_update = null;
            $device->save();
        }

        return response()->json(['status' => 'acknowledged']);
    }

    /**
     * POST /api/print-jobs/sync
     */
    public function syncJobs(Request $request): JsonResponse
    {
        $deviceId = $request->header('X-Device-Id') ?? $request->input('device_id');
        $jobs = $request->input('jobs', []);

        if (empty($deviceId)) {
            return response()->json(['error' => 'Device ID is required'], 400);
        }

        $device = Device::where('device_id', $deviceId)->first();
        $shopName = $device ? $device->shop_name : 'Default Shop';
        $computerName = $device ? $device->computer_name : 'Workstation';
        $userEmail = $device ? $device->user_email : null;

        $syncedCount = 0;

        DB::beginTransaction();
        try {
            foreach ($jobs as $jobData) {
                if (empty($jobData['job_uid'])) {
                    continue;
                }

                $submittedAt = !empty($jobData['submitted_at'])
                    ? Carbon::parse($jobData['submitted_at'])
                    : now();

                $pages = max(1, (int) ($jobData['pages'] ?? 1));
                $copies = max(1, (int) ($jobData['copies'] ?? 1));
                $calculatedTotal = (int) ($jobData['total_pages_calculated'] ?? ($pages * $copies));

                PrintJob::updateOrCreate(
                    ['job_uid' => $jobData['job_uid']],
                    [
                        'device_id'              => $deviceId,
                        'shop_name'              => $shopName,
                        'computer_name'          => $computerName,
                        'user_email'             => $userEmail,
                        'job_id'                 => (int) ($jobData['job_id'] ?? 0),
                        'printer_name'           => $jobData['printer_name'] ?? 'Default Printer',
                        'document_name'          => $jobData['document_name'] ?? 'Document',
                        'username'               => $jobData['username'] ?? 'User',
                        'pages'                  => $pages,
                        'pages_printed'          => (int) ($jobData['pages_printed'] ?? $pages),
                        'copies'                 => $copies,
                        'total_pages_calculated' => $calculatedTotal,
                        'color_mode'             => $jobData['color_mode'] ?? 'Monochrome',
                        'duplex'                 => $jobData['duplex'] ?? 'Simplex',
                        'paper_size'             => $jobData['paper_size'] ?? 'A4',
                        'status'                 => $jobData['status'] ?? 'Completed',
                        'submitted_at'           => $submittedAt,
                    ]
                );

                $syncedCount++;
            }

            // Also refresh device heartbeat
            if ($device) {
                $device->last_heartbeat_at = now();
                $device->save();
            }

            DB::commit();
        } catch (\Throwable $e) {
            DB::rollBack();
            return response()->json([
                'success' => false,
                'error'   => 'Database sync error: ' . $e->getMessage(),
            ], 500);
        }

        return response()->json([
            'success' => true,
            'synced'  => $syncedCount,
        ]);
    }

    /**
     * POST /api/printers/sync
     */
    public function syncPrinters(Request $request): JsonResponse
    {
        $deviceId = $request->header('X-Device-Id') ?? $request->input('device_id');
        $printers = $request->input('printers', []);

        if (empty($deviceId)) {
            return response()->json(['error' => 'Device ID is required'], 400);
        }

        foreach ($printers as $p) {
            if (empty($p['name'])) {
                continue;
            }

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

    /**
     * GET /api/version/latest
     */
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
