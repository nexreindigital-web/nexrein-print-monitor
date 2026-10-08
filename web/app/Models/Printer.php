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
        'server_name',
        'port_name',
        'driver_name',
        'status',
        'job_count',
        'is_default',
        'is_network',
        'last_seen_at',
    ];

    protected $casts = [
        'is_default' => 'boolean',
        'is_network' => 'boolean',
        'last_seen_at' => 'datetime',
    ];

    public function device()
    {
        return $this->belongsTo(Device::class, 'device_id', 'device_id');
    }
}
