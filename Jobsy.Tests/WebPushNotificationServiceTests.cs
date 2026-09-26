using Jobsy.Core.Entities;
using Jobsy.Core.Enums;
using Jobsy.Core.Interfaces;
using Jobsy.Infrastructure.Data;
using Jobsy.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace Jobsy.Tests;

public class WebPushNotificationServiceTests
{
    [Fact]
    public async Task SendAsync_without_subscriptions_logs_only()
    {
        await using var db = CreateDb();
        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = "push.user@jobsy.local",
            FullName = "Push User",
            Role = UserRole.Candidate,
            IsActive = true
        };
        db.Users.Add(user);
        await db.SaveChangesAsync();

        var vapid = new WebPushVapidKeyProvider(
            new TestOptionsMonitor(new Jobsy.Core.Options.WebPushOptions()),
            new FakeHostEnvironment { EnvironmentName = Environments.Development },
            NullLogger<WebPushVapidKeyProvider>.Instance);
        var sut = new WebPushNotificationService(db, vapid, NullLogger<WebPushNotificationService>.Instance);

        await sut.SendAsync(new PushMessage(user.Email, "Nieuwe match", "Er staat een rol voor je klaar.", "/candidate/match", "Match"));

        Assert.Contains(db.PlatformLogs, l => l.Category == "Match" && l.Message.Contains("Push to", StringComparison.Ordinal));
        Assert.False(string.IsNullOrWhiteSpace(vapid.PublicKey));
    }

    [Fact]
    public async Task Subscription_service_upserts_by_endpoint()
    {
        await using var db = CreateDb();
        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = "sub.user@jobsy.local",
            FullName = "Sub User",
            Role = UserRole.Candidate,
            IsActive = true
        };
        db.Users.Add(user);
        await db.SaveChangesAsync();

        var sut = new WebPushSubscriptionService(db);
        await sut.UpsertAsync(user.Id, new WebPushSubscriptionInput(
            "https://push.example/endpoint-1",
            "p256dh-key",
            "auth-key",
            "TestAgent"));

        await sut.UpsertAsync(user.Id, new WebPushSubscriptionInput(
            "https://push.example/endpoint-1",
            "p256dh-key-2",
            "auth-key-2",
            "TestAgent/2"));

        var rows = await sut.ListForUserAsync(user.Id);
        Assert.Single(rows);
        Assert.Equal(1, await db.WebPushSubscriptions.CountAsync());
        Assert.Equal("p256dh-key-2", (await db.WebPushSubscriptions.SingleAsync()).P256dh);
    }

    [Fact]
    public void Pwa_assets_and_manifest_exist()
    {
        var root = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", ".."));
        Assert.True(File.Exists(Path.Combine(root, "Jobsy.Web/wwwroot/manifest.webmanifest")));
        Assert.True(File.Exists(Path.Combine(root, "Jobsy.Web/wwwroot/service-worker.js")));
        Assert.True(File.Exists(Path.Combine(root, "Jobsy.Web/wwwroot/service-worker.published.js")));
        Assert.True(File.Exists(Path.Combine(root, "Jobsy.Web/wwwroot/icons/icon-192.png")));
        Assert.True(File.Exists(Path.Combine(root, "Jobsy.Web/wwwroot/icons/icon-512.png")));
        Assert.True(File.Exists(Path.Combine(root, "Jobsy.Web/wwwroot/js/lobsyPush.js")));

        var app = File.ReadAllText(Path.Combine(root, "Jobsy.Web/Components/App.razor"));
        Assert.Contains("rel=\"manifest\"", app);
        Assert.Contains("apple-mobile-web-app-capable", app);
        Assert.Contains("lobsyPush.js", app);
    }

    private static JobsyDbContext CreateDb()
    {
        var options = new DbContextOptionsBuilder<JobsyDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .Options;
        return new JobsyDbContext(options);
    }

    private sealed class TestOptionsMonitor(Jobsy.Core.Options.WebPushOptions current)
        : IOptionsMonitor<Jobsy.Core.Options.WebPushOptions>
    {
        public Jobsy.Core.Options.WebPushOptions CurrentValue { get; } = current;
        public Jobsy.Core.Options.WebPushOptions Get(string? name) => CurrentValue;
        public IDisposable? OnChange(Action<Jobsy.Core.Options.WebPushOptions, string?> listener) => null;
    }

    private sealed class FakeHostEnvironment : IHostEnvironment
    {
        public string EnvironmentName { get; set; } = Environments.Development;
        public string ApplicationName { get; set; } = "Jobsy.Tests";
        public string ContentRootPath { get; set; } = AppContext.BaseDirectory;
        public Microsoft.Extensions.FileProviders.IFileProvider ContentRootFileProvider { get; set; }
            = new Microsoft.Extensions.FileProviders.NullFileProvider();
    }
}
