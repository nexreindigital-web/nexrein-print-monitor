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
        User::updateOrCreate(
            ['email' => 'admin@nexreindigital.co.ke'],
            [
                'name'                 => 'Super Admin',
                'password'             => Hash::make('admin'), // Default initial password 'admin'
                'shop_name'            => 'Headquarters',
                'must_change_password' => true,
            ]
        );
    }
}
