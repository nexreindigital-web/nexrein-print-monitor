@extends('layouts.portal')

@section('title', 'Computers & Workstations')

@push('styles')
<style>
    .section-toolbar {
        display: flex; align-items: center;
        justify-content: space-between;
        margin-bottom: 1.15rem;
        flex-wrap: wrap; gap: 0.75rem;
    }

    .section-title {
        font-size: 1.15rem; font-weight: 700;
        color: var(--text-primary);
    }

    /* Devices grid: main table + side password card */
    .device-layout {
        display: grid;
        grid-template-columns: 1fr 300px;
        gap: 1.25rem;
        align-items: start;
    }
    @media (max-width: 1100px) {
        .device-layout { grid-template-columns: 1fr; }
    }

    /* ── Table Card ── */
    .table-card {
        background-color: var(--bg-card);
        border: 1px solid var(--border-color);
        border-radius: 6px;
        padding: 1.15rem 1.25rem;
    }

    .table-header {
        display: flex; align-items: center;
        justify-content: space-between;
        margin-bottom: 1rem;
    }

    .data-table {
        width: 100%; border-collapse: collapse;
        font-size: 0.78rem; text-align: left;
    }
    .data-table th {
        background-color: var(--table-header-bg);
        color: var(--text-secondary);
        font-weight: 700; font-size: 0.68rem;
        letter-spacing: 0.04em; text-transform: uppercase;
        padding: 0.65rem 0.85rem;
        border-bottom: 1px solid var(--border-color);
        white-space: nowrap;
    }
    .data-table td {
        padding: 0.75rem 0.85rem;
        border-bottom: 1px solid var(--border-color);
        color: var(--text-primary);
        vertical-align: middle;
    }
    .data-table tr:nth-child(even) td {
        background-color: var(--table-row-alt);
    }
    .data-table tr:hover td {
        background-color: var(--bg-card-hover);
    }

    /* ── Status badges ── */
    .status-pill {
        display: inline-flex; align-items: center;
        gap: 0.3rem; border-radius: 9999px;
        padding: 0.2rem 0.65rem;
        font-size: 0.68rem; font-weight: 700;
        white-space: nowrap;
    }
    .status-dot {
        width: 6px; height: 6px;
        border-radius: 50%; flex-shrink: 0;
    }
    .status-online {
        background: var(--badge-completed-bg);
        border: 1px solid var(--badge-completed-border);
        color: var(--badge-completed-fg);
    }
    .status-online .status-dot {
        background-color: #10B981;
        box-shadow: 0 0 6px #10B981;
        animation: pulse-dot 2s infinite;
    }
    .status-offline {
        background: rgba(100,116,139,0.12);
        border: 1px solid #475569;
        color: #94A3B8;
    }
    .status-offline .status-dot { background-color: #64748B; }

    @keyframes pulse-dot {
        0%, 100% { opacity: 1; transform: scale(1); }
        50% { opacity: 0.5; transform: scale(0.8); }
    }

    .pill-badge {
        display: inline-block; font-size: 0.68rem;
        font-weight: 700; padding: 0.15rem 0.5rem;
        border-radius: 4px; white-space: nowrap;
    }

    /* ── Device Detail Rows ── */
    .device-meta {
        font-size: 0.72rem; color: var(--text-secondary);
        margin-top: 2px;
    }

    /* ── Password Push Card ── */
    .password-card {
        background-color: var(--bg-card);
        border: 1px solid var(--border-color);
        border-radius: 6px;
        padding: 1.25rem;
        position: sticky;
        top: 80px;
    }

    .form-control-theme {
        width: 100%;
        background-color: var(--bg-input);
        border: 1px solid var(--border-color);
        color: var(--text-primary);
        padding: 0.6rem 0.8rem;
        border-radius: 6px;
        font-size: 0.82rem;
        outline: none;
        margin-bottom: 0.75rem;
    }
    .form-control-theme:focus { border-color: var(--accent-primary); }

    .btn-push {
        width: 100%;
        background-color: var(--accent-primary);
        color: #0F172A;
        font-weight: 700;
        border: none;
        padding: 0.7rem;
        border-radius: 6px;
        cursor: pointer;
        font-size: 0.85rem;
        transition: background-color 0.15s;
    }
    .btn-push:hover { background-color: var(--accent-primary-hover); }

    /* ── KPI Row ── */
    .kpi-row-4 {
        display: grid;
        grid-template-columns: repeat(4, 1fr);
        gap: 0.75rem;
        margin-bottom: 1.15rem;
    }
    @media (max-width: 900px) {
        .kpi-row-4 { grid-template-columns: repeat(2, 1fr); }
    }

    .kpi-card {
        background-color: var(--bg-card);
        border: 1px solid var(--border-color);
        border-radius: 6px;
        padding: 0.85rem 1rem;
        position: relative;
        overflow: hidden;
    }
    .kpi-stripe { position: absolute; top: 0; left: 0; right: 0; height: 3px; }
    .kpi-label { font-size: 0.66rem; font-weight: 700; color: var(--text-secondary); letter-spacing: 0.04em; text-transform: uppercase; }
    .kpi-val { font-size: 1.7rem; font-weight: 800; line-height: 1.1; margin: 0.2rem 0; }
    .kpi-sub { font-size: 0.7rem; color: var(--text-secondary); }

    /* ── Uptime bar ── */
    .uptime-bar {
        width: 80px; height: 5px;
        background-color: var(--border-color);
        border-radius: 9999px;
        overflow: hidden;
        display: inline-block;
        vertical-align: middle;
        margin-right: 5px;
    }
    .uptime-bar-fill {
        height: 100%;
        border-radius: 9999px;
        background-color: #10B981;
    }
</style>
@endpush

@section('content')

{{-- Alerts --}}
@if (session('success'))
    <div style="background: rgba(16,185,129,0.12); border: 1px solid rgba(16,185,129,0.3); color: #34D399; padding: 0.75rem 1.1rem; border-radius: 6px; margin-bottom: 1.25rem; font-size: 0.82rem;">
        ✅ {{ session('success') }}
    </div>
@endif
@if (session('error'))
    <div style="background: rgba(239,68,68,0.12); border: 1px solid rgba(239,68,68,0.3); color: #FCA5A5; padding: 0.75rem 1.1rem; border-radius: 6px; margin-bottom: 1.25rem; font-size: 0.82rem;">
        ❌ {{ session('error') }}
    </div>
@endif

{{-- Section header --}}
<div class="section-toolbar">
    <div>
        <h2 class="section-title">💻 Computers &amp; Workstations</h2>
        <p style="font-size: 0.76rem; color: var(--text-secondary); margin-top: 2px;">
            Live fleet view — IP addresses, locations, OS, uptime, last heartbeat, and remote password control.
        </p>
    </div>
    <div style="display: flex; gap: 0.5rem;">
        <span class="pill-badge" style="background: rgba(56,189,248,0.12); color: var(--accent-primary); border: 1px solid rgba(56,189,248,0.25); padding: 0.35rem 0.75rem; font-size: 0.76rem;">
            👤 {{ Auth::user()->email }}
        </span>
    </div>
</div>

{{-- KPI row --}}
@php
    $onlineCount  = $devices->filter(fn($d) => $d->isOnline())->count();
    $offlineCount = $devices->count() - $onlineCount;
    $totalPages   = $devices->sum('print_jobs_sum_total_pages_calculated');
    $totalPrinters = $devices->sum('printers_count');
@endphp

<div class="kpi-row-4">
    <div class="kpi-card">
        <div class="kpi-stripe" style="background:#38BDF8;"></div>
        <div class="kpi-label">TOTAL COMPUTERS</div>
        <div class="kpi-val" style="color: var(--kpi-val1);">{{ $devices->count() }}</div>
        <div class="kpi-sub">Registered workstations</div>
    </div>
    <div class="kpi-card">
        <div class="kpi-stripe" style="background:#10B981;"></div>
        <div class="kpi-label">ONLINE NOW</div>
        <div class="kpi-val" style="color: var(--kpi-val5);">{{ $onlineCount }}</div>
        <div class="kpi-sub">Heartbeat in last 5 min</div>
    </div>
    <div class="kpi-card">
        <div class="kpi-stripe" style="background:#A855F7;"></div>
        <div class="kpi-label">FLEET PRINTERS</div>
        <div class="kpi-val" style="color: var(--kpi-val4);">{{ $totalPrinters }}</div>
        <div class="kpi-sub">Discovered across all PCs</div>
    </div>
    <div class="kpi-card">
        <div class="kpi-stripe" style="background:#F97316;"></div>
        <div class="kpi-label">LIFETIME PAGES</div>
        <div class="kpi-val" style="color: var(--kpi-val6);">{{ number_format($totalPages) }}</div>
        <div class="kpi-sub">Total pages synced to cloud</div>
    </div>
</div>

{{-- Main layout --}}
<div class="device-layout">

    {{-- ── Devices Table ── --}}
    <div class="table-card">
        <div class="table-header">
            <h3 style="font-size: 0.92rem; font-weight: 700; color: var(--text-primary);">
                Fleet Workstations ({{ $devices->count() }})
            </h3>
            <span style="font-size: 0.72rem; color: var(--text-secondary);">
                Auto-refreshes every 10s via heartbeat
            </span>
        </div>

        <div style="overflow-x: auto;">
            <table class="data-table">
                <thead>
                    <tr>
                        <th>Status</th>
                        <th>Computer / Shop</th>
                        <th>IP Address</th>
                        <th>OS / Location</th>
                        <th>User Email</th>
                        <th>Printers</th>
                        <th>Pages Printed</th>
                        <th>App Version</th>
                        <th>Last Heartbeat</th>
                        <th>Uptime / Seen</th>
                    </tr>
                </thead>
                <tbody>
                    @forelse ($devices as $d)
                        @php
                            $isOnline = $d->isOnline();
                            $lastSeen = $d->last_heartbeat_at;

                            // Uptime = time since FIRST created (proxy for how long it's been running)
                            $firstSeen = $d->created_at;
                            $daysSinceInstall = $firstSeen ? now()->diffInDays($firstSeen) : 0;

                            // Minutes offline / online
                            $minutesAgo = $lastSeen ? $lastSeen->diffInMinutes(now()) : null;
                            $hoursAgo   = $lastSeen ? $lastSeen->diffInHours(now()) : null;
                            $daysAgo    = $lastSeen ? $lastSeen->diffInDays(now()) : null;

                            // Uptime percentage (last 24h: online if heartbeat < 5min ago, offline otherwise)
                            // Simple visual: if online show 95%+ bar, if offline go by how long offline
                            $uptimePct = $isOnline ? 98 : max(0, 100 - ($minutesAgo ?? 100) * 2);
                            $uptimePct = min(100, max(0, $uptimePct));

                            // Build uptime display string
                            if ($isOnline) {
                                $uptimeStr = '● Live now';
                            } elseif ($minutesAgo !== null && $minutesAgo < 60) {
                                $uptimeStr = 'Off ' . $minutesAgo . 'm ago';
                            } elseif ($hoursAgo !== null && $hoursAgo < 24) {
                                $uptimeStr = 'Off ' . $hoursAgo . 'h ago';
                            } elseif ($daysAgo !== null) {
                                $uptimeStr = 'Off ' . $daysAgo . 'd ago';
                            } else {
                                $uptimeStr = 'Unknown';
                            }
                        @endphp
                        <tr>
                            {{-- Status --}}
                            <td>
                                @if ($isOnline)
                                    <span class="status-pill status-online">
                                        <span class="status-dot"></span>ONLINE
                                    </span>
                                @else
                                    <span class="status-pill status-offline">
                                        <span class="status-dot"></span>OFFLINE
                                    </span>
                                @endif
                            </td>

                            {{-- Computer / Shop --}}
                            <td>
                                <strong style="color: var(--accent-primary); font-size: 0.85rem;">
                                    {{ $d->computer_name }}
                                </strong>
                                <div class="device-meta">🏪 {{ $d->shop_name }}</div>
                                <div class="device-meta" style="font-size: 0.68rem; color: var(--text-muted);">
                                    ID: {{ substr($d->device_id, 0, 12) }}…
                                </div>
                            </td>

                            {{-- IP Address --}}
                            <td>
                                @if (!empty($d->ip_address))
                                    <span class="pill-badge" style="background: rgba(56,189,248,0.12); color: var(--accent-primary); border: 1px solid rgba(56,189,248,0.2); font-family: monospace; font-size: 0.75rem; letter-spacing: 0.02em;">
                                        {{ $d->ip_address }}
                                    </span>
                                @else
                                    <span style="color: var(--text-muted); font-size: 0.72rem;">Not recorded</span>
                                @endif
                                @if (!empty($d->mac_address))
                                    <div class="device-meta" style="font-size: 0.66rem; font-family: monospace;">{{ $d->mac_address }}</div>
                                @endif
                            </td>

                            {{-- OS / Location --}}
                            <td>
                                @if (!empty($d->os_version))
                                    <span style="font-size: 0.78rem; color: var(--text-primary);">
                                        {{ Str::limit($d->os_version, 28) }}
                                    </span>
                                @else
                                    <span style="color: var(--text-muted); font-size: 0.72rem;">Windows</span>
                                @endif
                                <div class="device-meta">
                                    📍 {{ $d->shop_name ?: 'Main Branch' }}
                                </div>
                            </td>

                            {{-- User Email --}}
                            <td>
                                <span style="font-size: 0.78rem; color: var(--text-primary);">{{ $d->user_email }}</span>
                            </td>

                            {{-- Printers --}}
                            <td style="text-align: center;">
                                <span class="pill-badge" style="background: rgba(168,85,247,0.12); color: #A855F7; border: 1px solid rgba(168,85,247,0.2);">
                                    🖨️ {{ $d->printers_count }}
                                </span>
                            </td>

                            {{-- Pages --}}
                            <td>
                                <strong style="color: var(--kpi-val6); font-size: 0.92rem;">
                                    {{ number_format($d->print_jobs_sum_total_pages_calculated ?: 0) }}
                                </strong>
                                <div class="device-meta">pg total</div>
                            </td>

                            {{-- App Version --}}
                            <td>
                                <span class="pill-badge" style="background: rgba(16,185,129,0.12); color: #10B981; border: 1px solid rgba(16,185,129,0.2); font-size: 0.7rem;">
                                    v{{ $d->app_version ?? '2.0.0' }}
                                </span>
                            </td>

                            {{-- Last Heartbeat --}}
                            <td style="white-space: nowrap;">
                                @if ($lastSeen)
                                    <span style="font-size: 0.78rem; color: {{ $isOnline ? 'var(--accent-primary)' : 'var(--text-secondary)' }};">
                                        {{ $lastSeen->format('M d, H:i') }}
                                    </span>
                                    <div class="device-meta">{{ $lastSeen->diffForHumans() }}</div>
                                @else
                                    <span style="color: var(--text-muted); font-size: 0.72rem;">Never connected</span>
                                @endif
                            </td>

                            {{-- Uptime / Seen ── --}}
                            <td>
                                <div style="display: flex; align-items: center; margin-bottom: 3px;">
                                    <div class="uptime-bar">
                                        <div class="uptime-bar-fill" style="width: {{ $uptimePct }}%; background-color: {{ $isOnline ? '#10B981' : '#64748B' }};"></div>
                                    </div>
                                    <span style="font-size: 0.68rem; color: {{ $isOnline ? '#10B981' : '#64748B' }}; font-weight: 700;">{{ $uptimePct }}%</span>
                                </div>
                                <div class="device-meta" style="font-size: 0.69rem;">
                                    {{ $uptimeStr }}
                                </div>
                                @if ($firstSeen)
                                    <div class="device-meta" style="font-size: 0.66rem; color: var(--text-muted);">
                                        Installed {{ $firstSeen->diffForHumans() }}
                                    </div>
                                @endif
                            </td>
                        </tr>
                    @empty
                        <tr>
                            <td colspan="10" style="text-align: center; padding: 3rem; color: var(--text-secondary);">
                                <div style="font-size: 2rem; margin-bottom: 0.5rem;">💻</div>
                                No workstations connected yet.<br>
                                <span style="font-size: 0.78rem;">Install the Nexrein Printer Monitor desktop agent on a Windows PC to see it appear here automatically.</span>
                            </td>
                        </tr>
                    @endforelse
                </tbody>
            </table>
        </div>
    </div>

    {{-- ── Password Push Card ── --}}
    <div>
        <div class="password-card">
            <div style="display: flex; align-items: center; gap: 0.65rem; margin-bottom: 1rem;">
                <div style="width: 34px; height: 34px; background: #38BDF8; border-radius: 7px; display: flex; align-items: center; justify-content: center; font-size: 1rem; color: #0F172A;">🔑</div>
                <div>
                    <h3 style="font-size: 0.92rem; font-weight: 700; color: var(--text-primary);">Push Desktop Password</h3>
                    <p style="font-size: 0.7rem; color: var(--text-secondary);">Updates apply within 10 seconds</p>
                </div>
            </div>

            @if (session('password_success'))
                <div style="background: rgba(16,185,129,0.12); border: 1px solid rgba(16,185,129,0.3); color: #34D399; padding: 0.65rem; border-radius: 5px; font-size: 0.74rem; margin-bottom: 0.85rem;">
                    ✅ {{ session('password_success') }}
                </div>
            @endif

            <form action="{{ route('devices.update_software_password') }}" method="POST">
                @csrf

                <label style="display: block; font-size: 0.68rem; font-weight: 700; text-transform: uppercase; color: var(--text-secondary); margin-bottom: 0.35rem;">Target Workstation</label>
                <select name="device_id" class="form-control-theme">
                    <option value="all">🌐 ALL Connected Computers</option>
                    @foreach ($devices as $d)
                        <option value="{{ $d->device_id }}">
                            {{ $d->shop_name }} – {{ $d->computer_name }}
                            {{ $d->isOnline() ? '● LIVE' : '' }}
                        </option>
                    @endforeach
                </select>

                <label style="display: block; font-size: 0.68rem; font-weight: 700; text-transform: uppercase; color: var(--text-secondary); margin-bottom: 0.35rem;">New Desktop Admin Password</label>
                <input type="password" name="new_password" class="form-control-theme"
                       placeholder="Enter new password" required minlength="4">

                <button type="submit" class="btn-push">
                    🚀 Push &amp; Update Password
                </button>
            </form>

            <div style="margin-top: 1rem; padding-top: 0.85rem; border-top: 1px solid var(--border-color);">
                <div style="font-size: 0.7rem; color: var(--text-secondary); line-height: 1.55;">
                    <strong style="color: var(--text-primary);">How it works:</strong><br>
                    1. Enter new password here<br>
                    2. Desktop agent receives it on next heartbeat (&lt;10s)<br>
                    3. Software password updated automatically<br>
                    4. No manual intervention required ✅
                </div>
            </div>
        </div>

        {{-- Download card in side panel --}}
        <div class="password-card" style="margin-top: 1rem; border-color: rgba(56,189,248,0.25);">
            <div style="font-size: 0.8rem; font-weight: 700; color: var(--text-primary); margin-bottom: 0.65rem;">
                📥 Need to add a new workstation?
            </div>
            <p style="font-size: 0.72rem; color: var(--text-secondary); margin-bottom: 0.85rem; line-height: 1.5;">
                Download and install the desktop agent on any Windows 10/11 PC. It registers automatically under your account.
            </p>
            <a href="https://github.com/nexreindigital-web/nexrein-print-monitor/releases/latest/download/PrintMonitor-Setup.exe"
               style="display: flex; align-items: center; justify-content: center; gap: 0.45rem; background: rgba(56,189,248,0.12); border: 1px solid rgba(56,189,248,0.3); color: var(--accent-primary); padding: 0.6rem; border-radius: 6px; font-size: 0.8rem; font-weight: 700; text-decoration: none; transition: background 0.2s;">
                ⬇ Download PrintMonitor-Setup.exe
            </a>
        </div>
    </div>

</div>
@endsection
