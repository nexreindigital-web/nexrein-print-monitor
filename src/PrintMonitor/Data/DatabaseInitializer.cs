using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Logging;

namespace PrintMonitor.Data;

public class DatabaseInitializer
{
    private readonly string _connectionString;
    private readonly ILogger<DatabaseInitializer>? _logger;

    public DatabaseInitializer(string connectionString, ILogger<DatabaseInitializer>? logger = null)
    {
        _connectionString = connectionString;
        _logger = logger;
    }

    public void Initialize()
    {
        try
        {
            var builder = new SqliteConnectionStringBuilder(_connectionString);
            var dbPath = builder.DataSource;
            var directory = Path.GetDirectoryName(dbPath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            using var connection = new SqliteConnection(_connectionString);
            connection.Open();

            // Set SQLite performance & concurrency PRAGMAs
            using (var pragmaCmd = connection.CreateCommand())
            {
                pragmaCmd.CommandText = @"
                    PRAGMA journal_mode = WAL;
                    PRAGMA synchronous = NORMAL;
                    PRAGMA foreign_keys = ON;
                    PRAGMA busy_timeout = 5000;
                ";
                pragmaCmd.ExecuteNonQuery();
            }

            // Create tables and indexes
            using (var cmd = connection.CreateCommand())
            {
                cmd.CommandText = @"
                    CREATE TABLE IF NOT EXISTS print_jobs (
                        id INTEGER PRIMARY KEY AUTOINCREMENT,
                        remote_id INTEGER NULL,
                        job_uid TEXT NOT NULL UNIQUE,
                        job_id INTEGER NOT NULL,
                        printer_name TEXT NOT NULL,
                        printer_server TEXT NULL,
                        document_name TEXT NOT NULL,
                        username TEXT NOT NULL,
                        domain TEXT NULL,
                        computer_name TEXT NOT NULL,
                        pages INTEGER NOT NULL DEFAULT 0,
                        pages_printed INTEGER NOT NULL DEFAULT 0,
                        copies INTEGER NOT NULL DEFAULT 1,
                        color_mode TEXT NOT NULL DEFAULT 'Unknown',
                        duplex TEXT NOT NULL DEFAULT 'Unknown',
                        paper_size TEXT NOT NULL DEFAULT 'Unknown',
                        status TEXT NOT NULL DEFAULT 'Submitted',
                        error_message TEXT NULL,
                        submitted_at TEXT NOT NULL,
                        started_at TEXT NULL,
                        completed_at TEXT NULL,
                        created_at TEXT NOT NULL,
                        updated_at TEXT NOT NULL,
                        synced_at TEXT NULL,
                        sync_status TEXT NOT NULL DEFAULT 'Pending',
                        retry_count INTEGER NOT NULL DEFAULT 0
                    );

                    CREATE INDEX IF NOT EXISTS idx_print_jobs_uid ON print_jobs (job_uid);
                    CREATE INDEX IF NOT EXISTS idx_print_jobs_sync ON print_jobs (sync_status, created_at);
                    CREATE INDEX IF NOT EXISTS idx_print_jobs_printer ON print_jobs (printer_name, job_id);

                    CREATE TABLE IF NOT EXISTS printers (
                        id INTEGER PRIMARY KEY AUTOINCREMENT,
                        name TEXT NOT NULL UNIQUE,
                        server_name TEXT NULL,
                        share_name TEXT NULL,
                        port_name TEXT NULL,
                        driver_name TEXT NULL,
                        comment TEXT NULL,
                        location TEXT NULL,
                        status TEXT NOT NULL DEFAULT 'Ready',
                        status_raw INTEGER NOT NULL DEFAULT 0,
                        job_count INTEGER NOT NULL DEFAULT 0,
                        attributes INTEGER NOT NULL DEFAULT 0,
                        is_default INTEGER NOT NULL DEFAULT 0,
                        is_network INTEGER NOT NULL DEFAULT 0,
                        is_shared INTEGER NOT NULL DEFAULT 0,
                        is_active INTEGER NOT NULL DEFAULT 1,
                        first_seen_at TEXT NOT NULL,
                        last_seen_at TEXT NOT NULL
                    );

                    CREATE INDEX IF NOT EXISTS idx_printers_name ON printers (name);

                    CREATE TABLE IF NOT EXISTS computer (
                        device_id TEXT PRIMARY KEY,
                        computer_name TEXT NOT NULL,
                        windows_user TEXT NOT NULL,
                        domain TEXT NULL,
                        os_version TEXT NOT NULL,
                        os_architecture TEXT NOT NULL,
                        application_version TEXT NOT NULL,
                        ip_address TEXT NULL,
                        mac_address TEXT NULL,
                        installed_at TEXT NOT NULL,
                        last_heartbeat_at TEXT NULL,
                        is_registered INTEGER NOT NULL DEFAULT 0
                    );

                    CREATE TABLE IF NOT EXISTS sync_queue (
                        id INTEGER PRIMARY KEY AUTOINCREMENT,
                        entity_type TEXT NOT NULL,
                        entity_id TEXT NOT NULL,
                        payload_json TEXT NOT NULL,
                        attempts INTEGER NOT NULL DEFAULT 0,
                        last_attempt_at TEXT NULL,
                        status TEXT NOT NULL DEFAULT 'Pending',
                        error_message TEXT NULL,
                        created_at TEXT NOT NULL
                    );

                    CREATE INDEX IF NOT EXISTS idx_sync_queue_status ON sync_queue (status, created_at);

                    CREATE TABLE IF NOT EXISTS settings (
                        key TEXT PRIMARY KEY,
                        value TEXT NOT NULL,
                        updated_at TEXT NOT NULL
                    );
                ";
                cmd.ExecuteNonQuery();
            }

            _logger?.LogInformation("SQLite database initialized successfully at {Path}", dbPath);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Failed to initialize SQLite database at {ConnectionString}", _connectionString);
            throw;
        }
    }
}
