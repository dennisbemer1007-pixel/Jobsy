using Jobsy.Core.Email;
using Jobsy.Core.Interfaces;
using Jobsy.Core.Options;
using Jobsy.Infrastructure.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace Jobsy.Tests;

public class EmailTestSendLimitsTests
{
    public EmailTestSendLimitsTests()
        => EmailCatalogService.ResetForTests();

    [Fact]
    public async Task Send_forces_admin_address_and_honours_allow_list()
    {
        var mailer = new RecordingMailer();
        var sut = CreateSut(mailer, allowList: ["allowed@example.com"]);

        var self = await sut.SendAsync("MailTest", "nl", "admin@lobsy.nl", null);
        Assert.True(self.Ok);
        Assert.Equal("admin@lobsy.nl", mailer.Sent[^1].To);
        Assert.StartsWith("[Test] ", self.Subject, StringComparison.Ordinal);
        Assert.True(mailer.Sent[^1].Options?.IsTest);

        var allowed = await sut.SendAsync("MailTest", "nl", "admin@lobsy.nl", "allowed@example.com");
        Assert.True(allowed.Ok);
        Assert.Equal("allowed@example.com", mailer.Sent[^1].To);

        var foreign = await sut.SendAsync("MailTest", "nl", "admin@lobsy.nl", "other@example.com");
        Assert.False(foreign.Ok);
        Assert.Contains("allow-list", foreign.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Daily_cap_blocks_further_sends()
    {
        var sut = CreateSut(new RecordingMailer(), dailyCap: 1);
        var first = await sut.SendAsync("MailTest", "nl", "cap@lobsy.nl", null);
        Assert.True(first.Ok);
        var second = await sut.SendAsync("MailTest", "nl", "cap@lobsy.nl", null);
        Assert.False(second.Ok);
        Assert.Contains("Daglimiet", second.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Second_send_all_within_15_minutes_is_rejected()
    {
        var adminId = Guid.NewGuid();
        var sut = CreateSut(new RecordingMailer(), dailyCap: 500);
        var first = await sut.StartSendAllAsync("nl", "sendall@lobsy.nl", null, adminId);
        Assert.True(first.Total > 0);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => sut.StartSendAllAsync("nl", "sendall@lobsy.nl", null, adminId));
    }

    [Fact]
    public async Task Send_all_skips_parked_keys()
    {
        var sut = CreateSut(new RecordingMailer(), ambassadorsEnabled: false, dailyCap: 500);
        var accepted = await sut.StartSendAllAsync("nl", "park@lobsy.nl", null, Guid.NewGuid());
        var listed = sut.ListTemplates(ambassadorsEnabled: false);
        Assert.Contains(listed, t => t.Parked && t.Key == "AmbassadeurInvite");
        Assert.Equal(listed.Count(t => !t.Parked), accepted.Total);
    }

    private static EmailCatalogService CreateSut(
        RecordingMailer mailer,
        string[]? allowList = null,
        int dailyCap = 100,
        bool ambassadorsEnabled = true)
    {
        var services = new ServiceCollection();
        var mail = Options.Create(new MailOptions
        {
            TestRecipientAllowList = allowList ?? [],
            TestDailyCap = dailyCap
        });
        services.AddSingleton<ITransactionalMailer>(mailer);
        services.AddSingleton<IPlatformFeatureService>(new FakeFeatures(ambassadorsEnabled));
        services.AddSingleton<IAdminAuditLog>(new NoopAudit());
        services.AddSingleton(mail);
        services.AddSingleton<IEmailCatalogService>(sp => new EmailCatalogService(
            sp.GetRequiredService<ITransactionalMailer>(),
            sp.GetRequiredService<IPlatformFeatureService>(),
            sp.GetRequiredService<IAdminAuditLog>(),
            sp.GetRequiredService<IServiceScopeFactory>(),
            sp.GetRequiredService<IOptions<MailOptions>>(),
            NullLogger<EmailCatalogService>.Instance));
        var sp = services.BuildServiceProvider();
        return (EmailCatalogService)sp.GetRequiredService<IEmailCatalogService>();
    }

    private sealed class FakeFeatures(bool ambassadors) : IPlatformFeatureService
    {
        public Task<PlatformFeatureSnapshot> GetAsync(CancellationToken cancellationToken = default)
            => Task.FromResult(new PlatformFeatureSnapshot(true, ambassadors, false, "https://lobsy.nl", DateTime.UtcNow));

        public Task<PlatformFeatureSnapshot> UpdateAsync(
            PlatformFeatureUpdate update,
            CancellationToken cancellationToken = default)
            => GetAsync(cancellationToken);
    }

    private sealed class NoopAudit : IAdminAuditLog
    {
        public Task WriteAsync(AdminAuditEntry entry, CancellationToken cancellationToken = default)
            => Task.CompletedTask;

        public void Stage(AdminAuditEntry entry) { }
    }

    private sealed class RecordingMailer : ITransactionalMailer
    {
        public List<(ComposedEmail Mail, string To, EmailSendOptions? Options)> Sent { get; } = [];

        public Task<EmailSendOutcome> SendAsync(
            ComposedEmail mail,
            string to,
            EmailSendOptions? options = null,
            CancellationToken cancellationToken = default)
        {
            Sent.Add((mail, to, options));
            return Task.FromResult(new EmailSendOutcome(true, false, null, EmailDeliveryKind.Stub));
        }
    }
}
