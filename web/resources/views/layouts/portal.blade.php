<!DOCTYPE html>
<html lang="en" data-theme="dark">
<head>
    <meta charset="UTF-8">
    <meta name="viewport" content="width=device-width, initial-scale=1.0">
    <title>@yield('title', 'Dashboard') — Nexrein Printer Monitor</title>
    <link rel="preconnect" href="https://fonts.googleapis.com">
    <link rel="preconnect" href="https://fonts.gstatic.com" crossorigin>
    <link href="https://fonts.googleapis.com/css2?family=Segoe+UI:wght@400;500;600;700;800&family=Plus+Jakarta+Sans:wght@400;500;600;700;800&display=swap" rel="stylesheet">
    <style>
        /* Exact color palette matching Desktop Manager (Image 1 Dark & Image 2 Light) */
        :root, [data-theme="dark"] {
            --bg-body: #0A0F1D;
            --bg-header: #0C1322;
            --bg-card: #111C35;
            --bg-card-hover: #162342;
            --bg-input: #0E172E;
            --border-color: #1E2D4A;
            --border-light: #2A3B5E;

            --text-primary: #FFFFFF;
            --text-secondary: #94A3B8;
            --text-muted: #64748B;

            --accent-primary: #38BDF8;
            --accent-primary-hover: #0EA5E9;
            --accent-success: #10B981;
            --accent-danger: #DC2626;

            --header-btn-bg: #1E293B;
            --header-btn-border: #334155;
            --header-btn-fg: #E2E8F0;

            --service-badge-bg: #022C22;
            --service-badge-border: #059669;
            --service-badge-fg: #34D399;

            --section-btn-bg: #111C35;
            --section-btn-border: #1E2D4A;
            --section-btn-fg: #FFFFFF;

            --kpi-val1: #38BDF8;
            --kpi-val2: #22D3EE;
            --kpi-val3: #CBD5E1;
            --kpi-val4: #C084FC;
            --kpi-val5: #34D399;
            --kpi-val6: #FB923C;

            --table-header-bg: #111C35;
            --table-row-bg: #111C35;
            --table-row-alt: #0E172E;

            --badge-color-bg: #0891B2;
            --badge-completed-bg: #022C22;
            --badge-completed-border: #059669;
            --badge-completed-fg: #34D399;

            --footer-bg: #0C1322;
            --footer-fg: #94A3B8;
        }

        [data-theme="light"] {
            --bg-body: #EDF2F7;
            --bg-header: #0C1322; /* Dark header preserved matching Image 2! */
            --bg-card: #FFFFFF;
            --bg-card-hover: #F8FAFC;
            --bg-input: #FFFFFF;
            --border-color: #E2E8F0;
            --border-light: #CBD5E1;

            --text-primary: #0F172A;
            --text-secondary: #475569;
            --text-muted: #64748B;

            --accent-primary: #2563EB;
            --accent-primary-hover: #1D4ED8;
            --accent-success: #059669;
            --accent-danger: #DC2626;

            --header-btn-bg: #1E293B;
            --header-btn-border: #334155;
            --header-btn-fg: #E2E8F0;

            --service-badge-bg: #022C22;
            --service-badge-border: #059669;
            --service-badge-fg: #34D399;

            --section-btn-bg: #FFFFFF;
            --section-btn-border: #CBD5E1;
            --section-btn-fg: #0F172A;

            --kpi-val1: #1D4ED8;
            --kpi-val2: #0D9488;
            --kpi-val3: #475569;
            --kpi-val4: #7C3AED;
            --kpi-val5: #059669;
            --kpi-val6: #D97706;

            --table-header-bg: #F8FAFC;
            --table-row-bg: #FFFFFF;
            --table-row-alt: #F8FAFC;

            --badge-color-bg: #0D9488;
            --badge-completed-bg: #DCFCE7;
            --badge-completed-border: #86EFAC;
            --badge-completed-fg: #166534;

            --footer-bg: #EDF2F7;
            --footer-fg: #64748B;
        }

        * {
            box-sizing: border-box;
            margin: 0;
            padding: 0;
            font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', 'Plus Jakarta Sans', Roboto, sans-serif;
        }

        body {
            background-color: var(--bg-body);
            color: var(--text-primary);
            min-height: 100vh;
            display: flex;
            flex-direction: column;
            transition: background-color 0.2s ease, color 0.2s ease;
        }

        /* Top Header (Midnight Navy in both dark & light themes) */
        .header-top {
            background-color: var(--bg-header);
            border-bottom: 1px solid var(--border-color);
            position: sticky;
            top: 0;
            z-index: 50;
        }

        .header-container {
            max-width: 1440px;
            margin: 0 auto;
            padding: 0.85rem 1.75rem 0.35rem;
        }

        .header-row-1 {
            display: flex;
            align-items: center;
            justify-content: space-between;
            margin-bottom: 0.65rem;
            flex-wrap: wrap;
            gap: 1rem;
        }

        .brand-link {
            display: flex;
            align-items: center;
            gap: 0.85rem;
            text-decoration: none;
        }

        .brand-badge {
            width: 34px;
            height: 34px;
            background-color: #38BDF8;
            border-radius: 6px;
            display: flex;
            align-items: center;
            justify-content: center;
            color: #FFFFFF;
            font-size: 1.25rem;
            font-weight: 800;
            line-height: 1;
        }

        .brand-text h1 {
            font-size: 1.05rem;
            font-weight: 700;
            color: #FFFFFF;
            letter-spacing: -0.01em;
            line-height: 1.2;
        }

        .brand-text p {
            font-size: 0.72rem;
            color: #94A3B8;
            margin-top: 2px;
        }

        .header-actions {
            display: flex;
            align-items: center;
            gap: 0.55rem;
        }

        /* Service Running Pill */
        .service-pill {
            display: inline-flex;
            align-items: center;
            gap: 0.45rem;
            background-color: var(--service-badge-bg);
            border: 1px solid var(--service-badge-border);
            color: var(--service-badge-fg);
            padding: 0.35rem 0.85rem;
            border-radius: 9999px;
            font-size: 0.72rem;
            font-weight: 700;
            letter-spacing: 0.04em;
        }

        .service-dot {
            width: 7px;
            height: 7px;
            border-radius: 50%;
            background-color: #10B981;
            box-shadow: 0 0 6px #10B981;
            animation: pulse-dot 2s infinite;
        }

        @keyframes pulse-dot {
            0%, 100% { opacity: 1; transform: scale(1); }
            50% { opacity: 0.5; transform: scale(0.85); }
        }

        .btn-header {
            background-color: var(--header-btn-bg);
            border: 1px solid var(--header-btn-border);
            color: var(--header-btn-fg);
            font-size: 0.78rem;
            font-weight: 600;
            padding: 0.45rem 0.95rem;
            border-radius: 5px;
            text-decoration: none;
            cursor: pointer;
            transition: all 0.15s ease;
            display: inline-flex;
            align-items: center;
            gap: 0.35rem;
        }
        .btn-header:hover { background-color: #334155; }

        .btn-header-stop {
            background-color: #DC2626;
            border: none;
            color: #FFFFFF;
        }
        .btn-header-stop:hover { background-color: #B91C1C; }

        .btn-header-refresh {
            background-color: #38BDF8;
            border: none;
            color: #0F172A;
            font-weight: 700;
        }
        .btn-header-refresh:hover { background-color: #0EA5E9; }

        .theme-toggle-btn {
            background: #1E293B;
            border: 1px solid #334155;
            color: #E2E8F0;
            padding: 0.45rem 0.75rem;
            border-radius: 5px;
            font-size: 0.78rem;
            cursor: pointer;
            display: inline-flex;
            align-items: center;
            gap: 0.35rem;
        }

        /* Nav Tabs Bar (Flat Underline Tabs) */
        .nav-tabs-bar {
            display: flex;
            align-items: center;
            justify-content: space-between;
            border-top: 1px solid rgba(255, 255, 255, 0.05);
            padding-top: 0.2rem;
        }

        .nav-links {
            display: flex;
            align-items: center;
            gap: 1.5rem;
        }

        .nav-tab-item {
            color: #94A3B8;
            text-decoration: none;
            font-size: 0.85rem;
            font-weight: 600;
            padding: 0.5rem 0.2rem 0.65rem;
            border-bottom: 2.5px solid transparent;
            transition: all 0.2s ease;
        }
        .nav-tab-item:hover { color: #FFFFFF; }
        .nav-tab-item.active {
            color: #FFFFFF;
            font-weight: 700;
            border-bottom-color: #38BDF8;
        }

        .nav-admin-badge {
            font-size: 0.78rem;
            color: #94A3B8;
            font-weight: 600;
            display: flex;
            align-items: center;
            gap: 0.75rem;
        }

        .btn-logout {
            background: transparent;
            border: 1px solid #334155;
            color: #94A3B8;
            font-size: 0.72rem;
            padding: 0.25rem 0.65rem;
            border-radius: 4px;
            cursor: pointer;
            text-decoration: none;
            transition: all 0.2s;
        }
        .btn-logout:hover { color: #FFFFFF; background-color: #1E293B; }

        /* Main Container */
        .main-content {
            flex: 1;
            max-width: 1440px;
            width: 100%;
            margin: 0 auto;
            padding: 1.5rem 1.75rem;
        }

        /* Section Action Buttons */
        .btn-section {
            background-color: var(--section-btn-bg);
            border: 1px solid var(--section-btn-border);
            color: var(--section-btn-fg);
            font-size: 0.8rem;
            font-weight: 600;
            padding: 0.5rem 0.95rem;
            border-radius: 5px;
            text-decoration: none;
            cursor: pointer;
            display: inline-flex;
            align-items: center;
            gap: 0.35rem;
            transition: all 0.2s;
        }
        .btn-section:hover { background-color: var(--bg-card-hover); }

        .btn-primary-action {
            background-color: var(--accent-primary);
            border: none;
            color: #FFFFFF;
            font-weight: 700;
        }
        .btn-primary-action:hover { background-color: var(--accent-primary-hover); }

        /* Card Styles */
        .card {
            background-color: var(--bg-card);
            border: 1px solid var(--border-color);
            border-radius: 6px;
            padding: 1.15rem 1.25rem;
            box-shadow: 0 1px 3px rgba(0, 0, 0, 0.1);
            transition: background-color 0.2s ease, border-color 0.2s ease;
        }

        /* Alerts */
        .alert {
            padding: 0.85rem 1.25rem;
            border-radius: 6px;
            margin-bottom: 1.25rem;
            font-size: 0.85rem;
            display: flex;
            align-items: center;
            justify-content: space-between;
        }
        .alert-success { background: rgba(16, 185, 129, 0.12); border: 1px solid rgba(16, 185, 129, 0.35); color: #34D399; }
        .alert-warning { background: rgba(245, 158, 11, 0.12); border: 1px solid rgba(245, 158, 11, 0.35); color: #FCD34D; }
        .alert-danger  { background: rgba(239, 68, 68, 0.12); border: 1px solid rgba(239, 68, 68, 0.35); color: #FCA5A5; }
        .alert-info    { background: rgba(56, 189, 248, 0.12); border: 1px solid rgba(56, 189, 248, 0.35); color: #7DD3FC; }

        /* Footer */
        .footer {
            background-color: var(--footer-bg);
            border-top: 1px solid var(--border-color);
            padding: 0.65rem 1.75rem;
            font-size: 0.74rem;
            color: var(--footer-fg);
            margin-top: auto;
        }
        .footer-container {
            max-width: 1440px;
            margin: 0 auto;
            display: flex;
            align-items: center;
            justify-content: space-between;
        }
    </style>
    @stack('styles')
</head>
<body>
    @auth
    <header class="header-top">
        <div class="header-container">
            <!-- Row 1: Brand Logo & Title + Actions -->
            <div class="header-row-1">
                <a href="{{ route('dashboard') }}" class="brand-link">
                    <div class="brand-badge">N</div>
                    <div class="brand-text">
                        <h1>Nexrein Printer Monitor</h1>
                        <p>Enterprise Print Accounting &amp; Spooler Management</p>
                    </div>
                </a>

                <div class="header-actions">
                    <div class="service-pill">
                        <span class="service-dot"></span>
                        <span>SERVICE RUNNING</span>
                    </div>

                    <a href="{{ route('dashboard') }}" class="btn-header">Start</a>
                    <a href="{{ route('dashboard') }}" class="btn-header btn-header-stop">Stop</a>
                    <a href="{{ route('dashboard') }}" class="btn-header">Restart</a>
                    <a href="{{ route('dashboard') }}" class="btn-header btn-header-refresh">Refresh</a>

                    <!-- Theme Switcher Toggle -->
                    <button type="button" id="btnThemeToggle" class="theme-toggle-btn" title="Toggle Dark/Light Mode">
                        <span id="themeIcon">🌙</span>
                        <span id="themeLabel">Dark</span>
                    </button>
                </div>
            </div>

            <!-- Row 2: Underline Nav Tabs Bar -->
            <div class="nav-tabs-bar">
                <nav class="nav-links">
                    <a href="{{ route('dashboard') }}" class="nav-tab-item {{ request()->routeIs('dashboard') ? 'active' : '' }}">
                        Dashboard
                    </a>
                    <a href="{{ route('devices') }}" class="nav-tab-item {{ request()->routeIs('devices') ? 'active' : '' }}">
                        Print Jobs &amp; Pages
                    </a>
                    <a href="{{ route('printers') }}" class="nav-tab-item {{ request()->routeIs('printers') ? 'active' : '' }}">
                        Printers
                    </a>
                    <a href="{{ route('password.change') }}" class="nav-tab-item {{ request()->routeIs('password.change') ? 'active' : '' }}">
                        API &amp; Security
                    </a>
                </nav>

                <div class="nav-admin-badge">
                    <span>Super Admin • {{ strtoupper(Auth::user()->name ?: 'NEXREIN') }}</span>
                    <form action="{{ route('logout') }}" method="POST" style="display: inline;">
                        @csrf
                        <button type="submit" class="btn-logout">Logout</button>
                    </form>
                </div>
            </div>
        </div>
    </header>
    @endauth

    <main class="main-content">
        @if (session('success'))
            <div class="alert alert-success">
                <span>✅ {{ session('success') }}</span>
            </div>
        @endif

        @if (session('warning'))
            <div class="alert alert-warning">
                <span>⚠️ {{ session('warning') }}</span>
            </div>
        @endif

        @if (session('error'))
            <div class="alert alert-danger">
                <span>❌ {{ session('error') }}</span>
            </div>
        @endif

        @if (session('info'))
            <div class="alert alert-info">
                <span>ℹ️ {{ session('info') }}</span>
            </div>
        @endif

        @yield('content')
    </main>

    <footer class="footer">
        <div class="footer-container">
            <div>Connected - https://printmonitor.nexreindigital.co.ke</div>
            <div>Suite v2.0.0 (win-x64 Enterprise)</div>
        </div>
    </footer>

    <script>
        // Synchronized Theme Manager (Persists choice and matches desktop palette)
        (function() {
            const html = document.documentElement;
            const btn = document.getElementById('btnThemeToggle');
            const icon = document.getElementById('themeIcon');
            const label = document.getElementById('themeLabel');

            function applyTheme(theme) {
                html.setAttribute('data-theme', theme);
                localStorage.setItem('printmonitor_theme', theme);
                if (icon && label) {
                    if (theme === 'light') {
                        icon.textContent = '☀️';
                        label.textContent = 'Light';
                    } else {
                        icon.textContent = '🌙';
                        label.textContent = 'Dark';
                    }
                }
            }

            // Read saved preference or detect system
            const savedTheme = localStorage.getItem('printmonitor_theme') || 
                (window.matchMedia && window.matchMedia('(prefers-color-scheme: light)').matches ? 'light' : 'dark');
            applyTheme(savedTheme);

            if (btn) {
                btn.addEventListener('click', () => {
                    const current = html.getAttribute('data-theme') || 'dark';
                    const next = current === 'dark' ? 'light' : 'dark';
                    applyTheme(next);
                });
            }
        })();
    </script>
    @stack('scripts')
</body>
</html>
