@extends('layouts.portal')

@section('title', 'Manage Printers')

@push('styles')
<style>
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

    .kpi-row-4 {
        display: grid;
        grid-template-columns: repeat(4, 1fr);
        gap: 0.75rem;
        margin-bottom: 1.15rem;
    }
    @media (max-width: 900px) {
        .kpi-row-4 { grid-template-columns: repeat(2, 1fr); }
    }
    @media (max-width: 500px) {
        .kpi-row-4 { grid-template-columns: 1fr; }
    }

    .kpi-card {
        background-color: var(--bg-card);
        border: 1px solid var(--border-color);
        border-radius: 6px;
        padding: 0.85rem 1rem;
        position: relative;
        overflow: hidden;
        min-height: 95px;
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

    .table-card {
        background-color: var(--bg-card);
        border: 1px solid var(--border-color);
        border-radius: 6px;
        padding: 1.15rem 1.25rem;
        margin-bottom: 1.5rem;
    }

    .table-header {
        display: flex;
        align-items: center;
        justify-content: space-between;
        margin-bottom: 1rem;
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
        padding: 0.75rem 0.85rem;
        border-bottom: 1px solid var(--border-color);
        color: var(--text-primary);
        vertical-align: middle;
    }

    .data-table tr:nth-child(even) td {
        background-color: var(--table-row-alt);
    }

    .pill-badge {
        display: inline-flex;
        align-items: center;
        gap: 0.25rem;
        font-size: 0.72rem;
        font-weight: 700;
        padding: 0.2rem 0.55rem;
        border-radius: 4px;
        line-height: 1;
    }

    .pill-network {
        background: rgba(16, 185, 129, 0.15);
        color: #10B981;
        border: 1px solid rgba(16, 185, 129, 0.3);
    }

    .pill-usb {
        background: rgba(148, 163, 184, 0.15);
        color: var(--text-secondary);
        border: 1px solid var(--border-color);
    }

    .pill-active {
        background: rgba(16, 185, 129, 0.15);
        color: #10B981;
    }

    .pill-disabled {
        background: rgba(239, 68, 68, 0.15);
        color: #EF4444;
    }

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
        border-color: var(--accent-primary);
    }
</style>
@endpush

@section('content')
<div class="section-toolbar">
    <div>
        <h2 class="section-title">🖨️ Printer Fleet Management</h2>
        <p style="font-size: 0.78rem; color: var(--text-secondary); margin-top: 2px;">
            Configure friendly printer aliases, physical stations, cost-per-page rates, and track printer activity across workstations.
        </p>
    </div>
    <div style="display: flex; gap: 0.5rem; align-items: center;">
        <span class="pill-badge" style="background: rgba(56, 189, 248, 0.12); color: var(--accent-primary); border: 1px solid rgba(56, 189, 248, 0.25); padding: 0.35rem 0.75rem;">
            👤 {{ Auth::user()->email }}
        </span>
    </div>
</div>

<!-- Fleet KPI Summary Row -->
<div class="kpi-row-4">
    <div class="kpi-card">
        <div class="kpi-stripe" style="background-color: #38BDF8;"></div>
        <div class="kpi-label">TOTAL REPORTED PRINTERS</div>
        <div class="kpi-val" style="color: var(--kpi-val1);">{{ $printers->count() }}</div>
        <div class="kpi-sub">Across all linked workstations</div>
    </div>
    <div class="kpi-card">
        <div class="kpi-stripe" style="background-color: #10B981;"></div>
        <div class="kpi-label">ACTIVE TRACKED</div>
        <div class="kpi-val" style="color: var(--kpi-val5);">{{ $printers->where('is_active', true)->count() }}</div>
        <div class="kpi-sub">Ready to monitor and bill</div>
    </div>
    <div class="kpi-card">
        <div class="kpi-stripe" style="background-color: #818CF8;"></div>
        <div class="kpi-label">NETWORK PRINTERS</div>
        <div class="kpi-val" style="color: #818CF8;">{{ $printers->where('is_network', true)->count() }}</div>
        <div class="kpi-sub">Shared on local network</div>
    </div>
    <div class="kpi-card">
        <div class="kpi-stripe" style="background-color: #F59E0B;"></div>
        <div class="kpi-label">LIFETIME PRINT JOBS</div>
        <div class="kpi-val" style="color: var(--kpi-val6);">{{ number_format($printers->sum('job_count')) }}</div>
        <div class="kpi-sub">Processed through spooler</div>
    </div>
</div>

