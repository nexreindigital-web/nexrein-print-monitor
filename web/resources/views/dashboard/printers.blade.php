@extends('layouts.portal')

@section('title', 'Manage Printers')

@section('content')
<div style="margin-bottom: 1.5rem; display: flex; justify-content: space-between; align-items: flex-start; flex-wrap: wrap; gap: 1rem;">
    <div>
        <h1 style="font-size: 1.75rem; font-weight: 800; letter-spacing: -0.02em;">🖨️ Printer Fleet Management</h1>
        <p style="color: var(--text-secondary); font-size: 0.9rem;">
            Configure friendly printer aliases, physical locations, cost-per-page rates, and track printer activity across your workstations.
        </p>
    </div>
    <div style="display: flex; gap: 0.5rem;">
        <span class="badge" style="background: rgba(99, 102, 241, 0.15); color: #818CF8; font-size: 0.85rem; padding: 0.5rem 0.85rem;">
            👤 Account: {{ Auth::user()->email }}
        </span>
    </div>
</div>

<!-- Fleet KPI Summary -->
<div class="metrics-grid" style="grid-template-columns: repeat(auto-fit, minmax(200px, 1fr)); margin-bottom: 1.5rem;">
    <div class="metric-card">
        <div class="metric-title">TOTAL REPORTED PRINTERS</div>
        <div class="metric-value">{{ $printers->count() }}</div>
        <div class="metric-subtext">All workstations in your fleet</div>
    </div>
    <div class="metric-card">
        <div class="metric-title">ACTIVE TRACKED</div>
        <div class="metric-value" style="color: #34D399;">{{ $printers->where('is_active', true)->count() }}</div>
        <div class="metric-subtext">Ready to monitor and bill</div>
    </div>
    <div class="metric-card">
        <div class="metric-title">NETWORK PRINTERS</div>
        <div class="metric-value" style="color: #38BDF8;">{{ $printers->where('is_network', true)->count() }}</div>
        <div class="metric-subtext">Shared on local network</div>
    </div>
    <div class="metric-card">
        <div class="metric-title">LIFETIME PRINT JOBS</div>
        <div class="metric-value" style="color: #FBBF24;">{{ number_format($printers->sum('job_count')) }}</div>
        <div class="metric-subtext">Processed through agent spooler</div>
    </div>
</div>

<div class="table-card">
    <div class="table-header">
        <h2 style="font-size: 1.15rem; font-weight: 700;">Printers List ({{ $printers->count() }})</h2>
    </div>

    <div style="overflow-x: auto;">
        <table>
            <thead>
                <tr>
                    <th>Printer / Alias</th>
                    <th>Host Machine</th>
                    <th>Type &amp; Port</th>
                    <th>Status</th>
                    <th>Pricing (B&amp;W / Color)</th>
                    <th>Total Jobs</th>
                    <th>Last Active</th>
                    <th style="text-align: right;">Action</th>
                </tr>
            </thead>
            <tbody>
                @forelse ($printers as $p)
                    <tr>
                        <td>
                            <div>
                                <strong style="color: white; font-size: 0.95rem;">
                                    {{ $p->alias_name ?: $p->name }}
                                </strong>
                                @if ($p->alias_name)
                                    <span style="font-size: 0.78rem; color: var(--text-muted); display: block;">System: {{ $p->name }}</span>
                                @endif
                                @if (!empty($p->location))
                                    <span style="font-size: 0.78rem; color: #818CF8; display: block;">📍 {{ $p->location }}</span>
                                @endif
                                @if ($p->is_default)
                                    <span class="badge" style="background: rgba(59, 130, 246, 0.2); color: #60A5FA; font-size: 0.7rem;">Default</span>
                                @endif
                            </div>
                        </td>
                        <td>
                            <span class="badge badge-shop">{{ $p->device ? $p->device->shop_name : 'Shop' }}</span>
                            <span style="font-size: 0.8rem; color: var(--text-muted); display: block;">{{ $p->device ? $p->device->computer_name : $p->device_id }}</span>
                        </td>
                        <td>
                            @if ($p->is_network)
                                <span class="badge" style="background: rgba(16, 185, 129, 0.15); color: #34D399;">🌐 Network</span>
                            @else
                                <span class="badge" style="background: rgba(107, 114, 128, 0.15); color: #CBD5E1;">🔌 Local USB</span>
                            @endif
                            <span style="font-size: 0.75rem; color: var(--text-muted); display: block;">Port: {{ $p->port_name ?: 'N/A' }}</span>
                        </td>
                        <td>
                            @if ($p->is_active)
                                <span class="badge" style="background: rgba(16, 185, 129, 0.15); color: #6EE7B7;">● Active</span>
                            @else
                                <span class="badge" style="background: rgba(239, 68, 68, 0.15); color: #FCA5A5;">○ Disabled</span>
                            @endif
                        </td>
                        <td>
                            <div style="font-size: 0.85rem;">
                                <span style="color: #CBD5E1;">B&amp;W: <strong>KES {{ number_format($p->cost_per_mono_page ?? 0, 2) }}</strong></span>
                                <br>
                                <span style="color: #E879F9;">Color: <strong>KES {{ number_format($p->cost_per_color_page ?? 0, 2) }}</strong></span>
                            </div>
                        </td>
                        <td>
                            <strong style="color: #38BDF8;">{{ number_format($p->job_count) }}</strong>
                        </td>
                        <td style="color: var(--text-secondary); font-size: 0.8rem;">
                            {{ $p->last_seen_at ? $p->last_seen_at->diffForHumans() : 'Active' }}
                        </td>
                        <td style="text-align: right;">
                            <button onclick="openEditModal({{ json_encode($p) }})" class="btn btn-secondary" style="padding: 0.4rem 0.75rem; font-size: 0.8rem;">
                                ⚙️ Edit / Rates
                            </button>
                        </td>
                    </tr>
                @empty
                    <tr>
                        <td colspan="8" style="text-align: center; padding: 3rem; color: var(--text-secondary);">
                            📭 No printers detected yet. Run the Nexrein Printer Monitor desktop agent on your workstation to automatically discover installed printers.
                        </td>
                    </tr>
                @endforelse
            </tbody>
        </table>
    </div>
