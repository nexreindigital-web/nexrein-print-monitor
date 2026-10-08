<?php

namespace App\Models;

use Illuminate\Database\Eloquent\Factories\HasFactory;
use Illuminate\Database\Eloquent\Model;

class Printer extends Model
{
    use HasFactory;

    protected $fillable = [
        'device_id',
        'name',
        'alias_name',
        'location',
        'server_name',
        'port_name',
        'driver_name',
        'status',
        'cost_per_mono_page',
        'cost_per_color_page',
        'is_active',
        'notes',
        'job_count',
        'is_default',
        'is_network',
        'last_seen_at',
    ];

    protected $casts = [
        'is_default' => 'boolean',
        'is_network' => 'boolean',
        'is_active' => 'boolean',
        'cost_per_mono_page' => 'decimal:2',
        'cost_per_color_page' => 'decimal:2',
        'last_seen_at' => 'datetime',
    ];

    public function device()
    {
        return $this->belongsTo(Device::class, 'device_id', 'device_id');
    }

    public function getDisplayNameAttribute(): string
    {
        return !empty($this->alias_name) ? $this->alias_name : $this->name;
    }
}
