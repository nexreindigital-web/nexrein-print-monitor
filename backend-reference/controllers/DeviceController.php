<?php

namespace App\Http\Controllers;

use App\Models\Device;
use Illuminate\Http\JsonResponse;
use Illuminate\Http\Request;
use Illuminate\Support\Str;

class DeviceController extends Controller
{
    /**
     * POST /api/devices/register
     */
    public function register(Request $request): JsonResponse
    {
        $validated = $request->validate([
            'device_id'           => 'required|string|max:64',
            'computer_name'       => 'required|string|max:255',
            'windows_user'        => 'required|string|max:255',
            'domain'              => 'nullable|string|max:255',
            'os_version'          => 'required|string|max:255',
            'architecture'        => 'nullable|string|max:50',
            'application_version' => 'nullable|string|max:50',
            'ip_address'          => 'nullable|string|max:64',
            'mac_address'         => 'nullable|string|max:64',
        ]);

        $apiKey = 'pm_' . Str::random(40);

        $device = Device::updateOrCreate(
            ['device_id' => $validated['device_id']],
            [
                'api_key'             => $apiKey,
                'computer_name'       => $validated['computer_name'],
                'windows_user'        => $validated['windows_user'],
                'domain'              => $validated['domain'] ?? null,
                'os_version'          => $validated['os_version'],
                'architecture'        => $validated['architecture'] ?? null,
                'application_version' => $validated['application_version'] ?? '1.0.0',
                'ip_address'          => $validated['ip_address'] ?? null,
                'mac_address'         => $validated['mac_address'] ?? null,
                'status'              => 'online',
                'last_seen_at'        => now(),
            ]
        );

        return response()->json([
            'success'   => true,
            'message'   => 'Device registered successfully',
            'device_id' => $device->device_id,
            'api_key'   => $device->api_key,
        ], 200);
    }

    /**
     * POST /api/devices/heartbeat
     */
    public function heartbeat(Request $request): JsonResponse
    {
        $validated = $request->validate([
            'device_id'          => 'required|string|max:64',
            'computer_name'      => 'nullable|string|max:255',
            'application_version'=> 'nullable|string|max:50',
            'status'             => 'nullable|string|max:50',
            'last_seen'          => 'nullable|string',
            'printer_count'      => 'nullable|integer',
            'pending_sync_count' => 'nullable|integer',
        ]);

        $device = Device::where('device_id', $validated['device_id'])->first();

        if ($device) {
            $device->update([
                'status'       => 'online',
                'last_seen_at' => now(),
            ]);
        }

        return response()->json([
            'success' => true,
            'message' => 'Heartbeat acknowledged',
        ], 200);
    }
}
