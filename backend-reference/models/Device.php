<?php

namespace App\Models;

use Illuminate\Database\Eloquent\Model;
use Illuminate\Database\Eloquent\Relations\HasMany;

class Device extends Model
{
    protected $fillable = [
        'device_id',
        'api_key',
        'computer_name',
        'windows_user',
        'domain',
        'os_version',
        'architecture',
        'application_version',
        'ip_address',
        'mac_address',
        'status',
        'last_seen_at',
    ];

    protected $casts = [
        'last_seen_at' => 'datetime',
    ];

    public function printers(): HasMany
    {
        return $this->hasMany(Printer::class);
    }

    public function printJobs(): HasMany
    {
        return $this->hasMany(PrintJob::class);
    }
}
