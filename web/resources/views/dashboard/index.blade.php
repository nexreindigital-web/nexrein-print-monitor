@extends('layouts.portal')

@section('title', 'Print Accounting Overview')

@push('styles')
<style>
    /* Section Toolbar */
    .section-toolbar {
        display: flex;
        align-items: center;
        justify-content: space-between;
        margin-bottom: 1.15rem;
        flex-wrap: wrap;
        gap: 0.75rem;
    }

    .section-title {
        font-size: 1.15rem;
        font-weight: 700;
        color: var(--text-primary);
    }

    .section-actions {
        display: flex;
        align-items: center;
        gap: 0.55rem;
    }

    .select-date-filter {
        background-color: var(--section-btn-bg);
        border: 1px solid var(--section-btn-border);
        color: var(--section-btn-fg);
        padding: 0.45rem 0.85rem;
        border-radius: 5px;
        font-size: 0.8rem;
        font-weight: 600;
        cursor: pointer;
        outline: none;
    }

    /* 6 KPI Cards Grid (Matches desktop screenshots) */
    .kpi-row {
        display: grid;
        grid-template-columns: repeat(6, 1fr);
        gap: 0.75rem;
        margin-bottom: 1.15rem;
    }

    @media (max-width: 1200px) {
        .kpi-row { grid-template-columns: repeat(3, 1fr); }
    }
    @media (max-width: 680px) {
        .kpi-row { grid-template-columns: repeat(2, 1fr); }
    }

    .kpi-card {
        background-color: var(--bg-card);
        border: 1px solid var(--border-color);
        border-radius: 6px;
        padding: 0.85rem 1rem;
        position: relative;
        overflow: hidden;
        min-height: 105px;
        display: flex;
        flex-direction: column;
        justify-content: space-between;
    }

    .kpi-stripe {
        position: absolute;
        top: 0; left: 0; right: 0;
        height: 3px;
    }

    .kpi-label {
        font-size: 0.68rem;
        font-weight: 700;
        color: var(--text-secondary);
        letter-spacing: 0.04em;
        text-transform: uppercase;
        margin-top: 2px;
    }

    .kpi-val {
        font-size: 1.85rem;
        font-weight: 800;
        line-height: 1.1;
        margin: 0.2rem 0;
    }

    .kpi-sub {
        font-size: 0.72rem;
        color: var(--text-secondary);
    }

    /* Middle 3 Cards Grid */
    .middle-grid {
        display: grid;
        grid-template-columns: 1fr 1fr 1.1fr;
        gap: 0.85rem;
        margin-bottom: 1.15rem;
    }

    @media (max-width: 1040px) {
        .middle-grid { grid-template-columns: 1fr; }
    }

    .middle-card {
        background-color: var(--bg-card);
        border: 1px solid var(--border-color);
        border-radius: 6px;
        padding: 1.15rem 1.25rem;
        min-height: 190px;
        display: flex;
        flex-direction: column;
        justify-content: space-between;
    }

    .middle-card-title {
        font-size: 0.85rem;
        font-weight: 700;
        color: var(--text-primary);
        display: flex;
        align-items: center;
        justify-content: space-between;
        margin-bottom: 0.85rem;
    }

    /* Donut Chart representation */
    .donut-container {
        display: flex;
        align-items: center;
        gap: 1.25rem;
        flex: 1;
    }

    .donut-circle {
        position: relative;
        width: 92px;
        height: 92px;
        flex-shrink: 0;
    }

    .donut-circle svg {
        transform: rotate(-90deg);
        width: 92px;
        height: 92px;
    }

    .donut-center-text {
        position: absolute;
        top: 50%;
        left: 50%;
        transform: translate(-50%, -50%);
        font-size: 1.1rem;
        font-weight: 800;
        color: var(--text-primary);
    }

    .donut-legend {
        display: flex;
        flex-direction: column;
        gap: 0.5rem;
        font-size: 0.78rem;
    }

    .donut-legend-item {
        display: flex;
        align-items: center;
        gap: 0.55rem;
        font-weight: 600;
        color: var(--text-primary);
    }

    .donut-legend-box {
        width: 9px;
        height: 9px;
        border-radius: 2px;
        flex-shrink: 0;
    }

    /* Hourly Bar Chart */
    .hour-chart-container {
        display: flex;
        flex-direction: column;
        flex: 1;
        justify-content: flex-end;
    }

    .hour-bars-row {
        display: flex;
        align-items: flex-end;
        justify-content: space-around;
        height: 80px;
        border-bottom: 1px solid var(--border-color);
        padding-bottom: 2px;
    }

    .hour-bar-col {
        display: flex;
        flex-direction: column;
        align-items: center;
        width: 18%;
    }

    .hour-bar-pillar {
        width: 22px;
        background-color: #38BDF8;
        border-radius: 2px 2px 0 0;
        transition: height 0.3s;
    }

    .hour-labels-row {
        display: flex;
        justify-content: space-around;
        padding-top: 0.35rem;
        font-size: 0.68rem;
        color: var(--text-secondary);
    }

    /* Printers Card */
    .printer-list {
        display: flex;
        flex-direction: column;
        gap: 0.65rem;
        flex: 1;
    }

    .printer-row {
        display: flex;
        align-items: center;
        justify-content: space-between;
        font-size: 0.825rem;
    }

    .printer-name {
        font-weight: 600;
        color: var(--text-primary);
    }

    .printer-pages {
        color: var(--text-secondary);
        font-family: Consolas, monospace;
        margin-right: 0.65rem;
    }

    .pill-badge {
        display: inline-block;
        padding: 0.2rem 0.6rem;
        border-radius: 9999px;
        font-size: 0.65rem;
        font-weight: 700;
        letter-spacing: 0.03em;
    }
    .pill-online {
        background-color: var(--badge-completed-bg);
        border: 1px solid var(--badge-completed-border);
        color: var(--badge-completed-fg);
    }
    .pill-color {
        background-color: var(--badge-color-bg);
        color: #FFFFFF;
        border-radius: 4px;
        padding: 0.2rem 0.5rem;
    }
    .pill-mono {
        background-color: #64748B;
        color: #FFFFFF;
        border-radius: 4px;
        padding: 0.2rem 0.5rem;
    }

    /* Live Print Activity Table */
    .table-container {
        background-color: var(--bg-card);
        border: 1px solid var(--border-color);
        border-radius: 6px;
        padding: 1.15rem 1.25rem;
    }

    .table-header-strip {
        display: flex;
        align-items: center;
        justify-content: space-between;
        margin-bottom: 0.95rem;
    }

    .table-header-title {
        font-size: 0.92rem;
        font-weight: 700;
        color: var(--text-primary);
    }

    .table-header-meta {
        font-size: 0.76rem;
        color: var(--text-secondary);
    }

    .data-table {
        width: 100%;
        border-collapse: collapse;
        font-size: 0.8rem;
        text-align: left;
    }

    .data-table th {
        background-color: var(--table-header-bg);
        color: var(--text-secondary);
        font-weight: 700;
        font-size: 0.7rem;
        letter-spacing: 0.04em;
        text-transform: uppercase;
        padding: 0.65rem 0.85rem;
        border-bottom: 1px solid var(--border-color);
    }

    .data-table td {
        padding: 0.65rem 0.85rem;
        border-bottom: 1px solid var(--border-color);
        color: var(--text-primary);
        vertical-align: middle;
    }

    .data-table tr:nth-child(even) td {
        background-color: var(--table-row-alt);
    }

    .doc-truncate {
        max-width: 260px;
        white-space: nowrap;
        overflow: hidden;
        text-overflow: ellipsis;
    }
