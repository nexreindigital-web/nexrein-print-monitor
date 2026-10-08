@extends('layouts.portal')

@section('title', 'Remote Print Dashboard')

@push('styles')
<style>
    /* Metric Cards Grid */
    .kpi-grid {
        display: grid;
        grid-template-columns: repeat(auto-fit, minmax(260px, 1fr));
        gap: 1.25rem;
        margin-bottom: 2rem;
    }

    .kpi-card {
        background: var(--bg-card);
        border: 1px solid var(--border-color);
        border-radius: 14px;
        padding: 1.5rem;
        transition: transform 0.2s, box-shadow 0.2s;
        position: relative;
        overflow: hidden;
    }
    .kpi-card:hover {
        transform: translateY(-2px);
        box-shadow: 0 10px 25px rgba(0, 0, 0, 0.3);
    }

    .kpi-card::before {
        content: '';
        position: absolute;
        top: 0; left: 0; right: 0; height: 3px;
    }
    .kpi-card.blue::before { background: linear-gradient(90deg, #3B82F6, #60A5FA); }
    .kpi-card.emerald::before { background: linear-gradient(90deg, #10B981, #34D399); }
    .kpi-card.purple::before { background: linear-gradient(90deg, #8B5CF6, #A78BFA); }
    .kpi-card.amber::before { background: linear-gradient(90deg, #F59E0B, #FBBF24); }

    .kpi-header {
        display: flex;
        align-items: center;
        justify-content: space-between;
        margin-bottom: 0.75rem;
    }

    .kpi-title {
        font-size: 0.75rem;
        font-weight: 700;
        text-transform: uppercase;
        letter-spacing: 0.05em;
        color: var(--text-secondary);
    }

    .kpi-icon {
        font-size: 1.25rem;
    }

    .kpi-value {
        font-size: 2.25rem;
        font-weight: 800;
        color: white;
        line-height: 1.1;
        margin-bottom: 0.35rem;
    }

    .kpi-meta {
        font-size: 0.8rem;
        color: var(--text-secondary);
    }

    /* Filter Strip */
    .filter-card {
        background: var(--bg-card);
        border: 1px solid var(--border-color);
        border-radius: 14px;
        padding: 1.25rem 1.5rem;
        margin-bottom: 1.5rem;
    }

    .filter-grid {
        display: grid;
        grid-template-columns: repeat(auto-fit, minmax(180px, 1fr));
        gap: 1rem;
        align-items: end;
    }

    .form-group-filter {
        display: flex;
        flex-direction: column;
        gap: 0.35rem;
    }

    .form-group-filter label {
        font-size: 0.75rem;
        font-weight: 700;
        text-transform: uppercase;
        color: var(--text-secondary);
    }

    .filter-select, .filter-input {
        background: var(--bg-input);
        border: 1px solid var(--border-light);
        color: white;
        padding: 0.6rem 0.85rem;
        border-radius: 8px;
        font-size: 0.875rem;
    }
    .filter-select:focus, .filter-input:focus {
        outline: none;
        border-color: var(--accent-blue);
    }

    /* Data Table Card */
    .table-card {
        background: var(--bg-card);
        border: 1px solid var(--border-color);
        border-radius: 14px;
        overflow: hidden;
        margin-bottom: 1.5rem;
    }

    .table-header {
        padding: 1.25rem 1.5rem;
        border-bottom: 1px solid var(--border-color);
        display: flex;
        align-items: center;
        justify-content: space-between;
        flex-wrap: wrap;
        gap: 1rem;
    }

    .table-title {
        font-size: 1.15rem;
        font-weight: 700;
    }

    .table-responsive {
        overflow-x: auto;
    }

    table {
        width: 100%;
        border-collapse: collapse;
        text-align: left;
        font-size: 0.875rem;
    }

    thead {
        background-color: rgba(15, 23, 42, 0.7);
        border-bottom: 1px solid var(--border-color);
    }

    th {
        padding: 0.85rem 1.25rem;
        font-size: 0.75rem;
        font-weight: 700;
        text-transform: uppercase;
        letter-spacing: 0.05em;
        color: var(--text-secondary);
        white-space: nowrap;
    }

    td {
        padding: 1rem 1.25rem;
        border-bottom: 1px solid var(--border-color);
        white-space: nowrap;
    }

    tr:hover {
        background-color: var(--bg-card-hover);
    }

    /* Badges */
    .badge {
        display: inline-flex;
        align-items: center;
        gap: 0.3rem;
        padding: 0.25rem 0.65rem;
        border-radius: 9999px;
        font-size: 0.75rem;
        font-weight: 700;
    }
    .badge-color { background: rgba(16, 185, 129, 0.15); color: #34D399; border: 1px solid rgba(16, 185, 129, 0.3); }
    .badge-mono  { background: rgba(148, 163, 184, 0.15); color: #CBD5E1; border: 1px solid rgba(148, 163, 184, 0.3); }
    .badge-shop  { background: rgba(59, 130, 246, 0.15); color: #93C5FD; border: 1px solid rgba(59, 130, 246, 0.3); }
    .badge-status { background: rgba(139, 92, 246, 0.15); color: #C4B5FD; border: 1px solid rgba(139, 92, 246, 0.3); }

    /* Custom Pagination */
    .pagination-bar {
        padding: 1rem 1.5rem;
        display: flex;
        align-items: center;
        justify-content: space-between;
        background: rgba(15, 23, 42, 0.5);
    }
</style>
@endpush

@section('content')
<div style="margin-bottom: 1.5rem; display: flex; align-items: center; justify-content: space-between; flex-wrap: wrap; gap: 1rem;">
    <div>
        <h1 style="font-size: 1.75rem; font-weight: 800; letter-spacing: -0.02em;">Remote Live Print Audit</h1>
        <p style="color: var(--text-secondary); font-size: 0.9rem;">
            Real-time page counts, shop accounting, and document monitoring for <strong>printmonitor.nexreindigital.co.ke</strong>
        </p>
    </div>
    <div style="display: flex; gap: 0.75rem;">
        <a href="{{ route('export.csv', request()->all()) }}" class="btn btn-secondary">
            📥 Export Filtered CSV
        </a>
        <a href="{{ route('dashboard') }}" class="btn btn-primary">
            🔄 Refresh Data
        </a>
    </div>
</div>

<!-- 4 Hero KPI Cards -->
<div class="kpi-grid">
    <div class="kpi-card blue">
        <div class="kpi-header">
            <span class="kpi-title">Selected Period Pages</span>
            <span class="kpi-icon">📅</span>
        </div>
        <div class="kpi-value">{{ number_format($totalPages) }}</div>
        <div class="kpi-meta">
            {{ $dateLabel }} &bull; {{ number_format($totalJobs) }} print jobs
        </div>
    </div>

    <div class="kpi-card emerald">
        <div class="kpi-header">
            <span class="kpi-title">Today's Global Pages</span>
            <span class="kpi-icon">🖨️</span>
        </div>
        <div class="kpi-value">{{ number_format($todayPages) }}</div>
        <div class="kpi-meta">
            Live today across all shops &amp; counters
        </div>
    </div>

    <div class="kpi-card purple">
        <div class="kpi-header">
            <span class="kpi-title">Color vs. B&amp;W Breakdown</span>
            <span class="kpi-icon">🎨</span>
        </div>
        <div class="kpi-value" style="font-size: 1.6rem; margin-top: 0.35rem;">
            <span style="color: #34D399;">{{ number_format($colorPages) }}</span> <span style="font-size: 1rem; color: var(--text-muted);">Clr</span> / 
            <span style="color: #CBD5E1;">{{ number_format($monoPages) }}</span> <span style="font-size: 1rem; color: var(--text-muted);">Mono</span>
        </div>
        <div class="kpi-meta">
            Page accounting in selected filter
        </div>
    </div>

    <div class="kpi-card amber">
        <div class="kpi-header">
            <span class="kpi-title">Shops &amp; Printers Online</span>
            <span class="kpi-icon">💻</span>
        </div>
        <div class="kpi-value">{{ $activeDevicesCount }} <span style="font-size: 1.1rem; color: var(--text-muted);">/ {{ $totalDevicesCount }} PCs</span></div>
        <div class="kpi-meta">
            {{ $totalPrintersCount }} fleet printers configured
        </div>
    </div>
</div>

<!-- Filter Toolbar -->
<div class="filter-card">
    <form action="{{ route('dashboard') }}" method="GET">
        <div class="filter-grid">
            <!-- Date Filter Preset -->
            <div class="form-group-filter">
                <label>Date Filter</label>
                <select name="date_preset" id="datePreset" class="filter-select" onchange="toggleCustomDates(this.value)">
                    <option value="today" {{ $datePreset === 'today' ? 'selected' : '' }}>Today</option>
                    <option value="yesterday" {{ $datePreset === 'yesterday' ? 'selected' : '' }}>Yesterday</option>
                    <option value="last_7_days" {{ $datePreset === 'last_7_days' ? 'selected' : '' }}>Last 7 Days</option>
                    <option value="this_month" {{ $datePreset === 'this_month' ? 'selected' : '' }}>This Month</option>
                    <option value="all" {{ $datePreset === 'all' ? 'selected' : '' }}>All Time</option>
                    <option value="custom" {{ $datePreset === 'custom' ? 'selected' : '' }}>Custom Date Range</option>
                </select>
            </div>

            <!-- Custom From Date -->
            <div class="form-group-filter custom-date-col" style="{{ $datePreset === 'custom' ? '' : 'display:none;' }}">
                <label>From Date</label>
                <input type="date" name="from_date" value="{{ $fromDate }}" class="filter-input">
            </div>

            <!-- Custom To Date -->
            <div class="form-group-filter custom-date-col" style="{{ $datePreset === 'custom' ? '' : 'display:none;' }}">
                <label>To Date</label>
                <input type="date" name="to_date" value="{{ $toDate }}" class="filter-input">
            </div>

            <!-- Shop / Branch Filter -->
            <div class="form-group-filter">
                <label>Shop / Branch</label>
                <select name="shop_name" class="filter-select">
                    <option value="">All Shops &amp; Branches</option>
                    @foreach ($shops as $s)
                        <option value="{{ $s }}" {{ request('shop_name') === $s ? 'selected' : '' }}>{{ $s }}</option>
                    @endforeach
                </select>
            </div>

            <!-- Computer Filter -->
            <div class="form-group-filter">
                <label>Computer Name</label>
                <select name="computer_name" class="filter-select">
                    <option value="">All Computers</option>
                    @foreach ($computers as $c)
                        <option value="{{ $c }}" {{ request('computer_name') === $c ? 'selected' : '' }}>{{ $c }}</option>
                    @endforeach
                </select>
            </div>

            <!-- Printer Filter -->
            <div class="form-group-filter">
                <label>Printer Name</label>
                <select name="printer_name" class="filter-select">
                    <option value="">All Printers</option>
                    @foreach ($printers as $p)
                        <option value="{{ $p }}" {{ request('printer_name') === $p ? 'selected' : '' }}>{{ $p }}</option>
                    @endforeach
                </select>
            </div>

            <!-- Color Mode -->
            <div class="form-group-filter">
                <label>Color Mode</label>
                <select name="color_mode" class="filter-select">
                    <option value="">All Colors</option>
                    <option value="Color" {{ request('color_mode') === 'Color' ? 'selected' : '' }}>Color</option>
                    <option value="Monochrome" {{ request('color_mode') === 'Monochrome' ? 'selected' : '' }}>Black &amp; White / Mono</option>
                </select>
            </div>

            <!-- Search Field -->
            <div class="form-group-filter">
                <label>Search Document / User</label>
                <input type="text" name="search" value="{{ request('search') }}" placeholder="File, user, or doc..." class="filter-input">
            </div>

            <!-- Action Buttons -->
            <div style="display: flex; gap: 0.5rem;">
                <button type="submit" class="btn btn-primary" style="flex: 1;">
                    🔍 Apply Filter
                </button>
                <a href="{{ route('dashboard') }}" class="btn btn-secondary">
                    Reset
                </a>
            </div>
        </div>
    </form>
</div>

<!-- Print Jobs Data Table -->
<div class="table-card">
    <div class="table-header">
        <div class="table-title">
            📄 Print Audit Records
            <span style="font-size: 0.85rem; font-weight: 500; color: var(--text-secondary); margin-left: 0.5rem;">
                (Showing {{ $jobs->firstItem() ?? 0 }} - {{ $jobs->lastItem() ?? 0 }} of {{ $jobs->total() }} records)
            </span>
        </div>
    </div>

    <div class="table-responsive">
        <table>
            <thead>
                <tr>
                    <th>Doc</th>
                    <th>Document Name</th>
                    <th>Shop / Branch</th>
                    <th>Computer</th>
                    <th>User</th>
                    <th>Printer</th>
                    <th>Pages × Copies</th>
                    <th>Total Pages</th>
                    <th>Color Mode</th>
                    <th>Status</th>
                    <th>Printed At</th>
                </tr>
            </thead>
            <tbody>
                @forelse ($jobs as $job)
                    <tr>
                        <td style="font-size: 1.25rem;">{{ $job->document_icon }}</td>
                        <td>
                            <strong style="color: white;">{{ $job->document_name }}</strong>
                            <div style="font-size: 0.75rem; color: var(--text-muted);">UID: {{ Str::limit($job->job_uid, 22) }}</div>
                        </td>
                        <td>
                            <span class="badge badge-shop">🏪 {{ $job->shop_name ?: 'Main Shop' }}</span>
                        </td>
                        <td>{{ $job->computer_name }}</td>
                        <td>{{ $job->username }}</td>
                        <td style="color: #93C5FD;">🖨️ {{ $job->printer_name }}</td>
                        <td>{{ $job->pages }} pgs &times; {{ $job->copies }} cpy</td>
                        <td>
                            <strong style="font-size: 1rem; color: #38BDF8;">{{ $job->total_pages_calculated }}</strong>
                        </td>
                        <td>
                            @if (stripos($job->color_mode, 'color') !== false)
                                <span class="badge badge-color">🌈 Color</span>
                            @else
                                <span class="badge badge-mono">⚫ B&amp;W / Mono</span>
                            @endif
                        </td>
                        <td>
                            <span class="badge badge-status">{{ $job->status }}</span>
                        </td>
                        <td style="color: var(--text-secondary);">
                            {{ $job->submitted_at ? $job->submitted_at->format('M d, Y H:i:s') : 'N/A' }}
                        </td>
                    </tr>
                @empty
                    <tr>
                        <td colspan="11" style="text-align: center; padding: 3rem; color: var(--text-secondary);">
                            <div style="font-size: 2rem; margin-bottom: 0.5rem;">📭</div>
                            No print jobs recorded matching your filter parameters.
                        </td>
                    </tr>
                @endforelse
            </tbody>
        </table>
    </div>

    @if ($jobs->hasPages())
        <div class="pagination-bar">
            {{ $jobs->links() }}
        </div>
    @endif
</div>
@endsection

@push('scripts')
<script>
    function toggleCustomDates(val) {
        const cols = document.querySelectorAll('.custom-date-col');
        cols.forEach(c => {
            c.style.display = (val === 'custom') ? 'flex' : 'none';
        });
    }
</script>
@endpush
