using System.Data;
using System.Globalization;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Logging;
using PrintMonitor.Models;

namespace PrintMonitor.Data;

public class PrintMonitorDbContext
{
    private readonly string _connectionString;
    private readonly ILogger<PrintMonitorDbContext>? _logger;

    public PrintMonitorDbContext(string connectionString, ILogger<PrintMonitorDbContext>? logger = null)
    {
        _connectionString = connectionString;
        _logger = logger;
    }

    private SqliteConnection CreateConnection()
    {
        var conn = new SqliteConnection(_connectionString);
        conn.Open();
        return conn;
    }

    #region Print Jobs

    public long SaveOrUpdateJob(PrintJob job)
    {
        using var conn = CreateConnection();
        using var tx = conn.BeginTransaction();

        try
        {
            // 1. Check if job exists by JobUid
            long existingId = 0;
            string? currentStatus = null;
            using (var checkCmd = conn.CreateCommand())
            {
                checkCmd.Transaction = tx;
                checkCmd.CommandText = "SELECT id, status FROM print_jobs WHERE job_uid = @uid LIMIT 1";
                checkCmd.Parameters.AddWithValue("@uid", job.JobUid);
                using var reader = checkCmd.ExecuteReader();
                if (reader.Read())
                {
                    existingId = reader.GetInt64(0);
                    currentStatus = reader.IsDBNull(1) ? null : reader.GetString(1);
                }
            }

            var nowStr = DateTime.UtcNow.ToString("o", CultureInfo.InvariantCulture);

            if (existingId > 0)
            {
                // Update existing job
                using var updateCmd = conn.CreateCommand();
                updateCmd.Transaction = tx;
                updateCmd.CommandText = @"
                    UPDATE print_jobs
                    SET pages = CASE WHEN @pages > 0 THEN @pages ELSE pages END,
                        pages_printed = CASE WHEN @pages_printed >= pages_printed THEN @pages_printed ELSE pages_printed END,
                        status = @status,
                        started_at = COALESCE(@started_at, started_at),
                        completed_at = COALESCE(@completed_at, completed_at),
                        error_message = COALESCE(@error_message, error_message),
                        updated_at = @updated_at,
                        sync_status = CASE WHEN sync_status = 'Synced' AND status != @status THEN 'Pending' ELSE sync_status END
                    WHERE id = @id;
                ";

                updateCmd.Parameters.AddWithValue("@id", existingId);
                updateCmd.Parameters.AddWithValue("@pages", job.Pages);
                updateCmd.Parameters.AddWithValue("@pages_printed", job.PagesPrinted);
                updateCmd.Parameters.AddWithValue("@status", job.Status);
                updateCmd.Parameters.AddWithValue("@started_at", (object?)job.StartedAt?.ToString("o", CultureInfo.InvariantCulture) ?? DBNull.Value);
                updateCmd.Parameters.AddWithValue("@completed_at", (object?)job.CompletedAt?.ToString("o", CultureInfo.InvariantCulture) ?? DBNull.Value);
                updateCmd.Parameters.AddWithValue("@error_message", (object?)job.ErrorMessage ?? DBNull.Value);
                updateCmd.Parameters.AddWithValue("@updated_at", nowStr);

                updateCmd.ExecuteNonQuery();
                tx.Commit();
                job.Id = existingId;
                return existingId;
            }
            else
            {
                // Insert new job
                using var insertCmd = conn.CreateCommand();
                insertCmd.Transaction = tx;
                insertCmd.CommandText = @"
                    INSERT INTO print_jobs (
                        remote_id, job_uid, job_id, printer_name, printer_server,
                        document_name, username, domain, computer_name,
                        pages, pages_printed, copies, color_mode, duplex, paper_size,
                        status, error_message, submitted_at, started_at, completed_at,
                        created_at, updated_at, synced_at, sync_status, retry_count
                    ) VALUES (
                        @remote_id, @job_uid, @job_id, @printer_name, @printer_server,
                        @document_name, @username, @domain, @computer_name,
                        @pages, @pages_printed, @copies, @color_mode, @duplex, @paper_size,
                        @status, @error_message, @submitted_at, @started_at, @completed_at,
                        @created_at, @updated_at, @synced_at, @sync_status, @retry_count
                    );
                    SELECT last_insert_rowid();
                ";

                insertCmd.Parameters.AddWithValue("@remote_id", (object?)job.RemoteId ?? DBNull.Value);
                insertCmd.Parameters.AddWithValue("@job_uid", job.JobUid);
                insertCmd.Parameters.AddWithValue("@job_id", job.JobId);
                insertCmd.Parameters.AddWithValue("@printer_name", job.PrinterName);
                insertCmd.Parameters.AddWithValue("@printer_server", (object?)job.PrinterServer ?? DBNull.Value);
                insertCmd.Parameters.AddWithValue("@document_name", job.DocumentName);
                insertCmd.Parameters.AddWithValue("@username", job.Username);
                insertCmd.Parameters.AddWithValue("@domain", (object?)job.Domain ?? DBNull.Value);
                insertCmd.Parameters.AddWithValue("@computer_name", job.ComputerName);
                insertCmd.Parameters.AddWithValue("@pages", job.Pages);
                insertCmd.Parameters.AddWithValue("@pages_printed", job.PagesPrinted);
                insertCmd.Parameters.AddWithValue("@copies", job.Copies);
                insertCmd.Parameters.AddWithValue("@color_mode", job.ColorMode);
                insertCmd.Parameters.AddWithValue("@duplex", job.Duplex);
                insertCmd.Parameters.AddWithValue("@paper_size", job.PaperSize);
                insertCmd.Parameters.AddWithValue("@status", job.Status);
                insertCmd.Parameters.AddWithValue("@error_message", (object?)job.ErrorMessage ?? DBNull.Value);
                insertCmd.Parameters.AddWithValue("@submitted_at", job.SubmittedAt.ToString("o", CultureInfo.InvariantCulture));
                insertCmd.Parameters.AddWithValue("@started_at", (object?)job.StartedAt?.ToString("o", CultureInfo.InvariantCulture) ?? DBNull.Value);
                insertCmd.Parameters.AddWithValue("@completed_at", (object?)job.CompletedAt?.ToString("o", CultureInfo.InvariantCulture) ?? DBNull.Value);
                insertCmd.Parameters.AddWithValue("@created_at", nowStr);
                insertCmd.Parameters.AddWithValue("@updated_at", nowStr);
                insertCmd.Parameters.AddWithValue("@synced_at", (object?)job.SyncedAt?.ToString("o", CultureInfo.InvariantCulture) ?? DBNull.Value);
                insertCmd.Parameters.AddWithValue("@sync_status", job.SyncStatus);
                insertCmd.Parameters.AddWithValue("@retry_count", job.RetryCount);

                var insertedId = (long)insertCmd.ExecuteScalar()!;
                tx.Commit();
                job.Id = insertedId;
                return insertedId;
            }
        }
        catch (Exception ex)
        {
            tx.Rollback();
            _logger?.LogError(ex, "Error saving/updating job UID {JobUid}", job.JobUid);
            throw;
        }
    }

