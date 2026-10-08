<?php

namespace App\Models;

use Illuminate\Database\Eloquent\Factories\HasFactory;
use Illuminate\Database\Eloquent\Model;

class PrintJob extends Model
{
    use HasFactory;

    protected $fillable = [
        'device_id',
        'shop_name',
        'computer_name',
        'user_email',
        'job_uid',
        'job_id',
        'printer_name',
        'document_name',
        'username',
        'pages',
        'pages_printed',
        'copies',
        'total_pages_calculated',
        'color_mode',
        'duplex',
        'paper_size',
        'status',
        'submitted_at',
    ];

    protected $casts = [
        'submitted_at' => 'datetime',
        'pages' => 'integer',
        'pages_printed' => 'integer',
        'copies' => 'integer',
        'total_pages_calculated' => 'integer',
    ];

    public function device()
    {
        return $this->belongsTo(Device::class, 'device_id', 'device_id');
    }

    public function getDocumentIconAttribute(): string
    {
        $ext = strtolower(pathinfo($this->document_name, PATHINFO_EXTENSION));
        return match ($ext) {
            'pdf' => '📕',
            'docx', 'doc' => '📘',
            'xlsx', 'xls', 'csv' => '📗',
            'pptx', 'ppt' => '📙',
            'pub' => '📐',
            'txt', 'rtf' => '📄',
            'jpg', 'jpeg', 'png', 'bmp', 'gif' => '🖼️',
            default => '📋',
        };
    }
}