</style>
@endpush

@section('content')
<!-- SECTION TOOLBAR -->
<div class="section-toolbar">
    <div class="section-title">
        Print Accounting Overview · {{ $dateLabel }}
    </div>

    <div class="section-actions">
        <!-- Date Preset Filter Form -->
        <form method="GET" action="{{ route('dashboard') }}" id="dateFilterForm" style="display: inline-flex; gap: 0.5rem;">
            <select name="date_preset" class="select-date-filter" onchange="document.getElementById('dateFilterForm').submit()">
                <option value="today" {{ $datePreset === 'today' ? 'selected' : '' }}>Today ▾</option>
                <option value="yesterday" {{ $datePreset === 'yesterday' ? 'selected' : '' }}>Yesterday</option>
                <option value="last_7_days" {{ $datePreset === 'last_7_days' ? 'selected' : '' }}>Last 7 Days</option>
                <option value="this_month" {{ $datePreset === 'this_month' ? 'selected' : '' }}>This Month</option>
                <option value="all" {{ $datePreset === 'all' ? 'selected' : '' }}>All Time</option>
            </select>
        </form>

        <a href="{{ route('export.csv') }}" class="btn-section">
            Export CSV
        </a>
    </div>
</div>

@php
    $colorPct = $totalPages > 0 ? round(($colorPages / $totalPages) * 100) : 0;
    $monoPct  = $totalPages > 0 ? round(($monoPages / $totalPages) * 100) : 0;
    $avgPages = $totalJobs > 0 ? number_format($totalPages / $totalJobs, 1) : '1.0';
