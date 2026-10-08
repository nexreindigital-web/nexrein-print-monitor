@extends('layouts.portal')

@section('title', 'Forgot Password')

@push('styles')
<style>
    .auth-wrapper {
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

    .back-link {
        display: block;
        text-align: center;
        margin-top: 1.25rem;
        color: var(--text-secondary);
        text-decoration: none;
        font-size: 0.78rem;
    }
    .back-link:hover { color: var(--text-primary); text-decoration: underline; }
</style>
@endpush

@section('content')
<div class="auth-wrapper">
    <div class="auth-card">
        <div class="auth-header">
            <div class="brand-badge-center">N</div>
            <h2 class="auth-title">Reset Your Password</h2>
            <p class="auth-subtitle">
                Enter your registered administrator email to receive a password reset authorization.
            </p>
        </div>

        @if (session('status'))
            <div class="alert alert-success" style="margin-bottom: 1.25rem;">
                <div>
                    {{ session('status') }}
                    @if (session('reset_url'))
                        <div style="margin-top: 0.75rem; word-break: break-all;">
                            <a href="{{ session('reset_url') }}" class="btn-submit" style="display: block; text-align: center; text-decoration: none; padding: 0.6rem; font-size: 0.8rem;">
                                ➔ Click Here to Set New Password
                            </a>
                        </div>
                    @endif
                </div>
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

        <form action="{{ route('password.email') }}" method="POST">
            @csrf

            <div class="form-group">
                <label class="form-label" for="email">Account Email Address</label>
                <input type="email" id="email" name="email" class="form-control" placeholder="user@company.com" value="{{ old('email') }}" required autofocus>
            </div>

            <button type="submit" class="btn-submit">
                Request Password Reset
            </button>
        </form>

        <a href="{{ route('login') }}" class="back-link">
            ← Return to Sign In
        </a>
    </div>
</div>
@endsection
