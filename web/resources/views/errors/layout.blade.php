<!DOCTYPE html>
<html lang="en">
<head>
    <meta charset="UTF-8">
    <meta name="viewport" content="width=device-width, initial-scale=1.0">
    <title>@yield('title') — Nexrein Printer Monitor</title>
    <link rel="preconnect" href="https://fonts.googleapis.com">
    <link rel="preconnect" href="https://fonts.gstatic.com" crossorigin>
    <link href="https://fonts.googleapis.com/css2?family=Plus+Jakarta+Sans:wght@400;500;600;700;800&display=swap" rel="stylesheet">
    <style>
        :root {
            --bg-body: #090D16;
            --bg-card: #111827;
            --bg-card-hover: #1F2937;
            --border-color: #1F2937;
            --border-light: #374151;
            --accent-blue: #3B82F6;
            --accent-blue-hover: #2563EB;
            --accent-rose: #F43F5E;
            --accent-amber: #F59E0B;
            --accent-emerald: #10B981;
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
            background-image: 
                radial-gradient(at 15% 15%, rgba(59, 130, 246, 0.08) 0px, transparent 50%),
                radial-gradient(at 85% 85%, rgba(139, 92, 246, 0.08) 0px, transparent 50%);
            color: var(--text-primary);
            min-height: 100vh;
            display: flex;
            flex-direction: column;
            align-items: center;
            justify-content: center;
            padding: 2rem 1.5rem;
        }

        .error-card {
            background-color: rgba(17, 24, 39, 0.85);
            backdrop-filter: blur(16px);
            border: 1px solid var(--border-color);
            border-radius: 20px;
            box-shadow: 0 20px 40px -15px rgba(0, 0, 0, 0.6), 0 0 30px rgba(59, 130, 246, 0.1);
            max-width: 560px;
            width: 100%;
            padding: 3rem 2.5rem;
            text-align: center;
            position: relative;
            overflow: hidden;
        }

        .error-card::before {
            content: '';
            position: absolute;
            top: 0;
            left: 0;
            right: 0;
            height: 3px;
            background: linear-gradient(90deg, #3B82F6, #8B5CF6, #F43F5E);
        }

        .brand-badge {
            display: inline-flex;
            align-items: center;
            gap: 0.5rem;
            padding: 0.4rem 0.85rem;
            background: rgba(59, 130, 246, 0.12);
            border: 1px solid rgba(59, 130, 246, 0.25);
            border-radius: 9999px;
            font-size: 0.8rem;
            font-weight: 600;
            color: var(--accent-blue);
            margin-bottom: 2rem;
        }

        .error-icon-box {
            width: 80px;
            height: 80px;
            margin: 0 auto 1.75rem;
            border-radius: 20px;
            display: flex;
            align-items: center;
            justify-content: center;
            font-size: 2.5rem;
            box-shadow: 0 10px 25px -5px rgba(0, 0, 0, 0.4);
            border: 1px solid var(--border-light);
        }

        .error-code {
            font-size: 4rem;
            font-weight: 800;
            line-height: 1;
            letter-spacing: -0.04em;
            margin-bottom: 0.75rem;
            background: linear-gradient(135deg, #FFFFFF 30%, #9CA3AF 100%);
            -webkit-background-clip: text;
            -webkit-text-fill-color: transparent;
        }

        .error-title {
            font-size: 1.4rem;
            font-weight: 700;
            color: var(--text-primary);
            margin-bottom: 0.75rem;
        }

        .error-message {
            font-size: 0.95rem;
            color: var(--text-secondary);
            line-height: 1.6;
            margin-bottom: 2.25rem;
        }

        .actions-group {
            display: flex;
            flex-wrap: wrap;
            gap: 0.75rem;
            justify-content: center;
        }

        .btn {
            display: inline-flex;
            align-items: center;
            justify-content: center;
            gap: 0.5rem;
            padding: 0.75rem 1.35rem;
            border-radius: 10px;
            font-size: 0.875rem;
            font-weight: 600;
            text-decoration: none;
            cursor: pointer;
            transition: all 0.2s ease;
            border: 1px solid transparent;
        }

        .btn-primary {
            background-color: var(--accent-blue);
            color: #FFFFFF;
            box-shadow: 0 4px 12px rgba(59, 130, 246, 0.3);
        }

        .btn-primary:hover {
            background-color: var(--accent-blue-hover);
            transform: translateY(-1px);
        }

        .btn-secondary {
            background-color: var(--bg-card-hover);
            color: var(--text-primary);
            border-color: var(--border-light);
        }

        .btn-secondary:hover {
            background-color: #374151;
            transform: translateY(-1px);
        }

        .footer-note {
            margin-top: 2.5rem;
            font-size: 0.75rem;
            color: var(--text-muted);
        }
    </style>
</head>
<body>
    <div class="error-card">
        <div class="brand-badge">
            <span>🖨️</span> Nexrein Printer Monitor Cloud
        </div>

        <div class="error-icon-box" style="@yield('icon_style', 'background: rgba(59, 130, 246, 0.15);')">
            @yield('icon', '⚠️')
        </div>

        <div class="error-code">@yield('code', 'Error')</div>
        <h1 class="error-title">@yield('heading', 'Something Went Wrong')</h1>
        <p class="error-message">@yield('message', 'An unexpected error occurred while processing your request.')</p>

        <div class="actions-group">
            <a href="{{ route('dashboard') }}" class="btn btn-primary">
                <span>📊</span> Go to Dashboard
            </a>
            <a href="javascript:history.back()" class="btn btn-secondary">
                <span>↩️</span> Previous Page
            </a>
            <a href="{{ route('login') }}" class="btn btn-secondary">
                <span>🔐</span> Sign In
            </a>
        </div>

        <div class="footer-note">
            Nexrein Printer Monitor &copy; {{ date('Y') }} &bull; Enterprise Print Analytics & Remote Management
        </div>
    </div>
</body>
</html>