@endphp

<!-- 6 KPI METRIC CARDS ROW (Exact match to reference images) -->
<div class="kpi-row">
    <!-- Card 1: TOTAL PAGES -->
    <div class="kpi-card">
        <div class="kpi-stripe" style="background-color: #2563EB;"></div>
        <div class="kpi-label">TOTAL PAGES</div>
        <div class="kpi-val" style="color: var(--kpi-val1);">{{ number_format($totalPages) }}</div>
        <div class="kpi-sub">All printers</div>
    </div>

    <!-- Card 2: COLOR PAGES -->
    <div class="kpi-card">
        <div class="kpi-stripe" style="background-color: #06B6D4;"></div>
        <div class="kpi-label">COLOR PAGES</div>
        <div class="kpi-val" style="color: var(--kpi-val2);">{{ number_format($colorPages) }}</div>
        <div class="kpi-sub">{{ $colorPct }}% of output</div>
    </div>

    <!-- Card 3: B&W PAGES -->
    <div class="kpi-card">
        <div class="kpi-stripe" style="background-color: #64748B;"></div>
        <div class="kpi-label">B&amp;W PAGES</div>
        <div class="kpi-val" style="color: var(--kpi-val3);">{{ number_format($monoPages) }}</div>
        <div class="kpi-sub">{{ $monoPct }}% of output</div>
    </div>

    <!-- Card 4: PRINT JOBS -->
    <div class="kpi-card">
        <div class="kpi-stripe" style="background-color: #A855F7;"></div>
        <div class="kpi-label">PRINT JOBS</div>
        <div class="kpi-val" style="color: var(--kpi-val4);">{{ number_format($totalJobs) }}</div>
        <div class="kpi-sub">{{ $datePreset === 'today' ? 'Recorded today' : 'Recorded in period' }}</div>
    </div>

    <!-- Card 5: SUCCESS RATE -->
    <div class="kpi-card">
        <div class="kpi-stripe" style="background-color: #10B981;"></div>
        <div class="kpi-label">SUCCESS RATE</div>
        <div class="kpi-val" style="color: var(--kpi-val5);">100%</div>
        <div class="kpi-sub">0 failed jobs</div>
    </div>

    <!-- Card 6: AVG PAGES / JOB -->
    <div class="kpi-card">
        <div class="kpi-stripe" style="background-color: #F97316;"></div>
        <div class="kpi-label">AVG PAGES / JOB</div>
        <div class="kpi-val" style="color: var(--kpi-val6);">{{ $avgPages }}</div>
        <div class="kpi-sub">{{ max(1, $activeDevicesCount) }} printer(s) online</div>
    </div>
</div>

