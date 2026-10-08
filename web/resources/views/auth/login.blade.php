@extends('layouts.portal')

@section('title', 'Sign In')

@push('styles')
<style>
    .login-wrapper {
        min-height: 75vh;
        display: flex;
        align-items: center;
        justify-content: center;
        padding: 2rem 1rem;
    }

    .auth-card {
        background-color: var(--bg-card);
        border: 1px solid var(--border-color);
        border-radius: 8px;
        padding: 2.25rem 2.5rem;
        width: 100%;
        max-width: 440px;
        box-shadow: 0 4px 20px rgba(0, 0, 0, 0.25);
    }

    .auth-header {
        text-align: center;
        margin-bottom: 1.75rem;
    }

    .brand-badge-center {
        width: 42px;
        height: 42px;
        background-color: #38BDF8;
        border-radius: 8px;
        display: inline-flex;
        align-items: center;
        justify-content: center;
        color: #FFFFFF;
        font-size: 1.6rem;
        font-weight: 800;
        margin-bottom: 0.85rem;
    }

    .auth-title {
        font-size: 1.25rem;
        font-weight: 700;
        color: var(--text-primary);
        letter-spacing: -0.01em;
        margin-bottom: 0.25rem;
    }

    .auth-subtitle {
        color: var(--text-secondary);
        font-size: 0.78rem;
    }

    .form-group {
        margin-bottom: 1.15rem;
    }

    .form-label {
        display: block;
        font-size: 0.78rem;
        font-weight: 600;
        margin-bottom: 0.35rem;
        color: var(--text-secondary);
    }

    .form-control {
        width: 100%;
        background-color: var(--bg-input);
        border: 1px solid var(--border-color);
        border-radius: 6px;
        padding: 0.65rem 0.85rem;
        color: var(--text-primary);
        font-size: 0.88rem;
        outline: none;
        transition: border-color 0.2s;
    }

    .form-control:focus {
        border-color: #38BDF8;
        box-shadow: 0 0 0 2px rgba(56, 189, 248, 0.2);
    }

    .form-options {
        display: flex;
        align-items: center;
        justify-content: space-between;
        margin-bottom: 1.35rem;
        font-size: 0.78rem;
    }

    .form-options a {
        color: #38BDF8;
        text-decoration: none;
        font-weight: 600;
    }
    .form-options a:hover {
        text-decoration: underline;
    }

    .btn-submit {
        width: 100%;
        background-color: #38BDF8;
        color: #0F172A;
        border: none;
        padding: 0.75rem;
        font-size: 0.88rem;
        font-weight: 700;
        border-radius: 6px;
        cursor: pointer;
        transition: background-color 0.15s;
    }
    .btn-submit:hover {
        background-color: #0EA5E9;
    }

    .callout-box {
        margin-top: 1.5rem;
        background-color: var(--bg-input);
        border: 1px solid var(--border-color);
        border-radius: 6px;
        padding: 0.85rem 1rem;
        font-size: 0.75rem;
        color: var(--text-secondary);
        line-height: 1.45;
    }
    .callout-box code {
        background-color: rgba(56, 189, 248, 0.15);
        color: #38BDF8;
        padding: 0.1rem 0.35rem;
        border-radius: 4px;
        font-weight: 700;
    }
</style>
@endpush

@section('content')
<div class="login-wrapper">
    <div class="auth-card">
        <div class="auth-header">
            <div class="brand-badge-center">N</div>
            <h2 class="auth-title">Nexrein Printer Monitor</h2>
            <p class="auth-subtitle">Enterprise Print Accounting &amp; Spooler Management</p>
        </div>

        @if (request('email'))
            <div style="background: rgba(16, 185, 129, 0.12); border: 1px solid rgba(16, 185, 129, 0.3); border-radius: 6px; padding: 0.75rem 0.95rem; margin-bottom: 1.25rem; font-size: 0.78rem; color: #34D399; line-height: 1.45;">
                🎉 <strong>Workstation Linked Successfully!</strong><br>
                Your account for <strong>{{ request('email') }}</strong> is ready. Sign in with default password <code>admin</code>.
            </div>
        @endif

        @if ($errors->any())
            <div class="alert alert-danger" style="margin-bottom: 1.25rem;">
                <div>
                    @foreach ($errors->all() as $error)
                        <p>{{ $error }}</p>
                    @endforeach
                </div>
            </div>
        @endif

        <form action="{{ route('login.post') }}" method="POST">
            @csrf

            <div class="form-group">
                <label class="form-label" for="email">User Email Address</label>
                <input type="email" id="email" name="email" class="form-control" placeholder="user@company.com" value="{{ old('email', request('email')) }}" required autofocus>
            </div>

            <div class="form-group">
                <label class="form-label" for="password">Password</label>
                <input type="password" id="password" name="password" class="form-control" placeholder="••••••••" required>
            </div>

            <div class="form-options">
                <label style="display: flex; align-items: center; gap: 0.4rem; cursor: pointer; color: var(--text-secondary);">
                    <input type="checkbox" name="remember" value="1"> Remember me
                </label>
                <a href="{{ route('password.forgot') }}">Forgot Password?</a>
            </div>

            <button type="submit" class="btn-submit">
                Sign In to Dashboard
            </button>
        </form>

        <div class="callout-box">
            <strong>💡 Setup Information:</strong><br>
            During desktop installation, your email is registered automatically.<br>
            Your initial default password is <code>admin</code>. Upon first sign-in, you can update it to your personal password.
        </div>
    </div>
</div>
@endsection