    public List<PrintJob> GetPendingSyncJobs(int limit = 50)
    {
        var list = new List<PrintJob>();
        using var conn = CreateConnection();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = @"
            SELECT id, remote_id, job_uid, job_id, printer_name, printer_server,
                   document_name, username, domain, computer_name,
                   pages, pages_printed, copies, color_mode, duplex, paper_size,
                   status, error_message, submitted_at, started_at, completed_at,
                   created_at, updated_at, synced_at, sync_status, retry_count
            FROM print_jobs
            WHERE sync_status != 'Synced'
            ORDER BY id ASC
            LIMIT @limit;
        ";
        cmd.Parameters.AddWithValue("@limit", limit);

        using var reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            list.Add(ReadJob(reader));
        }

        return list;
    }

    public void MarkJobsSynced(IEnumerable<(long clientId, long remoteId)> syncResults)
    {
        using var conn = CreateConnection();
        using var tx = conn.BeginTransaction();
        try
        {
            var nowStr = DateTime.UtcNow.ToString("o", CultureInfo.InvariantCulture);
            foreach (var (clientId, remoteId) in syncResults)
            {
                using var cmd = conn.CreateCommand();
                cmd.Transaction = tx;
                cmd.CommandText = @"
                    UPDATE print_jobs
                    SET sync_status = 'Synced',
                        remote_id = @remoteId,
                        synced_at = @now,
                        updated_at = @now
                    WHERE id = @clientId;
                ";
                cmd.Parameters.AddWithValue("@remoteId", remoteId);
                cmd.Parameters.AddWithValue("@now", nowStr);
                cmd.Parameters.AddWithValue("@clientId", clientId);
                cmd.ExecuteNonQuery();
            }
            tx.Commit();
        }
        catch
        {
            tx.Rollback();
            throw;
        }
    }

    public void IncrementRetryCount(IEnumerable<long> clientIds, string? errorMessage = null)
    {
        using var conn = CreateConnection();
        using var tx = conn.BeginTransaction();
        try
        {
            var nowStr = DateTime.UtcNow.ToString("o", CultureInfo.InvariantCulture);
            foreach (var id in clientIds)
            {
                using var cmd = conn.CreateCommand();
                cmd.Transaction = tx;
                cmd.CommandText = @"
                    UPDATE print_jobs
                    SET retry_count = retry_count + 1,
                        sync_status = 'Failed',
                        error_message = COALESCE(@error, error_message),
                        updated_at = @now
                    WHERE id = @id;
                ";
                cmd.Parameters.AddWithValue("@id", id);
                cmd.Parameters.AddWithValue("@error", (object?)errorMessage ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@now", nowStr);
                cmd.ExecuteNonQuery();
            }
            tx.Commit();
        }
        catch
        {
            tx.Rollback();
            throw;
        }
    }

    public int GetPendingSyncCount()
    {
        using var conn = CreateConnection();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT COUNT(*) FROM print_jobs WHERE sync_status != 'Synced';";
        return Convert.ToInt32(cmd.ExecuteScalar());
    }

    public int GetTotalJobCount()
    {
        using var conn = CreateConnection();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT COUNT(*) FROM print_jobs;";
        return Convert.ToInt32(cmd.ExecuteScalar());
    }

    public List<PrintJob> GetRecentJobs(int count = 20)
    {
        var list = new List<PrintJob>();
        using var conn = CreateConnection();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = @"
            SELECT id, remote_id, job_uid, job_id, printer_name, printer_server,
                   document_name, username, domain, computer_name,
                   pages, pages_printed, copies, color_mode, duplex, paper_size,
                   status, error_message, submitted_at, started_at, completed_at,
                   created_at, updated_at, synced_at, sync_status, retry_count
            FROM print_jobs
            ORDER BY id DESC
            LIMIT @limit;
        ";
        cmd.Parameters.AddWithValue("@limit", count);

        using var reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            list.Add(ReadJob(reader));
        }

        return list;
    }

    public (int totalJobs, int totalPages, int colorPages, int monoPages, int pendingSync) GetPrintJobSummaryStats()
    {
        using var conn = CreateConnection();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = @"
            SELECT 
                COUNT(*),
                COALESCE(SUM(CASE WHEN pages_printed > 0 THEN pages_printed ELSE pages * copies END), 0),
                COALESCE(SUM(CASE WHEN color_mode = 'Color' THEN (CASE WHEN pages_printed > 0 THEN pages_printed ELSE pages * copies END) ELSE 0 END), 0),
                COALESCE(SUM(CASE WHEN color_mode != 'Color' THEN (CASE WHEN pages_printed > 0 THEN pages_printed ELSE pages * copies END) ELSE 0 END), 0),
                COALESCE(SUM(CASE WHEN sync_status != 'Synced' THEN 1 ELSE 0 END), 0)
            FROM print_jobs;
        ";
        using var reader = cmd.ExecuteReader();
        if (reader.Read())
        {
            return (
                reader.GetInt32(0),
                Convert.ToInt32(reader.GetInt64(1)),
                Convert.ToInt32(reader.GetInt64(2)),
                Convert.ToInt32(reader.GetInt64(3)),
                reader.GetInt32(4)
            );
        }
        return (0, 0, 0, 0, 0);
    }

    public (int todayJobs, int todayPages, int todayColor, int todayMono, int totalJobs, int totalPages, int totalColor, int totalMono, int pendingSync) GetDetailedJobStats()
    {
        using var conn = CreateConnection();
        using var cmd = conn.CreateCommand();
        var todayPrefix = DateTime.UtcNow.ToString("yyyy-MM-dd");

        cmd.CommandText = @"
            SELECT 
                COALESCE(SUM(CASE WHEN submitted_at LIKE @today THEN 1 ELSE 0 END), 0),
                COALESCE(SUM(CASE WHEN submitted_at LIKE @today THEN (CASE WHEN pages_printed > 0 THEN pages_printed ELSE pages * copies END) ELSE 0 END), 0),
                COALESCE(SUM(CASE WHEN submitted_at LIKE @today AND color_mode = 'Color' THEN (CASE WHEN pages_printed > 0 THEN pages_printed ELSE pages * copies END) ELSE 0 END), 0),
                COALESCE(SUM(CASE WHEN submitted_at LIKE @today AND color_mode != 'Color' THEN (CASE WHEN pages_printed > 0 THEN pages_printed ELSE pages * copies END) ELSE 0 END), 0),
                COUNT(*),
                COALESCE(SUM(CASE WHEN pages_printed > 0 THEN pages_printed ELSE pages * copies END), 0),
                COALESCE(SUM(CASE WHEN color_mode = 'Color' THEN (CASE WHEN pages_printed > 0 THEN pages_printed ELSE pages * copies END) ELSE 0 END), 0),
                COALESCE(SUM(CASE WHEN color_mode != 'Color' THEN (CASE WHEN pages_printed > 0 THEN pages_printed ELSE pages * copies END) ELSE 0 END), 0),
                COALESCE(SUM(CASE WHEN sync_status != 'Synced' THEN 1 ELSE 0 END), 0)
            FROM print_jobs;
        ";
        cmd.Parameters.AddWithValue("@today", $"{todayPrefix}%");

        using var reader = cmd.ExecuteReader();
        if (reader.Read())
        {
            return (
                reader.GetInt32(0),
                Convert.ToInt32(reader.GetInt64(1)),
                Convert.ToInt32(reader.GetInt64(2)),
                Convert.ToInt32(reader.GetInt64(3)),
                reader.GetInt32(4),
                Convert.ToInt32(reader.GetInt64(5)),
                Convert.ToInt32(reader.GetInt64(6)),
                Convert.ToInt32(reader.GetInt64(7)),
                reader.GetInt32(8)
            );
        }
        return (0, 0, 0, 0, 0, 0, 0, 0, 0);
    }

    public List<PrintJob> GetFilteredJobs(
        int limit = 100, 
        string? search = null, 
        string? printer = null, 
        string? syncStatus = null,
        DateTime? fromDate = null,
        DateTime? toDate = null)
    {
        var list = new List<PrintJob>();
        using var conn = CreateConnection();
        using var cmd = conn.CreateCommand();
        var sql = @"
            SELECT id, remote_id, job_uid, job_id, printer_name, printer_server,
                   document_name, username, domain, computer_name,
                   pages, pages_printed, copies, color_mode, duplex, paper_size,
                   status, error_message, submitted_at, started_at, completed_at,
                   created_at, updated_at, synced_at, sync_status, retry_count
            FROM print_jobs
            WHERE 1=1
        ";

        if (!string.IsNullOrWhiteSpace(search))
        {
            sql += " AND (document_name LIKE @search OR username LIKE @search OR computer_name LIKE @search OR job_uid LIKE @search)";
            cmd.Parameters.AddWithValue("@search", $"%{search.Trim()}%");
        }

        if (!string.IsNullOrWhiteSpace(printer) && printer != "All Printers")
        {
            sql += " AND printer_name = @printer";
            cmd.Parameters.AddWithValue("@printer", printer);
        }

        if (!string.IsNullOrWhiteSpace(syncStatus) && syncStatus != "All Statuses")
        {
            if (syncStatus == "Synced")
            {
                sql += " AND sync_status = 'Synced'";
            }
            else if (syncStatus == "Pending")
            {
                sql += " AND sync_status != 'Synced'";
            }
        }

        if (fromDate.HasValue)
        {
            sql += " AND submitted_at >= @fromDate";
            cmd.Parameters.AddWithValue("@fromDate", fromDate.Value.ToUniversalTime().ToString("o", CultureInfo.InvariantCulture));
        }

        if (toDate.HasValue)
        {
            sql += " AND submitted_at <= @toDate";
            cmd.Parameters.AddWithValue("@toDate", toDate.Value.ToUniversalTime().ToString("o", CultureInfo.InvariantCulture));
        }

        sql += " ORDER BY id DESC LIMIT @limit;";
        cmd.Parameters.AddWithValue("@limit", limit);
        cmd.CommandText = sql;

        using var reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            list.Add(ReadJob(reader));
        }

        return list;
    }

    public (int jobsCount, int pagesCount, int colorPages, int monoPages) GetDateFilteredStats(DateTime? fromDate, DateTime? toDate)
    {
        using var conn = CreateConnection();
        using var cmd = conn.CreateCommand();
        var sql = @"
            SELECT 
                COUNT(*),
                COALESCE(SUM(CASE WHEN pages_printed > 0 THEN pages_printed ELSE pages * copies END), 0),
                COALESCE(SUM(CASE WHEN color_mode = 'Color' THEN (CASE WHEN pages_printed > 0 THEN pages_printed ELSE pages * copies END) ELSE 0 END), 0),
                COALESCE(SUM(CASE WHEN color_mode != 'Color' THEN (CASE WHEN pages_printed > 0 THEN pages_printed ELSE pages * copies END) ELSE 0 END), 0)
            FROM print_jobs
            WHERE 1=1
        ";

        if (fromDate.HasValue)
        {
            sql += " AND submitted_at >= @fromDate";
            cmd.Parameters.AddWithValue("@fromDate", fromDate.Value.ToUniversalTime().ToString("o", CultureInfo.InvariantCulture));
        }

        if (toDate.HasValue)
        {
            sql += " AND submitted_at <= @toDate";
            cmd.Parameters.AddWithValue("@toDate", toDate.Value.ToUniversalTime().ToString("o", CultureInfo.InvariantCulture));
        }

        cmd.CommandText = sql;
        using var reader = cmd.ExecuteReader();
        if (reader.Read())
        {
            return (
                reader.GetInt32(0),
                Convert.ToInt32(reader.GetInt64(1)),
                Convert.ToInt32(reader.GetInt64(2)),
                Convert.ToInt32(reader.GetInt64(3))
            );
        }

        return (0, 0, 0, 0);
    }

    private static PrintJob ReadJob(SqliteDataReader reader)
    {
        return new PrintJob
        {
            Id = reader.GetInt64(0),
            RemoteId = reader.IsDBNull(1) ? null : reader.GetInt64(1),
            JobUid = reader.GetString(2),
            JobId = reader.GetInt32(3),
            PrinterName = reader.GetString(4),
            PrinterServer = reader.IsDBNull(5) ? null : reader.GetString(5),
            DocumentName = reader.GetString(6),
            Username = reader.GetString(7),
            Domain = reader.IsDBNull(8) ? null : reader.GetString(8),
            ComputerName = reader.GetString(9),
            Pages = reader.GetInt32(10),
            PagesPrinted = reader.GetInt32(11),
            Copies = reader.GetInt32(12),
            ColorMode = reader.GetString(13),
            Duplex = reader.GetString(14),
            PaperSize = reader.GetString(15),
            Status = reader.GetString(16),
            ErrorMessage = reader.IsDBNull(17) ? null : reader.GetString(17),
            SubmittedAt = DateTime.Parse(reader.GetString(18), CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind),
            StartedAt = reader.IsDBNull(19) ? null : DateTime.Parse(reader.GetString(19), CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind),
            CompletedAt = reader.IsDBNull(20) ? null : DateTime.Parse(reader.GetString(20), CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind),
            CreatedAt = DateTime.Parse(reader.GetString(21), CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind),
            UpdatedAt = DateTime.Parse(reader.GetString(22), CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind),
            SyncedAt = reader.IsDBNull(23) ? null : DateTime.Parse(reader.GetString(23), CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind),
            SyncStatus = reader.GetString(24),
            RetryCount = reader.GetInt32(25)
        };
    }

    #endregion

    #region Printers

    public void SaveOrUpdatePrinters(IEnumerable<PrinterInfo> printers)
    {
        using var conn = CreateConnection();
        using var tx = conn.BeginTransaction();
        try
        {
            var nowStr = DateTime.UtcNow.ToString("o", CultureInfo.InvariantCulture);
            foreach (var p in printers)
            {
                using var cmd = conn.CreateCommand();
                cmd.Transaction = tx;
                cmd.CommandText = @"
                    INSERT INTO printers (
                        name, server_name, share_name, port_name, driver_name,
                        comment, location, status, status_raw, job_count, attributes,
                        is_default, is_network, is_shared, is_active, first_seen_at, last_seen_at
                    ) VALUES (
                        @name, @server_name, @share_name, @port_name, @driver_name,
                        @comment, @location, @status, @status_raw, @job_count, @attributes,
                        @is_default, @is_network, @is_shared, @is_active, @first_seen_at, @last_seen_at
                    )
                    ON CONFLICT(name) DO UPDATE SET
                        server_name = excluded.server_name,
                        share_name = excluded.share_name,
                        port_name = excluded.port_name,
                        driver_name = excluded.driver_name,
                        comment = excluded.comment,
                        location = excluded.location,
                        status = excluded.status,
                        status_raw = excluded.status_raw,
                        job_count = excluded.job_count,
                        attributes = excluded.attributes,
                        is_default = excluded.is_default,
                        is_network = excluded.is_network,
                        is_shared = excluded.is_shared,
                        is_active = 1,
                        last_seen_at = excluded.last_seen_at;
                ";

                cmd.Parameters.AddWithValue("@name", p.Name);
                cmd.Parameters.AddWithValue("@server_name", (object?)p.ServerName ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@share_name", (object?)p.ShareName ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@port_name", (object?)p.PortName ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@driver_name", (object?)p.DriverName ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@comment", (object?)p.Comment ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@location", (object?)p.Location ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@status", p.Status);
                cmd.Parameters.AddWithValue("@status_raw", p.StatusRaw);
                cmd.Parameters.AddWithValue("@job_count", p.JobCount);
                cmd.Parameters.AddWithValue("@attributes", p.Attributes);
                cmd.Parameters.AddWithValue("@is_default", p.IsDefault ? 1 : 0);
                cmd.Parameters.AddWithValue("@is_network", p.IsNetwork ? 1 : 0);
                cmd.Parameters.AddWithValue("@is_shared", p.IsShared ? 1 : 0);
                cmd.Parameters.AddWithValue("@is_active", 1);
                cmd.Parameters.AddWithValue("@first_seen_at", nowStr);
                cmd.Parameters.AddWithValue("@last_seen_at", nowStr);

                cmd.ExecuteNonQuery();
            }

            tx.Commit();
        }
        catch
        {
            tx.Rollback();
            throw;
        }
    }

    public List<PrinterInfo> GetAllPrinters()
    {
        var list = new List<PrinterInfo>();
        using var conn = CreateConnection();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = @"
            SELECT id, name, server_name, share_name, port_name, driver_name,
                   comment, location, status, status_raw, job_count, attributes,
                   is_default, is_network, is_shared, is_active, first_seen_at, last_seen_at
            FROM printers
            ORDER BY is_active DESC, name ASC;
        ";

        using var reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            list.Add(new PrinterInfo
            {
                Id = reader.GetInt64(0),
                Name = reader.GetString(1),
                ServerName = reader.IsDBNull(2) ? null : reader.GetString(2),
                ShareName = reader.IsDBNull(3) ? null : reader.GetString(3),
                PortName = reader.IsDBNull(4) ? null : reader.GetString(4),
                DriverName = reader.IsDBNull(5) ? null : reader.GetString(5),
                Comment = reader.IsDBNull(6) ? null : reader.GetString(6),
                Location = reader.IsDBNull(7) ? null : reader.GetString(7),
                Status = reader.GetString(8),
                StatusRaw = reader.GetInt32(9),
                JobCount = reader.GetInt32(10),
                Attributes = reader.GetInt32(11),
                IsDefault = reader.GetInt32(12) == 1,
                IsNetwork = reader.GetInt32(13) == 1,
                IsShared = reader.GetInt32(14) == 1,
                IsActive = reader.GetInt32(15) == 1,
                FirstSeenAt = DateTime.Parse(reader.GetString(16), CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind),
                LastSeenAt = DateTime.Parse(reader.GetString(17), CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind)
            });
        }

        return list;
    }

    public int GetPrinterCount()
    {
        using var conn = CreateConnection();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT COUNT(*) FROM printers WHERE is_active = 1;";
        return Convert.ToInt32(cmd.ExecuteScalar());
    }

    #endregion

    #region Computer Info & Settings

    public void SaveComputerInfo(ComputerInfo info)
    {
        using var conn = CreateConnection();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = @"
            INSERT INTO computer (
                device_id, computer_name, windows_user, domain, os_version,
                os_architecture, application_version, ip_address, mac_address,
                installed_at, last_heartbeat_at, is_registered
            ) VALUES (
                @device_id, @computer_name, @windows_user, @domain, @os_version,
                @os_architecture, @application_version, @ip_address, @mac_address,
                @installed_at, @last_heartbeat_at, @is_registered
            )
            ON CONFLICT(device_id) DO UPDATE SET
                computer_name = excluded.computer_name,
                windows_user = excluded.windows_user,
                domain = excluded.domain,
                os_version = excluded.os_version,
                os_architecture = excluded.os_architecture,
                application_version = excluded.application_version,
                ip_address = excluded.ip_address,
                mac_address = excluded.mac_address,
                last_heartbeat_at = excluded.last_heartbeat_at,
                is_registered = CASE WHEN excluded.is_registered = 1 THEN 1 ELSE computer.is_registered END;
        ";

        cmd.Parameters.AddWithValue("@device_id", info.DeviceId);
        cmd.Parameters.AddWithValue("@computer_name", info.ComputerName);
        cmd.Parameters.AddWithValue("@windows_user", info.WindowsUser);
        cmd.Parameters.AddWithValue("@domain", (object?)info.Domain ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@os_version", info.OsVersion);
        cmd.Parameters.AddWithValue("@os_architecture", info.OsArchitecture);
        cmd.Parameters.AddWithValue("@application_version", info.ApplicationVersion);
        cmd.Parameters.AddWithValue("@ip_address", (object?)info.IpAddress ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@mac_address", (object?)info.MacAddress ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@installed_at", info.InstalledAt.ToString("o", CultureInfo.InvariantCulture));
        cmd.Parameters.AddWithValue("@last_heartbeat_at", (object?)info.LastHeartbeatAt?.ToString("o", CultureInfo.InvariantCulture) ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@is_registered", info.IsRegistered ? 1 : 0);

        cmd.ExecuteNonQuery();
    }

    public ComputerInfo? GetComputerInfo(string deviceId)
    {
        using var conn = CreateConnection();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = @"
            SELECT device_id, computer_name, windows_user, domain, os_version,
                   os_architecture, application_version, ip_address, mac_address,
                   installed_at, last_heartbeat_at, is_registered
            FROM computer
            WHERE device_id = @id
            LIMIT 1;
        ";
        cmd.Parameters.AddWithValue("@id", deviceId);

        using var reader = cmd.ExecuteReader();
        if (!reader.Read())
            return null;

        return new ComputerInfo
        {
            DeviceId = reader.GetString(0),
            ComputerName = reader.GetString(1),
            WindowsUser = reader.GetString(2),
            Domain = reader.IsDBNull(3) ? null : reader.GetString(3),
            OsVersion = reader.GetString(4),
            OsArchitecture = reader.GetString(5),
            ApplicationVersion = reader.GetString(6),
            IpAddress = reader.IsDBNull(7) ? null : reader.GetString(7),
            MacAddress = reader.IsDBNull(8) ? null : reader.GetString(8),
            InstalledAt = DateTime.Parse(reader.GetString(9), CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind),
            LastHeartbeatAt = reader.IsDBNull(10) ? null : DateTime.Parse(reader.GetString(10), CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind),
            IsRegistered = reader.GetInt32(11) == 1
        };
    }

    public void UpdateHeartbeat(string deviceId, DateTime heartbeatTime)
    {
        using var conn = CreateConnection();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "UPDATE computer SET last_heartbeat_at = @hb WHERE device_id = @id;";
        cmd.Parameters.AddWithValue("@hb", heartbeatTime.ToString("o", CultureInfo.InvariantCulture));
        cmd.Parameters.AddWithValue("@id", deviceId);
        cmd.ExecuteNonQuery();
    }

    public void SetDeviceRegistered(string deviceId, bool isRegistered)
    {
        using var conn = CreateConnection();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "UPDATE computer SET is_registered = @reg WHERE device_id = @id;";
        cmd.Parameters.AddWithValue("@reg", isRegistered ? 1 : 0);
        cmd.Parameters.AddWithValue("@id", deviceId);
        cmd.ExecuteNonQuery();
    }

    #endregion
}
