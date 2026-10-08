<?php

namespace Database\Seeders;

use App\Models\User;
use Illuminate\Database\Seeder;
use Illuminate\Support\Facades\Hash;

class DatabaseSeeder extends Seeder
{
    /**
     * Seed the application's database.
     */
    public function run(): void
    {
        // Clean fresh installation: No dummy users, jobs, or mock data.
        // Client user accounts and workstation devices are provisioned dynamically 
        // when the software is installed and authenticated via API.
    }
}
