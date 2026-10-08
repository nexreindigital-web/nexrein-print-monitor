@extends('layouts.portal')

@section('title', 'Computers & Shops')

@push('styles')
<style>
    .device-grid {
        display: grid;
        grid-template-columns: 1fr 340px;
        gap: 1.25rem;
        align-items: start;
    }
    @media (max-width: 1040px) {
        .device-grid { grid-template-columns: 1fr; }
    }

    .table-card {
        background-color: var(--bg-card);
        border: 1px solid var(--border-color);
        border-radius: 6px;
        padding: 1.15rem 1.25rem;
    }

    .table-header {
        display: flex;
        align-items: center;
        justify-content: space-between;
        margin-bottom: 1rem;
    }

    .password-card {
        background-color: var(--bg-card);
        border: 1px solid var(--border-color);
        border-radius: 6px;
        padding: 1.25rem;
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

    .status-dot {
        width: 7px;
        height: 7px;
        border-radius: 50%;
        display: inline-block;
        margin-right: 5px;
    }
    .status-dot.online { background-color: #10B981; }
    .status-dot.offline { background-color: #64748B; }

    .form-control-theme {
        width: 100%;
        background-color: var(--bg-input);
        border: 1px solid var(--border-color);
        color: var(--text-primary);
        padding: 0.6rem 0.8rem;
        border-radius: 6px;
        font-size: 0.85rem;
        outline: none;
    }
    .form-control-theme:focus {
        border-color: #38BDF8;
    }

    .btn-push {
        width: 100%;
        background-color: #38BDF8;
        color: #0F172A;
        font-weight: 700;
        border: none;
        padding: 0.7rem;
        border-radius: 6px;
        cursor: pointer;
        transition: background-color 0.15s;
    }
    .btn-push:hover {
        background-color: #0EA5E9;
    }
</style>
@endpush

@section('content')
<div style="margin-bottom: 1.25rem;">
    <h1 style="font-size: 1.25rem; font-weight: 700; color: var(--text-primary);">Connected Computers &amp; Shop Stations</h1>
    <p style="color: var(--text-secondary); font-size: 0.78rem; margin-top: 2px;">
        Monitor real-time workstation heartbeats and sync passwords across your fleet.
    </p>
</div>

<div class="device-grid">
    <!-- Devices List -->
    <div class="table-card">
        <div class="table-header">
            <h2 style="font-size: 0.95rem; font-weight: 700; color: var(--text-primary);">
                Registered Workstations ({{ $devices->count() }})
            </h2>
        </div>

        <div style="overflow-x: auto;">
            <table class="data-table">
                <thead>
                    <tr>
                        <th>STATUS</th>
                        <th>SHOP / BRANCH</th>
                        <th>COMPUTER</th>
                        <th>USER EMAIL</th>
                        <th>PAGES</th>
                        <th>PRINTERS</th>
                        <th>LAST HEARTBEAT</th>
                    </tr>
                </thead>
                <tbody>
                    @forelse ($devices as $d)
                        <tr>
                            <td>
                                @if ($d->isOnline())
                                    <span style="display: inline-flex; align-items: center; background-color: var(--badge-completed-bg); border: 1px solid var(--badge-completed-border); color: var(--badge-completed-fg); border-radius: 9999px; padding: 2px 8px; font-size: 0.68rem; font-weight: 700;">
                                        <span class="status-dot online"></span> ONLINE
                                    </span>
                                @else
                                    <span style="display: inline-flex; align-items: center; background-color: rgba(100, 116, 139, 0.15); border: 1px solid #64748B; color: #94A3B8; border-radius: 9999px; padding: 2px 8px; font-size: 0.68rem; font-weight: 700;">
                                        <span class="status-dot offline"></span> OFFLINE
                                    </span>
                                @endif
                            </td>
                            <td>
                                <strong>{{ $d->shop_name }}</strong>
                            </td>
                            <td style="color: var(--accent-primary); font-weight: 600;">{{ $d->computer_name }}</td>
                            <td>{{ $d->user_email }}</td>
                            <td style="font-weight: 700;">{{ number_format($d->print_jobs_sum_total_pages_calculated ?: 0) }}</td>
                            <td>{{ $d->printers_count }}</td>
                            <td style="color: var(--text-secondary); font-size: 0.74rem;">
                                {{ $d->last_heartbeat_at ? $d->last_heartbeat_at->diffForHumans() : 'Never' }}
                            </td>
                        </tr>
                    @empty
                        <tr>
                            <td colspan="7" style="text-align: center; padding: 2.5rem; color: var(--text-secondary);">
                                No desktop computers connected yet. Run the installer on a workstation to automatically link it here.
                            </td>
                        </tr>
                    @endforelse
                </tbody>
            </table>
        </div>
    </div>

    <!-- Centralized Desktop Password Manager Card -->
    <div class="password-card">
        <div style="display: flex; align-items: center; gap: 0.65rem; margin-bottom: 0.85rem;">
            <div style="width: 32px; height: 32px; background-color: #38BDF8; border-radius: 6px; display: flex; align-items: center; justify-content: center; color: #0F172A; font-weight: bold;">
                🔑
            </div>
            <div>
                <h3 style="font-size: 0.95rem; font-weight: 700; color: var(--text-primary);">Push Software Password</h3>
                <p style="font-size: 0.72rem; color: var(--text-secondary);">Update desktop admin password remotely.</p>
            </div>
        </div>

        <p style="font-size: 0.75rem; color: var(--text-secondary); margin-bottom: 1rem; line-height: 1.45;">
            Updates stage immediately and are applied by each desktop agent within 10 seconds via background heartbeat.
        </p>

        <form action="{{ route('devices.update_software_password') }}" method="POST">
            @csrf

            <div style="margin-bottom: 0.85rem;">
                <label style="display: block; font-size: 0.7rem; font-weight: 700; text-transform: uppercase; color: var(--text-secondary); margin-bottom: 0.35rem;">
                    Target Workstation
                </label>
                <select name="device_id" class="form-control-theme" required>
                    <option value="all">🌐 ALL Connected Computers</option>
                    @foreach ($devices as $d)
                        <option value="{{ $d->device_id }}">{{ $d->shop_name }} ({{ $d->computer_name }})</option>
                    @endforeach
                </select>
            </div>

            <div style="margin-bottom: 1.15rem;">
                <label style="display: block; font-size: 0.7rem; font-weight: 700; text-transform: uppercase; color: var(--text-secondary); margin-bottom: 0.35rem;">
                    New Desktop Admin Password
                </label>
                <input type="password" name="new_password" class="form-control-theme" placeholder="Enter new password" required minlength="4">
            </div>

            <button type="submit" class="btn-push">
                Push &amp; Update Desktop Password
            </button>
        </form>
    </div>
</div>
@endsection
