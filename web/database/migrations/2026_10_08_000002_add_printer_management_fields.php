<?php

use Illuminate\Database\Migrations\Migration;
use Illuminate\Database\Schema\Blueprint;
use Illuminate\Support\Facades\Schema;

return new class extends Migration
{
    public function up(): void
    {
        Schema::table('printers', function (Blueprint $table) {
            if (!Schema::hasColumn('printers', 'alias_name')) {
                $table->string('alias_name')->nullable()->after('name');
            }
            if (!Schema::hasColumn('printers', 'location')) {
                $table->string('location')->nullable()->after('alias_name');
            }
            if (!Schema::hasColumn('printers', 'cost_per_mono_page')) {
                $table->decimal('cost_per_mono_page', 8, 2)->default(0.00)->after('status');
            }
            if (!Schema::hasColumn('printers', 'cost_per_color_page')) {
                $table->decimal('cost_per_color_page', 8, 2)->default(0.00)->after('cost_per_mono_page');
            }
            if (!Schema::hasColumn('printers', 'is_active')) {
                $table->boolean('is_active')->default(true)->after('cost_per_color_page');
            }
            if (!Schema::hasColumn('printers', 'notes')) {
                $table->text('notes')->nullable()->after('is_active');
            }
        });
    }

    public function down(): void
    {
        Schema::table('printers', function (Blueprint $table) {
            $table->dropColumn(['alias_name', 'location', 'cost_per_mono_page', 'cost_per_color_page', 'is_active', 'notes']);
        });
    }
};
