<?php

namespace App\Models;

use Illuminate\Database\Eloquent\Factories\HasFactory;
use Illuminate\Database\Eloquent\Model;

class Device extends Model
{
    use HasFactory;

    protected $fillable = [
        'device_id',
        'api_token',
        'user_email',
        'computer_name',
        'shop_name',
        'ip_address',
        'mac_address',
        'os_version',
        'app_version',
        'status',
        'software_password',
        'pending_password_update',
        'last_heartbeat_at',
    ];

    protected $casts = [
        'last_heartbeat_at' => 'datetime',
    ];

    public function printers()
    {
        return $this->hasMany(Printer::class, 'device_id', 'device_id');
    }

    public function printJobs()
    {
        return $this->hasMany(PrintJob::class, 'device_id', 'device_id');
    }

    public function isOnline(): bool
    {
        if (!$this->last_heartbeat_at) {
            return false;
        }
        return $this->last_heartbeat_at->diffInMinutes(now()) < 5;
    }
}
