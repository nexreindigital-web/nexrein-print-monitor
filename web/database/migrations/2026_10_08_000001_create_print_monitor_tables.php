<?php

use Illuminate\Database\Migrations\Migration;
use Illuminate\Database\Schema\Blueprint;
use Illuminate\Support\Facades\Schema;

return new class extends Migration
{
    public function up(): void
    {
        Schema::table('users', function (Blueprint $table) {
            if (!Schema::hasColumn('users', 'must_change_password')) {
                $table->boolean('must_change_password')->default(true);
            }
            if (!Schema::hasColumn('users', 'shop_name')) {
                $table->string('shop_name')->nullable();
            }
        });

        if (!Schema::hasTable('devices')) {
            Schema::create('devices', function (Blueprint $table) {
                $table->id();
                $table->string('device_id')->unique();
                $table->string('user_email')->index();
                $table->string('computer_name');
                $table->string('shop_name')->default('Main Shop');
                $table->string('ip_address')->nullable();
                $table->string('mac_address')->nullable();
                $table->string('os_version')->nullable();
                $table->string('app_version')->default('2.0.0');
                $table->string('status')->default('Online');
                $table->string('software_password')->nullable();
                $table->string('pending_password_update')->nullable();
                $table->timestamp('last_heartbeat_at')->nullable();
                $table->timestamps();
            });
        }

        if (!Schema::hasTable('printers')) {
            Schema::create('printers', function (Blueprint $table) {
                $table->id();
                $table->string('device_id')->index();
                $table->string('name');
                $table->string('server_name')->nullable();
                $table->string('port_name')->nullable();
                $table->string('driver_name')->nullable();
                $table->string('status')->default('Ready');
                $table->integer('job_count')->default(0);
                $table->boolean('is_default')->default(false);
                $table->boolean('is_network')->default(false);
                $table->timestamp('last_seen_at')->nullable();
                $table->timestamps();

                $table->unique(['device_id', 'name']);
            });
        }

        if (!Schema::hasTable('print_jobs')) {
            Schema::create('print_jobs', function (Blueprint $table) {
                $table->id();
                $table->string('device_id')->index();
                $table->string('shop_name')->nullable()->index();
                $table->string('computer_name')->nullable()->index();
                $table->string('user_email')->nullable()->index();
                $table->string('job_uid')->unique();
                $table->integer('job_id');
                $table->string('printer_name')->index();
                $table->string('document_name');
                $table->string('username');
                $table->integer('pages')->default(0);
                $table->integer('pages_printed')->default(0);
                $table->integer('copies')->default(1);
                $table->integer('total_pages_calculated')->default(0);
                $table->string('color_mode')->default('Unknown')->index();
                $table->string('duplex')->default('Unknown');
                $table->string('paper_size')->default('Unknown');
                $table->string('status')->default('Completed');
                $table->timestamp('submitted_at')->nullable()->index();
                $table->timestamps();
            });
        }
    }

    public function down(): void
    {
        Schema::dropIfExists('print_jobs');
        Schema::dropIfExists('printers');
        Schema::dropIfExists('devices');
    }
};
