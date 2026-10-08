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
    public function index(Request $request)
    {
        $user = Auth::user();

        // 1. Base Query
        $query = PrintJob::query();

        // If user is tied to specific email or shops, filter by user email unless admin
        if ($user && $user->email !== 'admin@nexreindigital.co.ke' && !str_starts_with($user->email, 'admin')) {
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

        // Today specific lifetime KPI cards
        $todayPages = (int) PrintJob::whereDate('submitted_at', $now->toDateString())->sum('total_pages_calculated');
        $lifetimePages = (int) PrintJob::sum('total_pages_calculated');

        // Connected devices stats
        $activeDevicesCount = Device::where('last_heartbeat_at', '>=', now()->subMinutes(5))->count();
        $totalDevicesCount = Device::count();
        $totalPrintersCount = Printer::count();

        // 5. Paginated Print Jobs
        $jobs = $query->orderBy('submitted_at', 'desc')->paginate(25)->withQueryString();

        // 6. Distinct options for filter controls
        $shops = Device::whereNotNull('shop_name')->distinct()->pluck('shop_name')->filter()->values();
        $computers = Device::whereNotNull('computer_name')->distinct()->pluck('computer_name')->filter()->values();
        $printers = Printer::distinct()->pluck('name')->filter()->values();

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
        $devices = Device::withCount(['printJobs', 'printers'])
            ->withSum('printJobs', 'total_pages_calculated')
            ->orderBy('last_heartbeat_at', 'desc')
            ->get();

        return view('dashboard.devices', compact('devices'));
    }

    public function updateSoftwarePassword(Request $request)
    {
        $request->validate([
            'device_id' => 'nullable|string',
            'shop_name' => 'nullable|string',
            'new_password' => 'required|string|min:4',
        ]);

        $query = Device::query();
        if ($request->filled('device_id') && $request->device_id !== 'all') {
            $query->where('device_id', $request->device_id);
        } elseif ($request->filled('shop_name') && $request->shop_name !== 'all') {
            $query->where('shop_name', $request->shop_name);
        }

        $count = $query->count();
        $query->update([
            'software_password' => $request->new_password,
            'pending_password_update' => $request->new_password,
        ]);

        return back()->with('success', "Desktop Super Admin password staged for {$count} computer(s). It will be updated automatically on their next heartbeat!");
    }

    public function printers()
    {
        $printers = Printer::with('device')
            ->orderBy('last_seen_at', 'desc')
            ->get();

        return view('dashboard.printers', compact('printers'));
    }

    public function exportCsv(Request $request): StreamedResponse
    {
        $query = PrintJob::query()->orderBy('submitted_at', 'desc');

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
            'Content-Type' => 'text/csv',
            'Content-Disposition' => 'attachment; filename="print_jobs_export_' . date('Y-m-d_His') . '.csv"',
        ];

        return new StreamedResponse(function () use ($query) {
            $handle = fopen('php://output', 'w');
            fputcsv($handle, [
                'Job UID',
                'Shop / Branch',
                'Computer Name',
                'User Account',
                'Printer',
                'Document Name',
                'Pages',
                'Copies',
                'Total Pages',
                'Color Mode',
                'Duplex',
                'Status',
                'Submitted At',
            ]);

            $query->chunk(500, function ($jobs) use ($handle) {
                foreach ($jobs as $j) {
                    fputcsv($handle, [
                        $j->job_uid,
                        $j->shop_name,
                        $j->computer_name,
                        $j->username,
                        $j->printer_name,
                        $j->document_name,
                        $j->pages,
                        $j->copies,
                        $j->total_pages_calculated,
                        $j->color_mode,
                        $j->duplex,
                        $j->status,
                        $j->submitted_at ? $j->submitted_at->toDateTimeString() : '',
                    ]);
                }
            });

            fclose($handle);
        }, 200, $headers);
    }
}
