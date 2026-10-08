<?php

namespace App\Models;

use Illuminate\Database\Eloquent\Model;
use Illuminate\Database\Eloquent\Relations\BelongsTo;
use Illuminate\Database\Eloquent\Relations\HasMany;

class Printer extends Model
{
    protected $fillable = [
        'device_id',
        'name',
        'server_name',
        'share_name',
        'port_name',
        'driver_name',
        'location',
        'status',
        'is_network',
        'is_shared',
        'is_active',
    ];

    protected $casts = [
        'is_network' => 'boolean',
        'is_shared' => 'boolean',
        'is_active' => 'boolean',
    ];

    public function device(): BelongsTo
    {
        return $this->belongsTo(Device::class);
    }

    public function printJobs(): HasMany
    {
        return $this->hasMany(PrintJob::class);
    }
}
