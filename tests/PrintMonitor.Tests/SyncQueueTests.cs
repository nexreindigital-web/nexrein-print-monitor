using PrintMonitor.Data;
using PrintMonitor.Models;
using Xunit;

namespace PrintMonitor.Tests;

public class SyncQueueTests : IDisposable
{
    private readonly string _dbPath;
    private readonly string _connStr;
    private readonly PrintMonitorDbContext _dbContext;

    public SyncQueueTests()
    {
        _dbPath = Path.Combine(Path.GetTempPath(), $"sync_test_{Guid.NewGuid():N}.db");
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
    public void PendingJobs_ArePreserved_And_NotDeleted_WhenOffline()
    {
        // Insert 3 jobs offline
        for (int i = 1; i <= 3; i++)
        {
            _dbContext.SaveOrUpdateJob(new PrintJob
            {
                JobId = i,
                JobUid = $"uid_{i}",
                PrinterName = "Office Printer",
                DocumentName = $"Doc_{i}.pdf",
                Username = "user",
                ComputerName = "MY-PC",
                Pages = i,
                Status = "Completed",
                SubmittedAt = DateTime.UtcNow
            });
        }

        Assert.Equal(3, _dbContext.GetPendingSyncCount());
        var pending = _dbContext.GetPendingSyncJobs(10);
        Assert.Equal(3, pending.Count);

        // Fail sync attempt
        _dbContext.IncrementRetryCount(pending.Select(p => p.Id), "Connection refused: backend offline");

        // Jobs must NOT be deleted!
        Assert.Equal(3, _dbContext.GetPendingSyncCount());
        var retried = _dbContext.GetPendingSyncJobs(10);
        Assert.Equal(3, retried.Count);
        Assert.All(retried, r => Assert.Equal(1, r.RetryCount));
        Assert.All(retried, r => Assert.Equal("Failed", r.SyncStatus));
    }

    [Fact]
    public void MarkJobsSynced_RemovesJobsFromPendingList()
    {
        var job1 = new PrintJob
        {
            JobId = 10,
            JobUid = "uid_10",
            PrinterName = "Color LaserJet",
            DocumentName = "Presentation.pptx",
            Username = "director",
            ComputerName = "EXEC-PC",
            Pages = 25,
            Status = "Completed",
            SubmittedAt = DateTime.UtcNow
        };
        var job2 = new PrintJob
        {
            JobId = 11,
            JobUid = "uid_11",
            PrinterName = "Color LaserJet",
            DocumentName = "Notes.txt",
            Username = "director",
            ComputerName = "EXEC-PC",
            Pages = 1,
            Status = "Completed",
            SubmittedAt = DateTime.UtcNow
        };

        var id1 = _dbContext.SaveOrUpdateJob(job1);
        var id2 = _dbContext.SaveOrUpdateJob(job2);

        Assert.Equal(2, _dbContext.GetPendingSyncCount());

        // Mark only job1 as synced
        _dbContext.MarkJobsSynced(new[] { (id1, 9999L) });

        Assert.Equal(1, _dbContext.GetPendingSyncCount());
        var remaining = _dbContext.GetPendingSyncJobs(10);
        Assert.Single(remaining);
        Assert.Equal(id2, remaining[0].Id);
    }
}
