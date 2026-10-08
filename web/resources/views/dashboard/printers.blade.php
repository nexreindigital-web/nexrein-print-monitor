@extends('layouts.portal')

@section('title', 'Fleet Printers')

@section('content')
<div style="margin-bottom: 1.5rem;">
    <h1 style="font-size: 1.75rem; font-weight: 800; letter-spacing: -0.02em;">Installed Printers Fleet</h1>
    <p style="color: var(--text-secondary); font-size: 0.9rem;">
        Complete inventory of physical, network, and virtual printers active across all registered computers.
    </p>
</div>

<div class="table-card">
    <div class="table-header">
        <h2 style="font-size: 1.15rem; font-weight: 700;">🖨️ Printer Fleet ({{ $printers->count() }})</h2>
    </div>

    <div style="overflow-x: auto;">
        <table>
            <thead>
                <tr>
                    <th>Printer Name</th>
                    <th>Host Workstation</th>
                    <th>Driver Name</th>
                    <th>Port</th>
                    <th>Type</th>
                    <th>Status</th>
                    <th>Total Jobs</th>
                    <th>Last Active</th>
                </tr>
            </thead>
            <tbody>
                @forelse ($printers as $p)
                    <tr>
                        <td>
                            <strong style="color: white; font-size: 0.95rem;">🖨️ {{ $p->name }}</strong>
                            @if ($p->is_default)
                                <span class="badge" style="background: rgba(59, 130, 246, 0.2); color: #60A5FA; font-size: 0.7rem; margin-left: 0.35rem;">Default</span>
                            @endif
                        </td>
                        <td>
                            <span class="badge badge-shop">{{ $p->device ? $p->device->shop_name : 'Workstation' }}</span>
                            <span style="font-size: 0.8rem; color: var(--text-muted); display: block;">{{ $p->device ? $p->device->computer_name : $p->device_id }}</span>
                        </td>
                        <td style="color: #CBD5E1;">{{ $p->driver_name ?: 'System Driver' }}</td>
                        <td style="color: var(--text-muted);">{{ $p->port_name ?: 'Local Port' }}</td>
                        <td>
                            @if ($p->is_network)
                                <span class="badge" style="background: rgba(16, 185, 129, 0.15); color: #34D399;">🌐 Network</span>
                            @else
                                <span class="badge" style="background: rgba(107, 114, 128, 0.15); color: #CBD5E1;">🔌 Local USB</span>
                            @endif
                        </td>
                        <td>
                            <span class="badge" style="background: rgba(16, 185, 129, 0.15); color: #6EE7B7;">{{ $p->status }}</span>
                        </td>
                        <td>
                            <strong style="color: #38BDF8;">{{ number_format($p->job_count) }}</strong>
                        </td>
                        <td style="color: var(--text-secondary); font-size: 0.8rem;">
                            {{ $p->last_seen_at ? $p->last_seen_at->diffForHumans() : 'Active' }}
                        </td>
                    </tr>
                @empty
                    <tr>
                        <td colspan="8" style="text-align: center; padding: 3rem; color: var(--text-secondary);">
                            📭 No printers reported yet.
                        </td>
                    </tr>
                @endforelse
            </tbody>
        </table>
    </div>
</div>
@endsection
