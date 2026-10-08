@extends('layouts.portal')

@section('title', 'Change Account Password')

@push('styles')
<style>
    .change-wrapper {
        max-width: 580px;
        margin: 2rem auto;
    }

    .change-card {
        background: var(--bg-card);
        border: 1px solid var(--border-color);
        border-radius: 16px;
        padding: 2.25rem;
        box-shadow: 0 10px 30px rgba(0, 0, 0, 0.35);
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
</style>
@endpush

@section('content')
<div class="change-wrapper">
    @if ($isForced)
        <div class="alert alert-warning" style="margin-bottom: 1.5rem;">
            <div>
                <strong>⚠️ Security Update Required:</strong><br>
                Your account was registered with the default password <code>admin</code>.
                You must set a new secure password before proceeding to the dashboard.
            </div>
        </div>
    @endif

    <div class="change-card">
        <div style="display: flex; align-items: center; gap: 0.75rem; margin-bottom: 1.5rem;">
            <div style="font-size: 1.75rem;">🔐</div>
            <div>
                <h2 style="font-size: 1.35rem; font-weight: 800;">
                    {{ $isForced ? 'Set Your Personal Password' : 'Change Account Password' }}
                </h2>
                <p style="font-size: 0.85rem; color: var(--text-secondary);">
                    Keep your cloud print audit portal secure with a strong password.
                </p>
            </div>
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

        <form action="{{ route('password.update') }}" method="POST">
            @csrf

            <div class="form-group">
                <label class="form-label" for="current_password">
                    Current Password {{ $isForced ? '(Default is admin)' : '' }}
                </label>
                <input type="password" id="current_password" name="current_password" class="form-control" placeholder="••••••••" required autofocus>
            </div>

            <div class="form-group">
                <label class="form-label" for="password">New Password</label>
                <input type="password" id="password" name="password" class="form-control" placeholder="••••••••" required>
            </div>

            <div class="form-group">
                <label class="form-label" for="password_confirmation">Confirm New Password</label>
                <input type="password" id="password_confirmation" name="password_confirmation" class="form-control" placeholder="••••••••" required>
            </div>

            <div style="display: flex; gap: 0.75rem; margin-top: 1.75rem;">
                <button type="submit" class="btn btn-primary" style="flex: 1; padding: 0.8rem;">
                    💾 Save &amp; Update Password
                </button>
                @if (!$isForced)
                    <a href="{{ route('dashboard') }}" class="btn btn-secondary">Cancel</a>
                @endif
            </div>
        </form>
    </div>
</div>
@endsection
