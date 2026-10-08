@extends('layouts.portal')

@section('title', 'Login')

@push('styles')
<style>
    .login-wrapper {
        min-height: 80vh;
        display: flex;
        align-items: center;
        justify-content: center;
    }

    .auth-card {
        background: var(--bg-card);
        border: 1px solid var(--border-color);
        border-radius: 16px;
        padding: 2.5rem;
        width: 100%;
        max-width: 460px;
        box-shadow: 0 20px 40px rgba(0, 0, 0, 0.45);
    }

    .auth-header {
        text-align: center;
        margin-bottom: 2rem;
    }

    .auth-icon {
        width: 60px;
        height: 60px;
        margin: 0 auto 1.25rem;
        background: linear-gradient(135deg, #3B82F6, #1D4ED8);
        border-radius: 16px;
        display: flex;
        align-items: center;
        justify-content: center;
        font-size: 2rem;
        box-shadow: 0 8px 24px rgba(59, 130, 246, 0.4);
    }

    .auth-title {
        font-size: 1.5rem;
        font-weight: 800;
        margin-bottom: 0.35rem;
    }

    .auth-subtitle {
        color: var(--text-secondary);
        font-size: 0.875rem;
    }

    .form-group {
        margin-bottom: 1.25rem;
    }

    .form-label {
        display: block;
        font-size: 0.85rem;
        font-weight: 600;
        margin-bottom: 0.4rem;
        color: var(--text-secondary);
    }

    .form-control {
        width: 100%;
        background: var(--bg-input);
        border: 1px solid var(--border-light);
        border-radius: 8px;
        padding: 0.75rem 1rem;
        color: white;
        font-size: 0.95rem;
        transition: border-color 0.2s;
    }

    .form-control:focus {
        outline: none;
        border-color: var(--accent-blue);
        box-shadow: 0 0 0 3px rgba(59, 130, 246, 0.2);
    }

    .form-options {
        display: flex;
        align-items: center;
        justify-content: space-between;
        margin-bottom: 1.5rem;
        font-size: 0.85rem;
    }

    .form-options a {
        color: #60A5FA;
        text-decoration: none;
    }
    .form-options a:hover {
        text-decoration: underline;
    }

    .btn-submit {
        width: 100%;
        padding: 0.85rem;
        font-size: 0.95rem;
        font-weight: 700;
    }

    .callout-box {
        margin-top: 1.75rem;
        background: rgba(59, 130, 246, 0.08);
        border: 1px dashed rgba(59, 130, 246, 0.3);
        border-radius: 10px;
        padding: 1rem;
        font-size: 0.825rem;
        color: #93C5FD;
        line-height: 1.45;
    }
</style>
@endpush

@section('content')
<div class="login-wrapper">
    <div class="auth-card">
        <div class="auth-header">
            <div class="auth-icon">🖨️</div>
            <h2 class="auth-title">Nexrein Printer Monitor</h2>
            <p class="auth-subtitle">Remote Print Accounting &amp; Workstation Portal</p>
        </div>

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
                <input type="email" id="email" name="email" class="form-control" placeholder="user@company.com" value="{{ old('email') }}" required autofocus>
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

            <button type="submit" class="btn btn-primary btn-submit">
                🔐 Sign In to Dashboard
            </button>
        </form>

        <div class="callout-box">
            <strong>💡 First-Time User Instructions:</strong><br>
            During desktop installation, your email address is registered automatically.
            Your initial default password is <code>admin</code>. Upon first sign-in, you will be prompted to change it to your own personal password.
        </div>
    </div>
</div>
@endsection