<div class="table-card">
    <div class="table-header">
        <h3 style="font-size: 0.92rem; font-weight: 700; color: var(--text-primary);">Fleet Printers ({{ $printers->count() }})</h3>
    </div>

    <div style="overflow-x: auto;">
        <table class="data-table">
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
                                <strong style="font-size: 0.9rem; color: var(--text-primary);">
                                    {{ $p->alias_name ?: $p->name }}
                                </strong>
                                @if ($p->alias_name)
                                    <span style="font-size: 0.75rem; color: var(--text-secondary); display: block;">System: {{ $p->name }}</span>
                                @endif
                                @if (!empty($p->location))
                                    <span style="font-size: 0.75rem; color: var(--accent-primary); display: block;">📍 {{ $p->location }}</span>
                                @endif
                                @if ($p->is_default)
                                    <span class="pill-badge" style="background: rgba(56, 189, 248, 0.15); color: var(--accent-primary); margin-top: 3px;">Default</span>
                                @endif
                            </div>
                        </td>
                        <td>
                            <strong style="color: var(--text-primary); font-size: 0.85rem;">{{ $p->device ? $p->device->shop_name : 'Shop' }}</strong>
                            <span style="font-size: 0.75rem; color: var(--text-secondary); display: block;">{{ $p->device ? $p->device->computer_name : $p->device_id }}</span>
                        </td>
                        <td>
                            @if ($p->is_network)
                                <span class="pill-badge pill-network">🌐 Network</span>
                            @else
                                <span class="pill-badge pill-usb">🔌 Local USB</span>
                            @endif
                            <span style="font-size: 0.72rem; color: var(--text-secondary); display: block; margin-top: 2px;">Port: {{ $p->port_name ?: 'N/A' }}</span>
                        </td>
                        <td>
                            @if ($p->is_active)
                                <span class="pill-badge pill-active">● Active</span>
                            @else
                                <span class="pill-badge pill-disabled">○ Disabled</span>
                            @endif
                        </td>
                        <td>
                            <div style="font-size: 0.8rem; line-height: 1.4;">
                                <span style="color: var(--text-secondary);">B&amp;W: <strong style="color: var(--text-primary);">KES {{ number_format($p->cost_per_mono_page ?? 0, 2) }}</strong></span>
                                <br>
                                <span style="color: var(--text-secondary);">Color: <strong style="color: #EC4899;">KES {{ number_format($p->cost_per_color_page ?? 0, 2) }}</strong></span>
                            </div>
                        </td>
                        <td>
                            <strong style="color: var(--accent-primary); font-size: 0.95rem;">{{ number_format($p->job_count) }}</strong>
                        </td>
                        <td style="color: var(--text-secondary); font-size: 0.75rem;">
                            {{ $p->last_seen_at ? $p->last_seen_at->diffForHumans() : 'Active' }}
                        </td>
                        <td style="text-align: right;">
                            <button onclick="openEditModal({{ json_encode($p) }})" class="btn-section" style="padding: 0.35rem 0.75rem; font-size: 0.78rem;">
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
<div id="printerModal" style="display: none; position: fixed; inset: 0; background: rgba(0, 0, 0, 0.65); backdrop-filter: blur(4px); z-index: 9999; justify-content: center; align-items: center; padding: 1rem;">
    <div style="background: var(--bg-card); border: 1px solid var(--border-color); border-radius: 8px; width: 100%; max-width: 500px; padding: 1.5rem; box-shadow: 0 25px 50px -12px rgba(0,0,0,0.5);">
        <div style="display: flex; justify-content: space-between; align-items: center; margin-bottom: 1.25rem;">
            <h3 style="font-size: 1.1rem; font-weight: 700; color: var(--text-primary);" id="modalTitle">⚙️ Configure Printer</h3>
            <button onclick="closeEditModal()" style="background: none; border: none; color: var(--text-secondary); font-size: 1.25rem; cursor: pointer;">✕</button>
        </div>

        <form id="printerForm" method="POST" action="">
            @csrf
            <div style="margin-bottom: 1rem;">
                <label style="display: block; font-size: 0.78rem; font-weight: 600; color: var(--text-secondary); margin-bottom: 0.35rem;">Friendly Alias / Display Name</label>
                <input type="text" name="alias_name" id="modalAlias" class="form-control-theme" placeholder="e.g. Front Cashier Desk HP LaserJet">
            </div>

            <div style="margin-bottom: 1rem;">
                <label style="display: block; font-size: 0.78rem; font-weight: 600; color: var(--text-secondary); margin-bottom: 0.35rem;">Physical Location / Station Notes</label>
                <input type="text" name="location" id="modalLocation" class="form-control-theme" placeholder="e.g. Counter 1 next to POS register">
            </div>

            <div style="display: grid; grid-template-columns: 1fr 1fr; gap: 1rem; margin-bottom: 1rem;">
                <div>
                    <label style="display: block; font-size: 0.78rem; font-weight: 600; color: var(--text-secondary); margin-bottom: 0.35rem;">B&amp;W Rate (Per Page)</label>
                    <input type="number" step="0.01" name="cost_per_mono_page" id="modalMonoCost" class="form-control-theme" placeholder="5.00">
                </div>
                <div>
                    <label style="display: block; font-size: 0.78rem; font-weight: 600; color: var(--text-secondary); margin-bottom: 0.35rem;">Color Rate (Per Page)</label>
                    <input type="number" step="0.01" name="cost_per_color_page" id="modalColorCost" class="form-control-theme" placeholder="20.00">
                </div>
            </div>

            <div style="margin-bottom: 1.25rem;">
                <label style="display: flex; align-items: center; gap: 0.5rem; font-size: 0.85rem; color: var(--text-primary); cursor: pointer;">
                    <input type="checkbox" name="is_active" id="modalIsActive" value="1" style="width: 16px; height: 16px; accent-color: var(--accent-primary);">
                    <span>Enable Active Monitoring &amp; Accounting for this printer</span>
                </label>
            </div>

            <div style="display: flex; justify-content: flex-end; gap: 0.75rem;">
                <button type="button" onclick="closeEditModal()" class="btn-section">Cancel</button>
                <button type="submit" class="btn-section btn-primary-action">💾 Save Printer Settings</button>
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
