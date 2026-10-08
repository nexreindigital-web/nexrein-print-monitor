<?php

use Illuminate\Database\Migrations\Migration;
use Illuminate\Database\Schema\Blueprint;
use Illuminate\Support\Facades\Schema;

return new class extends Migration
{
    public function up(): void
    {
        Schema::create('devices', function (Blueprint $table) {
            $table->id();
            $table->uuid('device_id')->unique();
            $table->string('api_key', 64)->nullable()->index();
            $table->string('computer_name');
            $table->string('windows_user');
            $table->string('domain')->nullable();
            $table->string('os_version');
            $table->string('architecture')->nullable();
            $table->string('application_version')->default('1.0.0');
            $table->string('ip_address')->nullable();
            $table->string('mac_address')->nullable();
            $table->string('status')->default('offline'); // online, offline
            $table->timestamp('last_seen_at')->nullable();
            $table->timestamps();
        });

        Schema::create('printers', function (Blueprint $table) {
            $table->id();
            $table->foreignId('device_id')->constrained('devices')->cascadeOnDelete();
            $table->string('name');
            $table->string('server_name')->nullable();
            $table->string('share_name')->nullable();
            $table->string('port_name')->nullable();
            $table->string('driver_name')->nullable();
            $table->string('location')->nullable();
            $table->string('status')->default('Ready');
            $table->boolean('is_network')->default(false);
            $table->boolean('is_shared')->default(false);
            $table->boolean('is_active')->default(true);
            $table->timestamps();

            $table->unique(['device_id', 'name']);
        });

        Schema::create('print_jobs', function (Blueprint $table) {
            $table->id();
            $table->foreignId('device_id')->constrained('devices')->cascadeOnDelete();
            $table->foreignId('printer_id')->nullable()->constrained('printers')->nullOnDelete();
            $table->string('job_uid')->unique(); // {device_id}_{printer}_{job_id}_{timestamp}
            $table->unsignedInteger('job_id');
            $table->string('printer_name');
            $table->string('document_name');
            $table->string('username');
            $table->string('domain')->nullable();
            $table->string('computer_name');
            $table->unsignedInteger('pages')->default(1);
            $table->unsignedInteger('pages_printed')->default(0);
            $table->unsignedInteger('copies')->default(1);
            $table->string('color_mode')->default('Unknown'); // Monochrome, Color
            $table->string('duplex')->default('Unknown');     // Simplex, DuplexLongEdge, DuplexShortEdge
            $table->string('paper_size')->default('Unknown');
            $table->string('status')->default('Completed');   // Submitted, Printing, Completed, Cancelled, Error
            $table->text('error_message')->nullable();
            $table->timestamp('submitted_at');
            $table->timestamp('started_at')->nullable();
            $table->timestamp('completed_at')->nullable();
            $table->timestamps();

            $table->index(['device_id', 'printer_name']);
            $table->index(['username']);
            $table->index(['status']);
            $table->index(['submitted_at']);
        });

        Schema::create('sync_logs', function (Blueprint $table) {
            $table->id();
            $table->foreignId('device_id')->constrained('devices')->cascadeOnDelete();
            $table->unsignedInteger('batch_count')->default(0);
            $table->unsignedInteger('accepted_count')->default(0);
            $table->unsignedInteger('duplicate_count')->default(0);
            $table->string('ip_address')->nullable();
            $table->timestamps();
        });
    }

    public function down(): void
    {
        Schema::dropIfExists('sync_logs');
        Schema::dropIfExists('print_jobs');
        Schema::dropIfExists('printers');
        Schema::dropIfExists('devices');
    }
};