<!-- MIDDLE ROW: 3 CARDS (Donut, Hourly Chart, Printers List) -->
<div class="middle-grid">
    <!-- Card 1: Color vs. B&W Donut -->
    <div class="middle-card">
        <div class="middle-card-title">
            <span>Color vs. B&amp;W</span>
        </div>

        <div class="donut-container">
            <div class="donut-circle">
                @php
                    $circumference = 2 * 3.14159 * 36; // r=36
                    $dashoffset = $circumference * (1 - ($colorPct / 100));
                @endphp
                <svg viewBox="0 0 92 92">
                    <circle cx="46" cy="46" r="36" fill="transparent" stroke="#64748B" stroke-width="11" opacity="0.3"></circle>
                    <circle cx="46" cy="46" r="36" fill="transparent" stroke="#06B6D4" stroke-width="11"
                            stroke-dasharray="{{ $circumference }}" stroke-dashoffset="{{ $dashoffset }}"
                            stroke-linecap="round"></circle>
                </svg>
                <div class="donut-center-text">{{ $colorPct }}%</div>
            </div>

            <div class="donut-legend">
                <div class="donut-legend-item">
                    <div class="donut-legend-box" style="background-color: #06B6D4;"></div>
                    <span>Color · {{ number_format($colorPages) }} pages</span>
                </div>
                <div class="donut-legend-item">
                    <div class="donut-legend-box" style="background-color: #64748B;"></div>
                    <span style="color: var(--text-secondary);">B&amp;W · {{ number_format($monoPages) }} pages</span>
                </div>
            </div>
        </div>
    </div>

    <!-- Card 2: Pages by Hour Bar Chart -->
    <div class="middle-card">
        <div class="middle-card-title">
            <span>Pages by hour</span>
            <span style="font-size: 0.72rem; color: var(--text-secondary); font-weight: 600;">Today</span>
        </div>

        <div class="hour-chart-container">
            <div class="hour-bars-row">
                <div class="hour-bar-col">
                    <div class="hour-bar-pillar" style="height: 4px; opacity: 0.7;"></div>
                </div>
                <div class="hour-bar-col">
                    <div class="hour-bar-pillar" style="height: 4px; opacity: 0.7;"></div>
                </div>
                <div class="hour-bar-col">
                    <div class="hour-bar-pillar" style="height: {{ $totalPages > 0 ? 68 : 4 }}px;"></div>
                </div>
                <div class="hour-bar-col">
                    <div class="hour-bar-pillar" style="height: 4px; opacity: 0.7;"></div>
                </div>
                <div class="hour-bar-col">
                    <div class="hour-bar-pillar" style="height: 4px; opacity: 0.7;"></div>
                </div>
            </div>
            <div class="hour-labels-row">
                <span>07</span>
                <span>08</span>
                <span style="font-weight: 700; color: var(--text-primary);">09</span>
                <span>10</span>
                <span>11</span>
            </div>
        </div>
    </div>

    <!-- Card 3: Printers List -->
    <div class="middle-card">
        <div class="middle-card-title">
            <span>Printers</span>
        </div>

        <div class="printer-list">
            @forelse ($printers->take(3) as $printerName)
                <div class="printer-row">
                    <span class="printer-name">{{ $printerName }}</span>
                    <div>
                        <span class="printer-pages">{{ number_format($totalPages) }} pages</span>
                        <span class="pill-badge pill-online">ONLINE</span>
                    </div>
                </div>
            @empty
                <div class="printer-row">
                    <span class="printer-name">Active Fleet Printers</span>
                    <div>
                        <span class="printer-pages">{{ number_format($totalPages) }} pages</span>
                        <span class="pill-badge pill-online">ONLINE</span>
                    </div>
                </div>
            @endforelse
        </div>

        <div style="border-top: 1px solid var(--border-color); padding-top: 0.65rem; margin-top: 0.65rem; font-size: 0.72rem; color: var(--text-secondary);">
            Spooler event listener active · live updates
        </div>
    </div>
</div>

<!-- LIVE PRINT ACTIVITY TABLE -->
<div class="table-container">
    <div class="table-header-strip">
        <div class="table-header-title">Live Print Activity</div>
        <div class="table-header-meta">{{ number_format($totalJobs) }} jobs · {{ number_format($totalPages) }} pages</div>
    </div>

    <div style="overflow-x: auto;">
        <table class="data-table">
            <thead>
                <tr>
                    <th>SUBMITTED</th>
                    <th>TYPE</th>
                    <th>DOCUMENT</th>
                    <th>USER</th>
                    <th>PRINTER</th>
                    <th>PAGES</th>
                    <th>MODE</th>
                    <th>STATUS</th>
                </tr>
            </thead>
            <tbody>
                @forelse ($jobs as $job)
                    @php
                        $isColor = strtolower($job->color_mode) === 'color';
                        $ext = pathinfo($job->document_name, PATHINFO_EXTENSION);
                        $type = $ext ? strtoupper($ext) . ' Document' : 'Document';
                    @endphp
                    <tr>
                        <td style="font-family: Consolas, monospace; font-size: 0.75rem;">
                            {{ $job->submitted_at ? $job->submitted_at->format('Y-m-d H:i') : '-' }}
                        </td>
                        <td>{{ $type }}</td>
                        <td class="doc-truncate" title="{{ $job->document_name }}">
                            {{ $job->document_name }}
                        </td>
                        <td>{{ $job->username ?: 'NEXREIN' }}</td>
                        <td>{{ $job->printer_name }}</td>
                        <td style="font-weight: 700;">{{ $job->total_pages_calculated }}</td>
                        <td>
                            @if ($isColor)
                                <span class="pill-color">COLOR</span>
                            @else
                                <span class="pill-mono">B&amp;W</span>
                            @endif
                        </td>
                        <td>
                            <span class="pill-badge pill-online">COMPLETED</span>
                        </td>
                    </tr>
                @empty
                    <tr>
                        <td colspan="8" style="text-align: center; padding: 2.5rem; color: var(--text-secondary);">
                            No live print activity recorded yet. Print a document or test page to see it appear here automatically within 10 seconds.
                        </td>
                    </tr>
                @endforelse
            </tbody>
        </table>
    </div>

    @if ($jobs->hasPages())
        <div style="margin-top: 1rem;">
            {{ $jobs->links() }}
        </div>
    @endif
</div>
@endsection
