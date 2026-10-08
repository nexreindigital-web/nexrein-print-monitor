@extends('layouts.portal')

@section('title', 'Computers & Shops Management')

@push('styles')
<style>
    .device-grid {
        display: grid;
        grid-template-columns: 1fr 360px;
        gap: 1.5rem;
        align-items: start;
    }
    @media (max-width: 1024px) {
        .device-grid { grid-template-columns: 1fr; }
    }

    .table-card {
        background: var(--bg-card);
        border: 1px solid var(--border-color);
        border-radius: 14px;
        overflow: hidden;
    }

    .table-header {
        padding: 1.25rem 1.5rem;
        border-bottom: 1px solid var(--border-color);
        display: flex;
        align-items: center;
        justify-content: space-between;
    }

    .password-card {
        background: var(--bg-card);
        border: 1px solid #4F46E5;
        border-radius: 14px;
        padding: 1.75rem;
        box-shadow: 0 10px 25px rgba(79, 70, 229, 0.15);
    }

    .status-dot {
        width: 10px;
        height: 10px;
        border-radius: 50%;
        display: inline-block;
        margin-right: 6px;
    }
    .status-dot.online { background-color: var(--accent-emerald); box-shadow: 0 0 8px var(--accent-emerald); }
    .status-dot.offline { background-color: var(--text-muted); }
</style>
@endpush

@section('content')
<div style="margin-bottom: 1.5rem;">
    <h1 style="font-size: 1.75rem; font-weight: 800; letter-spacing: -0.02em;">Connected Computers &amp; Shop Terminals</h1>
    <p style="color: var(--text-secondary); font-size: 0.9rem;">
        Monitor workstation connectivity, shop identities, and centrally manage desktop software security passwords.
    </p>
</div>

<div class="device-grid">
    <!-- Devices List -->
    <div class="table-card">
        <div class="table-header">
            <h2 style="font-size: 1.15rem; font-weight: 700;">💻 Registered Workstations ({{ $devices->count() }})</h2>
        </div>

        <div style="overflow-x: auto;">
            <table>
                <thead>
                    <tr>
                        <th>Status</th>
                        <th>Shop / Branch</th>
                        <th>Computer Name</th>
                        <th>User Email</th>
                        <th>Total Pages</th>
                        <th>Printers</th>
                        <th>Last Heartbeat</th>
                        <th>Software Password</th>
                    </tr>
                </thead>
                <tbody>
                    @forelse ($devices as $d)
                        <tr>
                            <td>
                                @if ($d->isOnline())
                                    <span class="badge" style="background: rgba(16, 185, 129, 0.15); color: #34D399; border: 1px solid rgba(16, 185, 129, 0.3);">
                                        <span class="status-dot online"></span> Online
                                    </span>
                                @else
                                    <span class="badge" style="background: rgba(107, 114, 128, 0.15); color: #9CA3AF; border: 1px solid rgba(107, 114, 128, 0.3);">
                                        <span class="status-dot offline"></span> Offline
                                    </span>
                                @endif
                            </td>
                            <td>
                                <strong style="color: white;">{{ $d->shop_name }}</strong>
                            </td>
                            <td style="color: #93C5FD;">{{ $d->computer_name }}</td>
                            <td>{{ $d->user_email }}</td>
                            <td>
                                <strong style="color: #38BDF8;">{{ number_format($d->print_jobs_sum_total_pages_calculated ?: 0) }}</strong>
                            </td>
                            <td>{{ $d->printers_count }}</td>
                            <td style="color: var(--text-secondary); font-size: 0.8rem;">
                                {{ $d->last_heartbeat_at ? $d->last_heartbeat_at->diffForHumans() : 'Never' }}
                            </td>
                            <td>
                                @if ($d->pending_password_update)
                                    <span class="badge" style="background: rgba(245, 158, 11, 0.15); color: #FBBF24; border: 1px solid rgba(245, 158, 11, 0.3);">
                                        ⏳ Syncing Update...
                                    </span>
                                @elseif ($d->software_password)
                                    <span class="badge" style="background: rgba(59, 130, 246, 0.15); color: #93C5FD; border: 1px solid rgba(59, 130, 246, 0.3);">
                                        🔒 Custom (Active)
                                    </span>
                                @else
                                    <span class="badge" style="background: rgba(107, 114, 128, 0.15); color: #D1D5DB; border: 1px solid rgba(107, 114, 128, 0.3);">
                                        Default
                                    </span>
                                @endif
                            </td>
                        </tr>
                    @empty
                        <tr>
                            <td colspan="8" style="text-align: center; padding: 3rem; color: var(--text-secondary);">
                                📭 No desktop computers connected yet. Run the installer on a workstation to automatically register it.
                            </td>
                        </tr>
                    @endforelse
                </tbody>
            </table>
        </div>
    </div>

    <!-- Centralized Desktop Password Manager Card -->
    <div class="password-card">
        <div style="display: flex; align-items: center; gap: 0.75rem; margin-bottom: 1.25rem;">
            <div style="font-size: 1.75rem;">🔑</div>
            <div>
                <h3 style="font-size: 1.15rem; font-weight: 700;">Remote Software Password</h3>
                <p style="font-size: 0.8rem; color: var(--text-secondary);">Centrally update desktop super admin credentials.</p>
            </div>
        </div>

        <p style="font-size: 0.825rem; color: #C7D2FE; margin-bottom: 1.25rem; line-height: 1.45;">
            Changing the password here pushes the update directly to the selected Windows desktop agent on its next 3-second heartbeat, updating its local admin password automatically!
        </p>

        <form action="{{ route('devices.update_software_password') }}" method="POST">
            @csrf

            <div style="margin-bottom: 1rem;">
                <label style="display: block; font-size: 0.75rem; font-weight: 700; text-transform: uppercase; color: var(--text-secondary); margin-bottom: 0.35rem;">
                    Target Computer or Shop
                </label>
                <select name="device_id" style="width: 100%; background: var(--bg-input); border: 1px solid var(--border-light); color: white; padding: 0.65rem; border-radius: 8px; font-size: 0.85rem;" required>
                    <option value="all">🌐 ALL Connected Computers</option>
                    @foreach ($devices as $d)
                        <option value="{{ $d->device_id }}">{{ $d->shop_name }} ({{ $d->computer_name }})</option>
                    @endforeach
                </select>
            </div>

            <div style="margin-bottom: 1.5rem;">
                <label style="display: block; font-size: 0.75rem; font-weight: 700; text-transform: uppercase; color: var(--text-secondary); margin-bottom: 0.35rem;">
                    New Desktop Admin Password
                </label>
                <input type="password" name="new_password" placeholder="Enter new password" style="width: 100%; background: var(--bg-input); border: 1px solid var(--border-light); color: white; padding: 0.65rem; border-radius: 8px; font-size: 0.9rem;" required minlength="4">
            </div>

            <button type="submit" class="btn btn-primary" style="width: 100%; padding: 0.8rem;">
                🚀 Push &amp; Update Desktop Password
            </button>
        </form>
    </div>
</div>
@endsection
