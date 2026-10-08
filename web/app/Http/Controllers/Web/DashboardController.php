<?php

namespace App\Http\Controllers\Web;

use App\Http\Controllers\Controller;
use App\Models\Device;
use App\Models\Printer;
use App\Models\PrintJob;
use Carbon\Carbon;
use Illuminate\Http\Request;
use Illuminate\Support\Facades\Auth;
use Symfony\Component\HttpFoundation\StreamedResponse;

class DashboardController extends Controller
{
    /**
     * Determine if currently authenticated user is platform super admin.
     */
    private function isSuperAdmin(): bool
    {
        $user = Auth::user();
        if (!$user) return false;
        return $user->email === 'admin@nexreindigital.co.ke' || (isset($user->is_admin) && $user->is_admin);
    }

    /**
     * Get accessible device IDs for the current user (multi-tenant isolation).
     */
    private function getUserDeviceIds()
    {
        $user = Auth::user();
        if ($this->isSuperAdmin()) {
            return Device::pluck('device_id');
        }
        return Device::where('user_email', $user->email)->pluck('device_id');
    }

    public function index(Request $request)
    {
        $user = Auth::user();
        $isSuperAdmin = $this->isSuperAdmin();
        $userDeviceIds = $this->getUserDeviceIds();

        // 1. Base Query with multi-tenant isolation
        $query = PrintJob::query();
        if (!$isSuperAdmin) {
            $query->where('user_email', $user->email);
        }

        // 2. Date Filtering
        $datePreset = $request->get('date_preset', 'today');
        $fromDate = $request->get('from_date');
        $toDate = $request->get('to_date');

        $now = Carbon::now();
        $dateLabel = 'Today';

        switch ($datePreset) {
            case 'today':
                $query->whereDate('submitted_at', $now->toDateString());
                $dateLabel = 'Today (' . $now->format('M d, Y') . ')';
                break;
            case 'yesterday':
                $yesterday = $now->copy()->subDay();
                $query->whereDate('submitted_at', $yesterday->toDateString());
                $dateLabel = 'Yesterday (' . $yesterday->format('M d, Y') . ')';
                break;
            case 'last_7_days':
                $query->where('submitted_at', '>=', $now->copy()->subDays(7)->startOfDay());
                $dateLabel = 'Last 7 Days';
                break;
            case 'this_month':
                $query->where('submitted_at', '>=', $now->copy()->startOfMonth());
                $dateLabel = 'This Month (' . $now->format('F Y') . ')';
                break;
            case 'custom':
                if ($fromDate && $toDate) {
                    $query->whereBetween('submitted_at', [
                        Carbon::parse($fromDate)->startOfDay(),
                        Carbon::parse($toDate)->endOfDay(),
                    ]);
                    $dateLabel = Carbon::parse($fromDate)->format('M d') . ' - ' . Carbon::parse($toDate)->format('M d, Y');
                }
                break;
            case 'all':
            default:
                $dateLabel = 'All Time';
                break;
        }

        // 3. Dropdown Filters
        if ($request->filled('shop_name')) {
            $query->where('shop_name', $request->shop_name);
        }

        if ($request->filled('computer_name')) {
            $query->where('computer_name', $request->computer_name);
        }

        if ($request->filled('printer_name')) {
            $query->where('printer_name', $request->printer_name);
        }

        if ($request->filled('color_mode')) {
            $query->where('color_mode', $request->color_mode);
        }

        if ($request->filled('search')) {
            $term = '%' . $request->search . '%';
            $query->where(function ($q) use ($term) {
                $q->where('document_name', 'like', $term)
                  ->orWhere('username', 'like', $term)
                  ->orWhere('printer_name', 'like', $term)
                  ->orWhere('shop_name', 'like', $term)
                  ->orWhere('computer_name', 'like', $term);
            });
        }

        // 4. Calculate Aggregate KPIs for filtered set
        $clone = clone $query;
        $totalJobs = $clone->count();
        $totalPages = (int) $clone->sum('total_pages_calculated');
        $colorPages = (int) (clone $query)->where('color_mode', 'Color')->sum('total_pages_calculated');
        $monoPages = (int) (clone $query)->whereIn('color_mode', ['Monochrome', 'Grayscale', 'BlackAndWhite'])->sum('total_pages_calculated');

        // Scoped lifetime and today metrics for this user
        $userJobsQuery = PrintJob::query();
        if (!$isSuperAdmin) {
            $userJobsQuery->where('user_email', $user->email);
        }
        $todayPages = (int) (clone $userJobsQuery)->whereDate('submitted_at', $now->toDateString())->sum('total_pages_calculated');
        $lifetimePages = (int) (clone $userJobsQuery)->sum('total_pages_calculated');

        // Connected devices stats scoped to this user
        $devicesQuery = Device::query();
        if (!$isSuperAdmin) {
            $devicesQuery->where('user_email', $user->email);
        }
        $activeDevicesCount = (clone $devicesQuery)->where('last_heartbeat_at', '>=', now()->subMinutes(5))->count();
        $totalDevicesCount = (clone $devicesQuery)->count();
        $totalPrintersCount = Printer::whereIn('device_id', $userDeviceIds)->count();

        // 5. Paginated Print Jobs
        $jobs = $query->orderBy('submitted_at', 'desc')->paginate(25)->withQueryString();

        // 6. Distinct options for filter controls (scoped)
        $shops = (clone $devicesQuery)->whereNotNull('shop_name')->distinct()->pluck('shop_name')->filter()->values();
        $computers = (clone $devicesQuery)->whereNotNull('computer_name')->distinct()->pluck('computer_name')->filter()->values();
        $printers = Printer::whereIn('device_id', $userDeviceIds)->distinct()->pluck('name')->filter()->values();

        return view('dashboard.index', compact(
            'jobs',
            'totalJobs',
            'totalPages',
            'colorPages',
            'monoPages',
            'todayPages',
            'lifetimePages',
            'activeDevicesCount',
            'totalDevicesCount',
            'totalPrintersCount',
            'datePreset',
            'fromDate',
            'toDate',
            'dateLabel',
            'shops',
            'computers',
            'printers'
        ));
    }

