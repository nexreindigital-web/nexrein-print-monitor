@extends('layouts.portal')

@section('title', 'Set New Password')

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
        background: linear-gradient(135deg, #10B981, #059669);
        border-radius: 16px;
        display: flex;
        align-items: center;
        justify-content: center;
        font-size: 2rem;
        box-shadow: 0 8px 24px rgba(16, 185, 129, 0.4);
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
</style>
@endpush

@section('content')
<div class="auth-wrapper">
    <div class="auth-card">
        <div class="auth-header">
            <div class="auth-icon">🛡️</div>
            <h2 class="auth-title">Set New Password</h2>
            <p class="auth-subtitle">Create a secure personal password for your dashboard account.</p>
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

        <form action="{{ route('password.reset.post') }}" method="POST">
            @csrf
            <input type="hidden" name="token" value="{{ $token }}">

            <div class="form-group">
                <label class="form-label" for="email">User Email Address</label>
                <input type="email" id="email" name="email" class="form-control" value="{{ $email ?? old('email') }}" required>
            </div>

            <div class="form-group">
                <label class="form-label" for="password">New Password</label>
                <input type="password" id="password" name="password" class="form-control" placeholder="••••••••" required autofocus>
            </div>

            <div class="form-group">
                <label class="form-label" for="password_confirmation">Confirm New Password</label>
                <input type="password" id="password_confirmation" name="password_confirmation" class="form-control" placeholder="••••••••" required>
            </div>

            <button type="submit" class="btn btn-primary btn-submit">
                💾 Save Password &amp; Sign In
            </button>
        </form>
    </div>
</div>
@endsection
