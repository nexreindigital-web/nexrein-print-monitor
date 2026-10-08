<?php

namespace App\Http\Controllers;

use Illuminate\Http\JsonResponse;
use Illuminate\Http\Request;
use Symfony\Component\HttpFoundation\BinaryFileResponse;

class VersionController extends Controller
{
    /**
     * Current canonical release version of Nexrein Print Monitor.
     */
    private const CURRENT_VERSION = '1.0.0';
    private const MIN_SUPPORTED_VERSION = '1.0.0';
    private const INSTALLER_FILENAME = 'PrintMonitor-Setup.exe';

    /**
     * GET /api/version/latest
     * Retrieve current release metadata, minimum supported version, and installer info.
     */
    public function latest(): JsonResponse
    {
        $installerPath = base_path('release/' . self::INSTALLER_FILENAME);
        $fileSize = file_exists($installerPath) ? filesize($installerPath) : 47709969;
        $sha256 = file_exists($installerPath) ? hash_file('sha256', $installerPath) : '4DAB6D8301C4CDBF0C347DEA2575A3C65A40B494E575BFB74EE6E501B836D45A';

        return response()->json([
            'success' => true,
            'app_name' => 'Nexrein Print Monitor',
            'latest_version' => self::CURRENT_VERSION,
            'min_supported_version' => self::MIN_SUPPORTED_VERSION,
            'release_date' => '2026-10-08',
            'release_notes' => [
                'Live Win32 Print Spooler real-time event listener',
                'Automatic document classification (DOCX, PUB, PDF, Images, Excel, Test Pages)',
                'Daily printed page counters with Color vs. Black & White breakdown',
                'Super Admin password protected settings, autostart, and service control',
                'Self-contained Windows installer (win-x64)',
                'Automatic Windows Service recovery and reboot survival'
            ],
            'installer' => [
                'filename' => self::INSTALLER_FILENAME,
                'download_url' => url('/api/version/download'),
                'size_bytes' => $fileSize,
                'sha256' => $sha256,
            ]
        ], 200);
    }

    /**
     * POST /api/version/check
     * Evaluate if a client requires an update based on its current version.
     */
    public function checkUpdate(Request $request): JsonResponse
    {
        $validated = $request->validate([
            'client_version' => 'required|string|max:50',
            'device_id'      => 'nullable|string|max:64',
            'os_version'     => 'nullable|string|max:255',
            'architecture'   => 'nullable|string|max:50',
        ]);

        $clientVersion = $validated['client_version'];
        $hasUpdate = version_compare($clientVersion, self::CURRENT_VERSION, '<');
        $isMandatory = version_compare($clientVersion, self::MIN_SUPPORTED_VERSION, '<');

        return response()->json([
            'success'          => true,
            'client_version'   => $clientVersion,
            'latest_version'   => self::CURRENT_VERSION,
            'update_available' => $hasUpdate,
            'mandatory'        => $isMandatory,
            'download_url'     => $hasUpdate ? url('/api/version/download') : null,
            'message'          => $hasUpdate
                ? ($isMandatory ? 'A mandatory update to v' . self::CURRENT_VERSION . ' is required.' : 'A new update (v' . self::CURRENT_VERSION . ') is available.')
                : 'Nexrein Print Monitor is up to date.'
        ], 200);
    }

    /**
     * GET /api/version/download
     * Stream or download the Nexrein Print Monitor self-contained installer executable.
     */
    public function downloadInstaller(): BinaryFileResponse|JsonResponse
    {
        $possiblePaths = [
            base_path('release/' . self::INSTALLER_FILENAME),
            base_path('dist/' . self::INSTALLER_FILENAME),
            public_path('installers/' . self::INSTALLER_FILENAME),
        ];

        foreach ($possiblePaths as $path) {
            if (file_exists($path)) {
                return response()->download($path, self::INSTALLER_FILENAME, [
                    'Content-Type' => 'application/octet-stream',
                    'Content-Disposition' => 'attachment; filename="' . self::INSTALLER_FILENAME . '"',
                ]);
            }
        }

        return response()->json([
            'success' => false,
            'message' => 'Installer binary not found on backend server.',
        ], 404);
    }
}
