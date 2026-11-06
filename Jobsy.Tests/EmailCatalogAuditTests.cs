using System.Text.Json;
using Jobsy.Core.Admin;
using Jobsy.Core.Email;
using Jobsy.Core.Interfaces;
using Jobsy.Core.Options;
using Jobsy.Infrastructure.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace Jobsy.Tests;

public class EmailCatalogAuditTests
{
    public EmailCatalogAuditTests()
        => EmailCatalogService.ResetForTests();

    [Fact]
    public async Task Send_writes_audit_with_redacted_recipient()
    {
        var audit = new RecordingAudit();
        var sut = CreateSut(audit);

        var result = await sut.SendAsync("MailTest", "nl", "reviewer@lobsy.nl", null);
        Assert.True(result.Ok);
        var entry = Assert.Single(audit.Entries);
        Assert.Equal(AdminAuditKeys.EmailTestSend, entry.Action);
        Assert.Equal(AdminAuditKeys.TargetTypes.EmailTemplate, entry.TargetType);
        Assert.Equal("MailTest", entry.TargetId);
        Assert.DoesNotContain("reviewer@lobsy.nl", entry.DetailsJson ?? "", StringComparison.OrdinalIgnoreCase);
        Assert.Contains("r***@lobsy.nl", entry.DetailsJson ?? "", StringComparison.Ordinal);
        using var doc = JsonDocument.Parse(entry.DetailsJson!);
        Assert.Equal("nl", doc.RootElement.GetProperty("language").GetString());
    }

    private static EmailCatalogService CreateSut(RecordingAudit audit)
    {
        var services = new ServiceCollection();
        services.AddSingleton<ITransactionalMailer>(new NoopMailer());
        services.AddSingleton<IPlatformFeatureService>(new FakeFeatures());
        services.AddSingleton<IAdminAuditLog>(audit);
        services.AddSingleton(Options.Create(new MailOptions()));
        services.AddSingleton<IEmailCatalogService>(sp => new EmailCatalogService(
            sp.GetRequiredService<ITransactionalMailer>(),
            sp.GetRequiredService<IPlatformFeatureService>(),
            sp.GetRequiredService<IAdminAuditLog>(),
            sp.GetRequiredService<IServiceScopeFactory>(),
            sp.GetRequiredService<IOptions<MailOptions>>(),
            NullLogger<EmailCatalogService>.Instance));
        return (EmailCatalogService)services.BuildServiceProvider().GetRequiredService<IEmailCatalogService>();
    }

    private sealed class RecordingAudit : IAdminAuditLog
    {
        public List<AdminAuditEntry> Entries { get; } = [];

        public Task WriteAsync(AdminAuditEntry entry, CancellationToken cancellationToken = default)
        {
            Entries.Add(entry);
            return Task.CompletedTask;
        }

        public void Stage(AdminAuditEntry entry) => Entries.Add(entry);
    }

    private sealed class FakeFeatures : IPlatformFeatureService
    {
        public Task<PlatformFeatureSnapshot> GetAsync(CancellationToken cancellationToken = default)
            => Task.FromResult(new PlatformFeatureSnapshot(true, false, "https://lobsy.nl", DateTime.UtcNow));

        public Task<PlatformFeatureSnapshot> UpdateAsync(
            PlatformFeatureUpdate update,
            CancellationToken cancellationToken = default)
            => GetAsync(cancellationToken);
    }

    private sealed class NoopMailer : ITransactionalMailer
    {
        public Task<EmailSendOutcome> SendAsync(
            ComposedEmail mail,
            string to,
            EmailSendOptions? options = null,
            CancellationToken cancellationToken = default)
            => Task.FromResult(new EmailSendOutcome(true, false, null, EmailDeliveryKind.Stub));
    }
}