</div>

<!-- Modal Dialog for Printer Management -->
<div id="printerModal" style="display: none; position: fixed; inset: 0; background: rgba(0, 0, 0, 0.75); z-index: 9999; justify-content: center; align-items: center; padding: 1rem;">
    <div style="background: var(--bg-card); border: 1px solid var(--border-color); border-radius: 12px; width: 100%; max-width: 520px; padding: 1.75rem; box-shadow: 0 25px 50px -12px rgba(0,0,0,0.5);">
        <div style="display: flex; justify-content: space-between; align-items: center; margin-bottom: 1.25rem;">
            <h3 style="font-size: 1.2rem; font-weight: 700;" id="modalTitle">⚙️ Configure Printer</h3>
            <button onclick="closeEditModal()" style="background: none; border: none; color: var(--text-secondary); font-size: 1.25rem; cursor: pointer;">✕</button>
        </div>

        <form id="printerForm" method="POST" action="">
            @csrf
            <div style="margin-bottom: 1rem;">
                <label style="display: block; font-size: 0.85rem; font-weight: 600; margin-bottom: 0.35rem;">Friendly Alias / Display Name</label>
                <input type="text" name="alias_name" id="modalAlias" class="form-control" placeholder="e.g. Front Cashier Desk HP LaserJet" style="width: 100%; padding: 0.6rem; border-radius: 6px; background: var(--bg-main); border: 1px solid var(--border-color); color: white;">
            </div>

            <div style="margin-bottom: 1rem;">
                <label style="display: block; font-size: 0.85rem; font-weight: 600; margin-bottom: 0.35rem;">Physical Location / Station Notes</label>
                <input type="text" name="location" id="modalLocation" class="form-control" placeholder="e.g. Counter 1 next to POS register" style="width: 100%; padding: 0.6rem; border-radius: 6px; background: var(--bg-main); border: 1px solid var(--border-color); color: white;">
            </div>

            <div style="display: grid; grid-template-columns: 1fr 1fr; gap: 1rem; margin-bottom: 1rem;">
                <div>
                    <label style="display: block; font-size: 0.85rem; font-weight: 600; margin-bottom: 0.35rem;">B&amp;W Rate (Per Page)</label>
                    <input type="number" step="0.01" name="cost_per_mono_page" id="modalMonoCost" class="form-control" placeholder="5.00" style="width: 100%; padding: 0.6rem; border-radius: 6px; background: var(--bg-main); border: 1px solid var(--border-color); color: white;">
                </div>
                <div>
                    <label style="display: block; font-size: 0.85rem; font-weight: 600; margin-bottom: 0.35rem;">Color Rate (Per Page)</label>
                    <input type="number" step="0.01" name="cost_per_color_page" id="modalColorCost" class="form-control" placeholder="20.00" style="width: 100%; padding: 0.6rem; border-radius: 6px; background: var(--bg-main); border: 1px solid var(--border-color); color: white;">
                </div>
            </div>

            <div style="margin-bottom: 1.25rem;">
                <label style="display: flex; align-items: center; gap: 0.5rem; font-size: 0.9rem; cursor: pointer;">
                    <input type="checkbox" name="is_active" id="modalIsActive" value="1" style="width: 18px; height: 18px;">
                    <span>Enable Active Monitoring &amp; Accounting for this printer</span>
                </label>
            </div>

            <div style="display: flex; justify-content: flex-end; gap: 0.75rem;">
                <button type="button" onclick="closeEditModal()" class="btn btn-secondary" style="padding: 0.6rem 1.25rem;">Cancel</button>
                <button type="submit" class="btn btn-primary" style="padding: 0.6rem 1.25rem;">💾 Save Printer Settings</button>
            </div>
        </form>
    </div>
</div>

<script>
function openEditModal(printer) {
    document.getElementById('modalTitle').innerText = '⚙️ Configure ' + printer.name;
    document.getElementById('modalAlias').value = printer.alias_name || '';
    document.getElementById('modalLocation').value = printer.location || '';
    document.getElementById('modalMonoCost').value = printer.cost_per_mono_page || '0.00';
    document.getElementById('modalColorCost').value = printer.cost_per_color_page || '0.00';
    document.getElementById('modalIsActive').checked = printer.is_active !== false;
    document.getElementById('printerForm').action = '/printers/' + printer.id + '/update';
    
    var modal = document.getElementById('printerModal');
    modal.style.display = 'flex';
}

function closeEditModal() {
    var modal = document.getElementById('printerModal');
    modal.style.display = 'none';
}
</script>
@endsection
