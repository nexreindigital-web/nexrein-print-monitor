<?php

namespace App\Models;

use Illuminate\Database\Eloquent\Model;
use Illuminate\Database\Eloquent\Relations\BelongsTo;

class PrintJob extends Model
{
    protected $fillable = [
        'device_id',
        'printer_id',
        'job_uid',
        'job_id',
        'printer_name',
        'document_name',
        'username',
        'domain',
        'computer_name',
        'pages',
        'pages_printed',
        'copies',
        'color_mode',
        'duplex',
        'paper_size',
        'status',
        'error_message',
        'submitted_at',
        'started_at',
        'completed_at',
    ];

    protected $casts = [
        'pages' => 'integer',
        'pages_printed' => 'integer',
        'copies' => 'integer',
        'submitted_at' => 'datetime',
        'started_at' => 'datetime',
        'completed_at' => 'datetime',
    ];

    public function device(): BelongsTo
    {
        return $this->belongsTo(Device::class);
    }

    public function printer(): BelongsTo
    {
        return $this->belongsTo(Printer::class);
    }
}