    public function devices()
    {
        $user = Auth::user();
        $isSuperAdmin = $this->isSuperAdmin();

        $query = Device::withCount(['printJobs', 'printers'])
            ->withSum('printJobs', 'total_pages_calculated')
            ->orderBy('last_heartbeat_at', 'desc');

        if (!$isSuperAdmin) {
            $query->where('user_email', $user->email);
        }

        $devices = $query->get();

        return view('dashboard.devices', compact('devices'));
    }

    public function updateSoftwarePassword(Request $request)
    {
        $request->validate([
            'device_id' => 'nullable|string',
            'shop_name' => 'nullable|string',
            'new_password' => 'required|string|min:4',
        ]);

        $user = Auth::user();
        $isSuperAdmin = $this->isSuperAdmin();

        $query = Device::query();
        if (!$isSuperAdmin) {
            $query->where('user_email', $user->email);
        }

        if ($request->filled('device_id') && $request->device_id !== 'all') {
            $query->where('device_id', $request->device_id);
        } elseif ($request->filled('shop_name') && $request->shop_name !== 'all') {
            $query->where('shop_name', $request->shop_name);
        }

        $count = $query->count();
        if ($count === 0) {
            return back()->with('error', 'No matching computers found for your account.');
        }

        $query->update([
            'software_password' => $request->new_password,
            'pending_password_update' => $request->new_password,
        ]);

        return back()->with('success', "Desktop Super Admin password staged for {$count} computer(s). It will be updated automatically on their next heartbeat!");
    }

    public function printers()
    {
        $user = Auth::user();
        $userDeviceIds = $this->getUserDeviceIds();

        $printers = Printer::with('device')
            ->whereIn('device_id', $userDeviceIds)
            ->orderBy('last_seen_at', 'desc')
            ->get();

        return view('dashboard.printers', compact('printers'));
    }

    public function updatePrinter(Request $request, $id)
    {
        $user = Auth::user();
        $userDeviceIds = $this->getUserDeviceIds();

        $printer = Printer::where('id', $id)
            ->whereIn('device_id', $userDeviceIds)
            ->firstOrFail();

        $validated = $request->validate([
            'alias_name'          => 'nullable|string|max:150',
            'location'            => 'nullable|string|max:150',
            'cost_per_mono_page'  => 'nullable|numeric|min:0',
            'cost_per_color_page' => 'nullable|numeric|min:0',
            'is_active'           => 'nullable|boolean',
            'notes'               => 'nullable|string|max:500',
        ]);

        $printer->alias_name = $validated['alias_name'] ?? null;
        $printer->location = $validated['location'] ?? null;
        $printer->cost_per_mono_page = $validated['cost_per_mono_page'] ?? 0.00;
        $printer->cost_per_color_page = $validated['cost_per_color_page'] ?? 0.00;
        $printer->is_active = $request->has('is_active');
        $printer->notes = $validated['notes'] ?? null;
        $printer->save();

        return back()->with('success', "Printer '{$printer->name}' settings updated successfully.");
    }

    public function exportCsv(Request $request): StreamedResponse
    {
        $user = Auth::user();
        $isSuperAdmin = $this->isSuperAdmin();

        $query = PrintJob::query()->orderBy('submitted_at', 'desc');

        if (!$isSuperAdmin) {
            $query->where('user_email', $user->email);
        }

        if ($request->filled('shop_name')) {
            $query->where('shop_name', $request->shop_name);
        }
        if ($request->filled('computer_name')) {
            $query->where('computer_name', $request->computer_name);
        }
        if ($request->filled('printer_name')) {
            $query->where('printer_name', $request->printer_name);
        }
        if ($request->filled('color_mode')) {
            $query->where('color_mode', $request->color_mode);
        }

        $headers = [
            'Content-Type' => 'text/csv; charset=UTF-8',
            'Content-Disposition' => 'attachment; filename="PrintJobs_' . now()->format('Ymd_His') . '.csv"',
            'Pragma' => 'no-cache',
            'Cache-Control' => 'must-revalidate, post-check=0, pre-check=0',
            'Expires' => '0',
        ];

        return response()->stream(function () use ($query) {
            $handle = fopen('php://output', 'w');
            fputcsv($handle, [
                'Job UID',
                'Date/Time (UTC)',
                'User Account',
                'Shop Name',
                'Workstation',
                'Printer',
                'Document Name',
                'User',
                'Pages Count',
                'Copies',
                'Total Pages',
                'Color Mode',
                'Duplex',
                'Paper Size',
                'Status'
            ]);

            $query->chunk(500, function ($jobs) use ($handle) {
                foreach ($jobs as $j) {
                    fputcsv($handle, [
                        $j->job_uid,
                        $j->submitted_at ? $j->submitted_at->toIso8601String() : '',
                        $j->user_email,
                        $j->shop_name,
                        $j->computer_name,
                        $j->printer_name,
                        $j->document_name,
                        $j->username,
                        $j->pages,
                        $j->copies,
                        $j->total_pages_calculated,
                        $j->color_mode,
                        $j->duplex,
                        $j->paper_size,
                        $j->status
                    ]);
                }
            });

            fclose($handle);
        }, 200, $headers);
    }
}
