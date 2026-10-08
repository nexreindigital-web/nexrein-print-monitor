@extends('layouts.portal')

@section('title', 'Change Account Password')

@push('styles')
<style>
    .change-wrapper {
        max-width: 520px;
        margin: 2rem auto;
    }

    .change-card {
        background-color: var(--bg-card);
        border: 1px solid var(--border-color);
        border-radius: 8px;
        padding: 2rem 2.25rem;
        box-shadow: 0 4px 20px rgba(0, 0, 0, 0.25);
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

    .btn-save-pw {
        background-color: #38BDF8;
        color: #0F172A;
        font-weight: 700;
        border: none;
        padding: 0.75rem 1.25rem;
        border-radius: 6px;
        cursor: pointer;
        flex: 1;
        transition: background-color 0.15s;
    }
    .btn-save-pw:hover {
        background-color: #0EA5E9;
    }
</style>
@endpush

@section('content')
<div class="change-wrapper">
    @if ($isForced)
        <div class="alert alert-warning">
            <div>
                <strong>⚠️ Security Update Required:</strong><br>
                Your account was created with the default password <code>admin</code>.<br>
                Please set your secure password to continue. It will also update your desktop software automatically!
            </div>
        </div>
    @endif

    <div class="change-card">
        <div style="display: flex; align-items: center; gap: 0.85rem; margin-bottom: 1.5rem;">
            <div style="width: 38px; height: 38px; background-color: #38BDF8; border-radius: 6px; display: flex; align-items: center; justify-content: center; color: white; font-size: 1.25rem; font-weight: bold;">
                🔐
            </div>
            <div>
                <h2 style="font-size: 1.2rem; font-weight: 700; color: var(--text-primary);">
                    {{ $isForced ? 'Set Your Personal Password' : 'Change Account Password' }}
                </h2>
                <p style="font-size: 0.76rem; color: var(--text-secondary); margin-top: 2px;">
                    Updates both your online dashboard &amp; connected desktop installations automatically.
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
                <button type="submit" class="btn-save-pw">
                    💾 Save &amp; Sync Password
                </button>
                @if (!$isForced)
                    <a href="{{ route('dashboard') }}" class="btn-section" style="padding: 0.75rem 1rem;">Cancel</a>
                @endif
            </div>
        </form>
    </div>
</div>
@endsection
