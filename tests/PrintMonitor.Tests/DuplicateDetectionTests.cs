using PrintMonitor.Data;
using PrintMonitor.Models;
using Xunit;

namespace PrintMonitor.Tests;

public class DuplicateDetectionTests : IDisposable
{
    private readonly string _dbPath;
    private readonly string _connStr;
    private readonly PrintMonitorDbContext _dbContext;

    public DuplicateDetectionTests()
    {
        _dbPath = Path.Combine(Path.GetTempPath(), $"duplicate_test_{Guid.NewGuid():N}.db");
        _connStr = $"Data Source={_dbPath}";
        var initializer = new DatabaseInitializer(_connStr);
        initializer.Initialize();
        _dbContext = new PrintMonitorDbContext(_connStr);
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
    public void SameJobUid_DoesNotCreateDuplicateRow()
    {
        var job1 = new PrintJob
        {
            JobId = 1,
            JobUid = "dev-1_Printer1_1_638000000000000000",
            PrinterName = "Printer1",
            DocumentName = "Contract.pdf",
            Username = "john",
            ComputerName = "OFFICE-PC",
            Pages = 5,
            PagesPrinted = 0,
            Status = "Submitted",
            SubmittedAt = DateTime.UtcNow
        };

        var id1 = _dbContext.SaveOrUpdateJob(job1);

        // Job polled again with progress
        var job2 = new PrintJob
        {
            JobId = 1,
            JobUid = "dev-1_Printer1_1_638000000000000000",
            PrinterName = "Printer1",
            DocumentName = "Contract.pdf",
            Username = "john",
            ComputerName = "OFFICE-PC",
            Pages = 5,
            PagesPrinted = 5,
            Status = "Printing",
            SubmittedAt = job1.SubmittedAt
        };

        var id2 = _dbContext.SaveOrUpdateJob(job2);

        Assert.Equal(id1, id2);
        Assert.Equal(1, _dbContext.GetTotalJobCount());

        var list = _dbContext.GetPendingSyncJobs(10);
        Assert.Single(list);
        Assert.Equal(5, list[0].PagesPrinted);
        Assert.Equal("Printing", list[0].Status);
    }

    [Fact]
    public void ReusedSpoolerJobId_AtDifferentTime_CreatesSeparateRecords()
    {
        var submittedTime1 = new DateTime(2026, 1, 1, 10, 0, 0, DateTimeKind.Utc);
        var submittedTime2 = new DateTime(2026, 1, 2, 14, 0, 0, DateTimeKind.Utc);

        // First print job: Job ID 1 on Jan 1
        var job1 = new PrintJob
        {
            JobId = 1,
            JobUid = $"dev-1_Printer1_1_{submittedTime1.Ticks}",
            PrinterName = "Printer1",
            DocumentName = "Doc1.pdf",
            Username = "john",
            ComputerName = "PC1",
            Pages = 2,
            PagesPrinted = 2,
            Status = "Completed",
            SubmittedAt = submittedTime1
        };

        var id1 = _dbContext.SaveOrUpdateJob(job1);

        // Spooler restarted or reset counter, next day another user prints and gets Job ID 1
        var job2 = new PrintJob
        {
            JobId = 1,
            JobUid = $"dev-1_Printer1_1_{submittedTime2.Ticks}",
            PrinterName = "Printer1",
            DocumentName = "Doc2.pdf",
            Username = "mary",
            ComputerName = "PC2",
            Pages = 8,
            PagesPrinted = 0,
            Status = "Submitted",
            SubmittedAt = submittedTime2
        };

        var id2 = _dbContext.SaveOrUpdateJob(job2);

        Assert.NotEqual(id1, id2);
        Assert.Equal(2, _dbContext.GetTotalJobCount());

        var allJobs = _dbContext.GetRecentJobs(10);
        Assert.Equal(2, allJobs.Count);
        Assert.Contains(allJobs, j => j.DocumentName == "Doc1.pdf" && j.Username == "john");
        Assert.Contains(allJobs, j => j.DocumentName == "Doc2.pdf" && j.Username == "mary");
    }
}
