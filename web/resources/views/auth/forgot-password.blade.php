@extends('layouts.portal')

@section('title', 'Forgot Password')

@push('styles')
<style>
    .auth-wrapper {
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
        background: linear-gradient(135deg, #F59E0B, #D97706);
        border-radius: 16px;
        display: flex;
        align-items: center;
        justify-content: center;
        font-size: 2rem;
        box-shadow: 0 8px 24px rgba(245, 158, 11, 0.4);
    }

    .auth-title {
        font-size: 1.5rem;
        font-weight: 800;
        margin-bottom: 0.35rem;
    }

    .auth-subtitle {
        color: var(--text-secondary);
        font-size: 0.875rem;
        line-height: 1.4;
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
    }

    .form-control:focus {
        outline: none;
        border-color: var(--accent-blue);
    }

    .btn-submit {
        width: 100%;
        padding: 0.85rem;
        font-size: 0.95rem;
        font-weight: 700;
    }

    .back-link {
        display: block;
        text-align: center;
        margin-top: 1.5rem;
        color: #9CA3AF;
        text-decoration: none;
        font-size: 0.875rem;
    }
    .back-link:hover { color: white; text-decoration: underline; }

    .reset-link-box {
        margin-top: 1.25rem;
        background: rgba(16, 185, 129, 0.1);
        border: 1px solid rgba(16, 185, 129, 0.3);
        border-radius: 8px;
        padding: 1rem;
        word-break: break-all;
        font-size: 0.85rem;
    }
</style>
@endpush

@section('content')
<div class="auth-wrapper">
    <div class="auth-card">
        <div class="auth-header">
            <div class="auth-icon">🔑</div>
            <h2 class="auth-title">Password Recovery</h2>
            <p class="auth-subtitle">Enter your registered user email address to reset your dashboard password.</p>
        </div>

        @if (session('status'))
            <div class="alert alert-success" style="margin-bottom: 1.25rem; flex-direction: column; align-items: flex-start; gap: 0.5rem;">
                <div>{{ session('status') }}</div>
                @if (session('reset_url'))
                    <div class="reset-link-box">
                        <a href="{{ session('reset_url') }}" style="color: #6EE7B7; font-weight: 700;">🔗 Click Here to Reset Password</a>
                    </div>
                @endif
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

        <form action="{{ route('password.forgot.post') }}" method="POST">
            @csrf

            <div class="form-group">
                <label class="form-label" for="email">Account Email Address</label>
                <input type="email" id="email" name="email" class="form-control" placeholder="user@company.com" value="{{ old('email') }}" required autofocus>
            </div>

            <button type="submit" class="btn btn-primary btn-submit">
                ✉️ Send Password Reset Authorization
            </button>
        </form>

        <a href="{{ route('login') }}" class="back-link">
            ← Back to Sign In
        </a>
    </div>
</div>
@endsection
