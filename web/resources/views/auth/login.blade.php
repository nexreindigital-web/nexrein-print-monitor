@extends('layouts.portal')

@section('title', 'Sign In — Nexrein Printer Monitor')

@push('styles')
<style>
    .login-page {
        min-height: 80vh;
        display: flex;
        align-items: center;
        justify-content: center;
        padding: 2rem 1rem;
    }

    .login-columns {
        display: grid;
        grid-template-columns: 1fr 1fr;
        gap: 2rem;
        width: 100%;
        max-width: 880px;
        align-items: start;
    }

    @media (max-width: 680px) {
        .login-columns { grid-template-columns: 1fr; }
        .download-panel { order: 2; }
        .login-panel { order: 1; }
    }

    /* ── Login Card ── */
    .auth-card {
        background-color: var(--bg-card);
        border: 1px solid var(--border-color);
        border-radius: 8px;
        padding: 2.25rem 2rem;
        box-shadow: 0 8px 32px rgba(0,0,0,0.2);
    }

    .auth-header { text-align: center; margin-bottom: 1.75rem; }

    .brand-badge-center {
        width: 44px; height: 44px;
        background-color: #38BDF8;
        border-radius: 8px;
        display: inline-flex;
        align-items: center; justify-content: center;
        color: #FFFFFF;
        font-size: 1.65rem; font-weight: 800;
        margin-bottom: 0.85rem;
    }

    .auth-title {
        font-size: 1.2rem; font-weight: 700;
        color: var(--text-primary);
        letter-spacing: -0.01em;
        margin-bottom: 0.2rem;
    }

    .auth-subtitle { color: var(--text-secondary); font-size: 0.76rem; }

    .form-group { margin-bottom: 1.1rem; }

    .form-label {
        display: block; font-size: 0.75rem;
        font-weight: 600; margin-bottom: 0.35rem;
        color: var(--text-secondary);
    }

    .form-control {
        width: 100%;
        background-color: var(--bg-input);
        border: 1px solid var(--border-color);
        border-radius: 6px;
        padding: 0.65rem 0.85rem;
        color: var(--text-primary);
        font-size: 0.88rem; outline: none;
        transition: border-color 0.2s, box-shadow 0.2s;
    }
    .form-control:focus {
        border-color: #38BDF8;
        box-shadow: 0 0 0 3px rgba(56, 189, 248, 0.15);
    }

    .form-options {
        display: flex; align-items: center;
        justify-content: space-between;
        margin-bottom: 1.35rem; font-size: 0.76rem;
    }
    .form-options a { color: var(--accent-primary); text-decoration: none; font-weight: 600; }
    .form-options a:hover { text-decoration: underline; }

    .btn-submit {
        width: 100%;
        background-color: #38BDF8; color: #0F172A;
        border: none; padding: 0.8rem;
        font-size: 0.88rem; font-weight: 700;
        border-radius: 6px; cursor: pointer;
        transition: background-color 0.15s;
    }
    .btn-submit:hover { background-color: #0EA5E9; }

    .callout-box {
        margin-top: 1.25rem;
        background-color: var(--bg-input);
        border: 1px solid var(--border-color);
        border-radius: 6px;
        padding: 0.8rem 1rem;
        font-size: 0.73rem;
        color: var(--text-secondary);
        line-height: 1.5;
    }
    .callout-box code {
        background-color: rgba(56, 189, 248, 0.15);
        color: #38BDF8;
        padding: 0.1rem 0.35rem;
        border-radius: 4px; font-weight: 700;
    }

    /* ── Download Panel ── */
    .download-panel {
        display: flex; flex-direction: column; gap: 1rem;
    }

    .download-card {
        background-color: var(--bg-card);
        border: 1px solid var(--border-color);
        border-radius: 8px;
        padding: 1.5rem 1.5rem 1.25rem;
        box-shadow: 0 4px 16px rgba(0,0,0,0.15);
    }

    .download-card-title {
        font-size: 1rem; font-weight: 700;
        color: var(--text-primary);
        margin-bottom: 0.35rem;
        display: flex; align-items: center; gap: 0.5rem;
    }

    .download-card-sub {
        font-size: 0.75rem; color: var(--text-secondary);
        margin-bottom: 1.15rem; line-height: 1.5;
    }

    .btn-download {
        display: flex; align-items: center; justify-content: center; gap: 0.55rem;
        width: 100%;
        background: linear-gradient(135deg, #38BDF8 0%, #0EA5E9 100%);
        color: #0F172A;
        border: none; padding: 0.8rem 1rem;
        font-size: 0.9rem; font-weight: 800;
        border-radius: 7px; cursor: pointer;
        text-decoration: none;
        transition: opacity 0.2s, transform 0.1s;
        letter-spacing: 0.01em;
        box-shadow: 0 4px 12px rgba(56, 189, 248, 0.35);
    }
    .btn-download:hover { opacity: 0.9; transform: translateY(-1px); }
    .btn-download:active { transform: translateY(0); }

    .feature-list {
        list-style: none; padding: 0;
        margin-top: 1rem;
        display: flex; flex-direction: column; gap: 0.5rem;
    }
    .feature-list li {
        display: flex; align-items: flex-start; gap: 0.55rem;
        font-size: 0.78rem; color: var(--text-secondary); line-height: 1.4;
    }
    .feature-list li .icon {
        font-size: 0.9rem; flex-shrink: 0; margin-top: 1px;
    }

    .badge-version {
        display: inline-block;
        background: rgba(56, 189, 248, 0.15);
        color: #38BDF8;
        border: 1px solid rgba(56, 189, 248, 0.3);
        font-size: 0.68rem; font-weight: 700;
        padding: 0.1rem 0.55rem;
        border-radius: 9999px;
        margin-left: 0.35rem;
        vertical-align: middle;
    }

    .system-req {
        background-color: var(--bg-input);
        border: 1px solid var(--border-color);
        border-radius: 6px; padding: 0.75rem 1rem;
        font-size: 0.72rem; color: var(--text-secondary);
        line-height: 1.55;
    }
    .system-req strong { color: var(--text-primary); }
</style>
@endpush

@section('content')
<div class="login-page">
    <div class="login-columns">

        {{-- ── Left: Login Form ── --}}
        <div class="login-panel">
            <div class="auth-card">
                <div class="auth-header">
                    <div class="brand-badge-center">N</div>
                    <h1 class="auth-title">Sign In to Portal</h1>
                    <p class="auth-subtitle">Nexrein Printer Monitor — Enterprise Print Accounting</p>
                </div>

                @if (request('email'))
                    <div style="background: rgba(16,185,129,0.12); border: 1px solid rgba(16,185,129,0.3); border-radius: 6px; padding: 0.75rem 0.95rem; margin-bottom: 1.25rem; font-size: 0.77rem; color: #34D399; line-height: 1.5;">
                        🎉 <strong>Workstation Linked!</strong><br>
                        Account ready for <strong>{{ request('email') }}</strong>. Sign in with default password <code style="background: rgba(52,211,153,0.15); color: #34D399; padding: 0.1rem 0.35rem; border-radius: 4px;">admin</code>.
                    </div>
                @endif

                @if (session('info'))
                    <div class="alert alert-info" style="margin-bottom: 1.25rem;">{{ session('info') }}</div>
                @endif

                @if ($errors->any())
                    <div style="background: rgba(239,68,68,0.12); border: 1px solid rgba(239,68,68,0.3); border-radius: 6px; padding: 0.75rem 1rem; margin-bottom: 1.25rem; font-size: 0.78rem; color: #FCA5A5;">
                        @foreach ($errors->all() as $error)
                            <p>{{ $error }}</p>
                        @endforeach
                    </div>
                @endif

                <form action="{{ route('login.post') }}" method="POST">
                    @csrf

                    <div class="form-group">
                        <label class="form-label" for="email">Email Address</label>
                        <input type="email" id="email" name="email" class="form-control"
                               placeholder="you@company.com"
                               value="{{ old('email', request('email')) }}"
                               required autofocus>
                    </div>

                    <div class="form-group">
                        <label class="form-label" for="password">Password</label>
                        <input type="password" id="password" name="password" class="form-control"
                               placeholder="••••••••" required>
                    </div>

                    <div class="form-options">
                        <label style="display: flex; align-items: center; gap: 0.4rem; cursor: pointer; color: var(--text-secondary);">
                            <input type="checkbox" name="remember" value="1"> Remember me
                        </label>
                        <a href="{{ route('password.forgot') }}">Forgot Password?</a>
                    </div>

                    <button type="submit" class="btn-submit" id="btnSignIn">
                        🔐 Sign In to Dashboard
                    </button>
                </form>

                <div class="callout-box">
                    <strong>💡 First Time?</strong><br>
                    Install the desktop agent and enter your email — your account is created automatically.<br>
                    Default password is <code>admin</code>. You'll be asked to change it on first sign-in.
                </div>
            </div>
        </div>

        {{-- ── Right: Download Software Panel ── --}}
        <div class="download-panel">
            <div class="download-card">
                <div class="download-card-title">
                    📥 Download Desktop Agent
                    <span class="badge-version">v2.0.0</span>
                </div>
                <p class="download-card-sub">
                    Install the Nexrein Printer Monitor agent on every Windows workstation you want to track. It runs silently as a background service, captures all print jobs, and syncs them here in real-time.
                </p>

                <a href="https://github.com/nexreindigital-web/nexrein-print-monitor/releases/latest/download/PrintMonitor-Setup.exe"
                   class="btn-download" id="btnDownloadSetup">
                    <svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2.5" stroke-linecap="round" stroke-linejoin="round">
                        <path d="M21 15v4a2 2 0 0 1-2 2H5a2 2 0 0 1-2-2v-4"/>
                        <polyline points="7 10 12 15 17 10"/>
                        <line x1="12" y1="15" x2="12" y2="3"/>
                    </svg>
                    Download PrintMonitor-Setup.exe
                </a>

                <ul class="feature-list">
                    <li>
                        <span class="icon">🖨️</span>
                        <span>Captures <strong>every print job</strong> — document name, user, pages, color mode, printer</span>
                    </li>
                    <li>
                        <span class="icon">⚡</span>
                        <span>Real-time sync to this cloud portal <strong>every 10 seconds</strong></span>
                    </li>
                    <li>
                        <span class="icon">🔒</span>
                        <span>Password updates pushed from this portal — applied on desktops automatically</span>
                    </li>
                    <li>
                        <span class="icon">💻</span>
                        <span>Works silently as a Windows Service — no user interaction needed after install</span>
                    </li>
                    <li>
                        <span class="icon">📊</span>
                        <span>Per-page cost accounting with Color and B&amp;W rate configuration</span>
                    </li>
                    <li>
                        <span class="icon">🏪</span>
                        <span>Multi-shop / multi-branch — each workstation tagged by shop and computer name</span>
                    </li>
                </ul>
            </div>

            <div class="system-req">
                <strong>System Requirements:</strong><br>
                Windows 10 / 11 &bull; 64-bit &bull; .NET 10 (included) &bull; ~46 MB disk space<br>
                Internet connection required for cloud sync &bull; Administrator privileges for install
            </div>

            <div class="system-req" style="border-color: rgba(56,189,248,0.3); background: rgba(56,189,248,0.06);">
                <strong style="color: #38BDF8;">🔗 After Installing:</strong><br>
                1. Enter your email &amp; shop name during setup<br>
                2. Return here and sign in with default password <strong>admin</strong><br>
                3. Change your password in API &amp; Security settings<br>
                4. All print jobs will appear here within 10 seconds ✅
            </div>
        </div>

    </div>
</div>
@endsection
