<?php

namespace App\Http\Controllers;

use App\Models\Device;
use App\Models\Printer;
use App\Models\PrintJob;
use Illuminate\Http\JsonResponse;
use Illuminate\Http\Request;
use Illuminate\Support\Facades\DB;

class PrintJobController extends Controller
{
    /**
     * POST /api/print-jobs/sync
     * Batch synchronization of print jobs from PrintMonitor agent.
     */
    public function sync(Request $request): JsonResponse
    {
        $validated = $request->validate([
            'device_id' => 'required|string|max:64',
            'jobs'      => 'required|array',
            'jobs.*.client_id'     => 'required|integer',
            'jobs.*.job_uid'       => 'required|string|max:255',
            'jobs.*.job_id'        => 'required|integer',
            'jobs.*.printer_name'  => 'required|string|max:255',
            'jobs.*.document_name' => 'required|string|max:255',
            'jobs.*.username'      => 'required|string|max:255',
            'jobs.*.computer_name' => 'required|string|max:255',
            'jobs.*.pages'         => 'required|integer|min:0',
            'jobs.*.pages_printed' => 'nullable|integer|min:0',
            'jobs.*.copies'        => 'nullable|integer|min:1',
            'jobs.*.color_mode'    => 'nullable|string',
            'jobs.*.duplex'        => 'nullable|string',
            'jobs.*.paper_size'    => 'nullable|string',
            'jobs.*.status'        => 'nullable|string',
            'jobs.*.error_message' => 'nullable|string',
            'jobs.*.submitted_at'  => 'required|string',
            'jobs.*.started_at'    => 'nullable|string',
            'jobs.*.completed_at'  => 'nullable|string',
        ]);

        $device = Device::firstOrCreate(
            ['device_id' => $validated['device_id']],
            [
                'computer_name' => $validated['jobs'][0]['computer_name'] ?? 'Unknown',
                'windows_user'  => $validated['jobs'][0]['username'] ?? 'Unknown',
                'os_version'    => 'Windows',
                'last_seen_at'  => now(),
            ]
        );

        $results = [];
        $acceptedCount = 0;

        DB::beginTransaction();
        try {
            foreach ($validated['jobs'] as $item) {
                // Ensure printer record exists
                $printer = Printer::firstOrCreate(
                    [
                        'device_id' => $device->id,
                        'name'      => $item['printer_name'],
                    ],
                    [
                        'status'    => 'Ready',
                        'is_active' => true,
                    ]
                );

                // Check for existing job by unique job_uid
                $existing = PrintJob::where('job_uid', $item['job_uid'])->first();

                if ($existing) {
                    // Update existing record
                    $existing->update([
                        'pages'         => max($existing->pages, $item['pages']),
                        'pages_printed' => max($existing->pages_printed, $item['pages_printed'] ?? 0),
                        'status'        => $item['status'] ?? $existing->status,
                        'completed_at'  => $item['completed_at'] ? date('Y-m-d H:i:s', strtotime($item['completed_at'])) : $existing->completed_at,
                        'error_message' => $item['error_message'] ?? $existing->error_message,
                    ]);

                    $results[] = [
                        'job_uid'   => $item['job_uid'],
                        'client_id' => $item['client_id'],
                        'remote_id' => $existing->id,
                        'status'    => 'duplicate', // recognized and updated
                        'message'   => 'Job updated',
                    ];
                } else {
                    // Insert new print job
                    $newJob = PrintJob::create([
                        'device_id'     => $device->id,
                        'printer_id'    => $printer->id,
                        'job_uid'       => $item['job_uid'],
                        'job_id'        => $item['job_id'],
                        'printer_name'  => $item['printer_name'],
                        'document_name' => $item['document_name'],
                        'username'      => $item['username'],
                        'domain'        => $item['domain'] ?? null,
                        'computer_name' => $item['computer_name'],
                        'pages'         => $item['pages'],
                        'pages_printed' => $item['pages_printed'] ?? $item['pages'],
                        'copies'        => $item['copies'] ?? 1,
                        'color_mode'    => $item['color_mode'] ?? 'Unknown',
                        'duplex'        => $item['duplex'] ?? 'Unknown',
                        'paper_size'    => $item['paper_size'] ?? 'Unknown',
                        'status'        => $item['status'] ?? 'Completed',
                        'error_message' => $item['error_message'] ?? null,
                        'submitted_at'  => date('Y-m-d H:i:s', strtotime($item['submitted_at'])),
                        'started_at'    => !empty($item['started_at']) ? date('Y-m-d H:i:s', strtotime($item['started_at'])) : null,
                        'completed_at'  => !empty($item['completed_at']) ? date('Y-m-d H:i:s', strtotime($item['completed_at'])) : null,
                    ]);

                    $acceptedCount++;
                    $results[] = [
                        'job_uid'   => $item['job_uid'],
                        'client_id' => $item['client_id'],
                        'remote_id' => $newJob->id,
                        'status'    => 'accepted',
                        'message'   => 'Job recorded',
                    ];
                }
            }

            DB::commit();
        } catch (\Exception $e) {
            DB::rollBack();
            return response()->json([
                'success' => false,
                'message' => 'Failed to process sync batch: ' . $e->getMessage(),
            ], 500);
        }

        return response()->json([
            'success'         => true,
            'processed_count' => count($results),
            'results'         => $results,
        ], 200);
    }

    /**
     * GET /api/dashboard/summary
     * Provides aggregated statistics for the Laravel Web Dashboard.
     */
    public function dashboardSummary(): JsonResponse
    {
        $today = now()->startOfDay();
        $thisMonth = now()->startOfMonth();

        return response()->json([
            'total_prints'       => PrintJob::count(),
            'total_pages'        => (int) PrintJob::sum('pages_printed'),
            'today_prints'       => PrintJob::where('submitted_at', '>=', $today)->count(),
            'today_pages'        => (int) PrintJob::where('submitted_at', '>=', $today)->sum('pages_printed'),
            'month_prints'       => PrintJob::where('submitted_at', '>=', $thisMonth)->count(),
            'color_prints'       => PrintJob::where('color_mode', 'Color')->count(),
            'monochrome_prints'  => PrintJob::where('color_mode', 'Monochrome')->count(),
            'active_devices'     => Device::where('last_seen_at', '>=', now()->subMinutes(5))->count(),
            'total_devices'      => Device::count(),
            'total_printers'     => Printer::count(),
        ], 200);
    }
}
