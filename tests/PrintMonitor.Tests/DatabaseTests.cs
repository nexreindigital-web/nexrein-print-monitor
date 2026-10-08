using PrintMonitor.Data;
using PrintMonitor.Models;
using Xunit;

namespace PrintMonitor.Tests;

public class DatabaseTests : IDisposable
{
    private readonly string _dbPath;
    private readonly string _connStr;
    private readonly DatabaseInitializer _initializer;
    private readonly PrintMonitorDbContext _dbContext;

    public DatabaseTests()
    {
        _dbPath = Path.Combine(Path.GetTempPath(), $"printmonitor_test_{Guid.NewGuid():N}.db");
        _connStr = $"Data Source={_dbPath}";
        _initializer = new DatabaseInitializer(_connStr);
        _dbContext = new PrintMonitorDbContext(_connStr);

        _initializer.Initialize();
    }

    public void Dispose()
    {
        try
        {
            if (File.Exists(_dbPath))
            {
                File.Delete(_dbPath);
            }
        }
        catch { }
    }

    [Fact]
    public void DatabaseInitializer_CreatesExpectedTables()
    {
        // Re-initializing should be idempotent
        _initializer.Initialize();

        var printerCount = _dbContext.GetPrinterCount();
        var pendingCount = _dbContext.GetPendingSyncCount();
        var totalJobs = _dbContext.GetTotalJobCount();

        Assert.Equal(0, printerCount);
        Assert.Equal(0, pendingCount);
        Assert.Equal(0, totalJobs);
    }

    [Fact]
    public void JobInsertion_And_Retrieval_WorksCorrectly()
    {
        var job = new PrintJob
        {
            JobId = 42,
            JobUid = "device1_HP_42_123456789",
            PrinterName = "HP LaserJet Pro",
            DocumentName = "Financial_Report.pdf",
            Username = "alice",
            Domain = "CORP",
            ComputerName = "WORKSTATION-01",
            Pages = 10,
            PagesPrinted = 0,
            Copies = 2,
            ColorMode = "Color",
            Duplex = "DuplexLongEdge",
            PaperSize = "A4",
            Status = "Submitted",
            SubmittedAt = DateTime.UtcNow
        };

        var id = _dbContext.SaveOrUpdateJob(job);
        Assert.True(id > 0);

        var pending = _dbContext.GetPendingSyncJobs(10);
        Assert.Single(pending);
        Assert.Equal("Financial_Report.pdf", pending[0].DocumentName);
        Assert.Equal("alice", pending[0].Username);
        Assert.Equal(10, pending[0].Pages);
        Assert.Equal("Submitted", pending[0].Status);
        Assert.Equal("Pending", pending[0].SyncStatus);
    }

    [Fact]
    public void JobStatus_Update_ReflectsInDatabase()
    {
        var job = new PrintJob
        {
            JobId = 101,
            JobUid = "dev1_Canon_101_555",
            PrinterName = "Canon G2010",
            DocumentName = "Invoice.docx",
            Username = "bob",
            ComputerName = "PC-BOB",
            Pages = 3,
            PagesPrinted = 0,
            Status = "Submitted",
            SubmittedAt = DateTime.UtcNow
        };

        var id = _dbContext.SaveOrUpdateJob(job);

        // Update pages printed and status to Completed
        job.PagesPrinted = 3;
        job.Status = "Completed";
        job.CompletedAt = DateTime.UtcNow;

        var updatedId = _dbContext.SaveOrUpdateJob(job);
        Assert.Equal(id, updatedId);

        var recent = _dbContext.GetRecentJobs(5);
        Assert.Single(recent);
        Assert.Equal(3, recent[0].PagesPrinted);
        Assert.Equal("Completed", recent[0].Status);
        Assert.NotNull(recent[0].CompletedAt);
    }
}
