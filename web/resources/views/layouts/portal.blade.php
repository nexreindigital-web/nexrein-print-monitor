<!DOCTYPE html>
<html lang="en">
<head>
    <meta charset="UTF-8">
    <meta name="viewport" content="width=device-width, initial-scale=1.0">
    <title>@yield('title', 'Dashboard') — Nexrein Printer Monitor</title>
    <link rel="preconnect" href="https://fonts.googleapis.com">
    <link rel="preconnect" href="https://fonts.gstatic.com" crossorigin>
    <link href="https://fonts.googleapis.com/css2?family=Plus+Jakarta+Sans:wght@400;500;600;700;800&display=swap" rel="stylesheet">
    <style>
        :root {
            --bg-body: #090D16;
            --bg-card: #111827;
            --bg-card-hover: #1F2937;
            --bg-input: #1F2937;
            --border-color: #1F2937;
            --border-light: #374151;
            --accent-blue: #3B82F6;
            --accent-blue-hover: #2563EB;
            --accent-emerald: #10B981;
            --accent-amber: #F59E0B;
            --accent-rose: #F43F5E;
            --accent-purple: #8B5CF6;
            --text-primary: #F9FAFB;
            --text-secondary: #9CA3AF;
            --text-muted: #6B7280;
        }

        * {
            box-sizing: border-box;
            margin: 0;
            padding: 0;
            font-family: 'Plus Jakarta Sans', system-ui, -apple-system, sans-serif;
        }

        body {
            background-color: var(--bg-body);
            color: var(--text-primary);
            min-height: 100vh;
            display: flex;
            flex-direction: column;
        }

        /* Nav Header */
        .navbar {
            background-color: rgba(17, 24, 39, 0.9);
            backdrop-filter: blur(12px);
            border-bottom: 1px solid var(--border-color);
            position: sticky;
            top: 0;
            z-index: 50;
            padding: 0.85rem 1.75rem;
        }

        .nav-container {
            max-width: 1400px;
            margin: 0 auto;
            display: flex;
            align-items: center;
            justify-content: space-between;
        }

        .brand-link {
            display: flex;
            align-items: center;
            gap: 0.75rem;
            text-decoration: none;
            color: var(--text-primary);
        }

        .brand-logo {
            width: 38px;
            height: 38px;
            background: linear-gradient(135deg, #3B82F6, #1D4ED8);
            border-radius: 10px;
            display: flex;
            align-items: center;
            justify-content: center;
            font-size: 1.25rem;
            box-shadow: 0 4px 14px rgba(59, 130, 246, 0.35);
        }

        .brand-text h1 {
            font-size: 1.1rem;
            font-weight: 700;
            letter-spacing: -0.02em;
        }

        .brand-text p {
            font-size: 0.75rem;
            color: var(--text-secondary);
        }

        .nav-links {
            display: flex;
            align-items: center;
            gap: 0.5rem;
        }

        .nav-item {
            color: var(--text-secondary);
            text-decoration: none;
            padding: 0.5rem 0.9rem;
            font-size: 0.875rem;
            font-weight: 600;
            border-radius: 8px;
            transition: all 0.2s ease;
        }

        .nav-item:hover, .nav-item.active {
            color: var(--text-primary);
            background-color: var(--bg-card-hover);
        }

        .nav-item.active {
            color: #60A5FA;
            background-color: rgba(59, 130, 246, 0.12);
        }

        .nav-user {
            display: flex;
            align-items: center;
            gap: 1rem;
        }

        .user-pill {
            display: flex;
            align-items: center;
            gap: 0.6rem;
            background: var(--bg-card);
            border: 1px solid var(--border-color);
            padding: 0.4rem 0.85rem;
            border-radius: 9999px;
            font-size: 0.825rem;
        }

        .user-dot {
            width: 8px;
            height: 8px;
            border-radius: 50%;
            background-color: var(--accent-emerald);
            box-shadow: 0 0 8px var(--accent-emerald);
        }

        .btn {
            display: inline-flex;
            align-items: center;
            justify-content: center;
            gap: 0.45rem;
            padding: 0.55rem 1.15rem;
            border-radius: 8px;
            font-size: 0.85rem;
            font-weight: 600;
            text-decoration: none;
            cursor: pointer;
            border: none;
            transition: all 0.2s;
        }

        .btn-primary {
            background-color: var(--accent-blue);
            color: white;
        }
        .btn-primary:hover { background-color: var(--accent-blue-hover); }

        .btn-success {
            background-color: var(--accent-emerald);
            color: white;
        }
        .btn-success:hover { background-color: #059669; }

        .btn-secondary {
            background-color: #374151;
            color: #E5E7EB;
        }
        .btn-secondary:hover { background-color: #4B5563; }

        .btn-danger {
            background-color: #DC2626;
            color: white;
        }
        .btn-danger:hover { background-color: #B91C1C; }

        /* Container */
        .main-content {
            flex: 1;
            max-width: 1400px;
            width: 100%;
            margin: 0 auto;
            padding: 1.75rem;
        }

        /* Alerts */
        .alert {
            padding: 1rem 1.25rem;
            border-radius: 10px;
            margin-bottom: 1.5rem;
            font-size: 0.9rem;
            display: flex;
            align-items: center;
            justify-content: space-between;
        }
        .alert-success { background: rgba(16, 185, 129, 0.12); border: 1px solid rgba(16, 185, 129, 0.35); color: #6EE7B7; }
        .alert-warning { background: rgba(245, 158, 11, 0.12); border: 1px solid rgba(245, 158, 11, 0.35); color: #FCD34D; }
        .alert-danger  { background: rgba(244, 63, 94, 0.12); border: 1px solid rgba(244, 63, 94, 0.35); color: #FDA4AF; }
        .alert-info    { background: rgba(59, 130, 246, 0.12); border: 1px solid rgba(59, 130, 246, 0.35); color: #93C5FD; }

        /* Footer */
        .footer {
            border-top: 1px solid var(--border-color);
            background: var(--bg-card);
            padding: 1.25rem 2rem;
            margin-top: auto;
            text-align: center;
            font-size: 0.8rem;
            color: var(--text-muted);
        }
    </style>
    @stack('styles')
</head>
<body>
    @auth
    <header class="navbar">
        <div class="nav-container">
            <a href="{{ route('dashboard') }}" class="brand-link">
                <div class="brand-logo">🖨️</div>
                <div class="brand-text">
                    <h1>Nexrein Printer Monitor</h1>
                    <p>printmonitor.nexreindigital.co.ke</p>
                </div>
            </a>

            <nav class="nav-links">
                <a href="{{ route('dashboard') }}" class="nav-item {{ request()->routeIs('dashboard') ? 'active' : '' }}">
                    📊 Dashboard Overview
                </a>
                <a href="{{ route('devices') }}" class="nav-item {{ request()->routeIs('devices') ? 'active' : '' }}">
                    💻 Computers &amp; Shops
                </a>
                <a href="{{ route('printers') }}" class="nav-item {{ request()->routeIs('printers') ? 'active' : '' }}">
                    🖨️ Printers Fleet
                </a>
                <a href="{{ route('password.change') }}" class="nav-item {{ request()->routeIs('password.change') ? 'active' : '' }}">
                    🔐 Security &amp; Password
                </a>
            </nav>

            <div class="nav-user">
                <div class="user-pill">
                    <span class="user-dot"></span>
                    <span style="font-weight: 600;">{{ Auth::user()->name }}</span>
                    <span style="color: var(--text-muted);">({{ Auth::user()->email }})</span>
                </div>

                <form action="{{ route('logout') }}" method="POST" style="display: inline;">
                    @csrf
                    <button type="submit" class="btn btn-secondary" style="padding: 0.4rem 0.85rem; font-size: 0.8rem;">
                        🚪 Logout
                    </button>
                </form>
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
        <p>&copy; {{ date('Y') }} <strong>Nexrein Digital Solutions</strong> — Nexrein Printer Monitor Enterprise Suite v2.0.0. All print data encrypted &amp; synchronized.</p>
    </footer>

    @stack('scripts')
</body>
</html>
