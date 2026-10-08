using System.Net;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging.Abstractions;
using PrintMonitor.Configuration;
using PrintMonitor.Models;
using PrintMonitor.Services;
using Xunit;

namespace PrintMonitor.Tests;

public class ApiClientTests
{
    private class MockHttpMessageHandler : HttpMessageHandler
    {
        private readonly Func<HttpRequestMessage, HttpResponseMessage> _handler;

        public MockHttpMessageHandler(Func<HttpRequestMessage, HttpResponseMessage> handler)
        {
            _handler = handler;
        }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            return Task.FromResult(_handler(request));
        }
    }

    [Fact]
    public async Task RegisterDeviceAsync_SendsCorrectPayload_AndParsesResponse()
    {
        string? capturedBody = null;
        string? capturedAuth = null;

        var handler = new MockHttpMessageHandler(req =>
        {
            capturedAuth = req.Headers.Authorization?.Parameter;
            using var stream = req.Content!.ReadAsStream();
            using var reader = new StreamReader(stream);
            capturedBody = reader.ReadToEnd();

            var responseDto = new DeviceRegisterResponse
            {
                Success = true,
                Message = "Device registered",
                DeviceId = "test-device-guid",
                ApiKey = "token-secret-123"
            };

            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(JsonSerializer.Serialize(responseDto), Encoding.UTF8, "application/json")
            };
        });

        var httpClient = new HttpClient(handler) { BaseAddress = new Uri("https://api.printmonitor.local/api/") };
        var settingsManager = new SettingsManager();
        settingsManager.Settings.ApiKey = "initial-api-key";

        var client = new ApiClient(httpClient, settingsManager, NullLogger<ApiClient>.Instance);

        var request = new DeviceRegisterRequest
        {
            DeviceId = "test-device-guid",
            ComputerName = "TEST-PC",
            WindowsUser = "tester",
            OsVersion = "Windows 11",
            ApplicationVersion = "1.0.0"
        };

        var (success, response, error) = await client.RegisterDeviceAsync(request);

        Assert.True(success);
        Assert.NotNull(response);
        Assert.True(response.Success);
        Assert.Equal("token-secret-123", response.ApiKey);
        Assert.Null(error);

        Assert.NotNull(capturedBody);
        Assert.Contains("test-device-guid", capturedBody);
        Assert.Contains("TEST-PC", capturedBody);
    }

    [Fact]
    public async Task SyncPrintJobsAsync_HandlesServerFailure_Gracefully()
    {
        var handler = new MockHttpMessageHandler(req =>
        {
            return new HttpResponseMessage(HttpStatusCode.InternalServerError)
            {
                Content = new StringContent("{\"error\":\"Database connection failed\"}", Encoding.UTF8, "application/json")
            };
        });

        var httpClient = new HttpClient(handler) { BaseAddress = new Uri("https://api.printmonitor.local/api/") };
        var settingsManager = new SettingsManager();
        var client = new ApiClient(httpClient, settingsManager, NullLogger<ApiClient>.Instance);

        var request = new PrintJobSyncRequest
        {
            DeviceId = "dev-1",
            Jobs = new List<PrintJobSyncDto>
            {
                new() { JobId = 1, JobUid = "uid-1", DocumentName = "Doc.pdf", Username = "user", Pages = 2 }
            }
        };

        var (success, response, error) = await client.SyncPrintJobsAsync(request);

        Assert.False(success);
        Assert.Null(response);
        Assert.NotNull(error);
        Assert.Contains("500", error);
    }

    [Fact]
    public async Task SendHeartbeatAsync_SucceedsWithValidResponse()
    {
        var handler = new MockHttpMessageHandler(req =>
        {
            var responseDto = new HeartbeatResponse { Success = true, Message = "Heartbeat recorded" };
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(JsonSerializer.Serialize(responseDto), Encoding.UTF8, "application/json")
            };
        });

        var httpClient = new HttpClient(handler) { BaseAddress = new Uri("https://api.printmonitor.local/api/") };
        var settingsManager = new SettingsManager();
        var client = new ApiClient(httpClient, settingsManager, NullLogger<ApiClient>.Instance);

        var request = new HeartbeatRequest
        {
            DeviceId = "dev-1",
            ComputerName = "PC1",
            Status = "online",
            PrinterCount = 2,
            PendingSyncCount = 0
        };

        var (success, response, error) = await client.SendHeartbeatAsync(request);

        Assert.True(success);
        Assert.NotNull(response);
        Assert.True(response.Success);
        Assert.Null(error);
    }
}
