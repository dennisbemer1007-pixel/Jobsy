using Jobsy.Core.Interfaces;
using Jobsy.Infrastructure.Ops;
using Microsoft.Extensions.Logging.Abstractions;

namespace Jobsy.Tests.TestAccounts;

public class TestAccountMailGuardTests
{
    private sealed class CapturingEmail : IEmailService
    {
        public List<EmailMessage> Sent { get; } = [];

        public Task<EmailDeliveryResult> SendAsync(EmailMessage message, CancellationToken cancellationToken = default)
        {
            Sent.Add(message);
            return Task.FromResult(EmailDeliveryResult.Stub);
        }
    }

    [Fact]
    public async Task Inside_test_scope_mail_to_real_recipient_is_dropped()
    {
        var inner = new CapturingEmail();
        var guard = new TestAccountMailGuard(inner, NullLogger<TestAccountMailGuard>.Instance, "lobsy.nl");
        using (TestAccountScope.Enter())
        {
            await guard.SendAsync(new EmailMessage("real@example.com", "Hi", "<p>x</p>", "tpl"));
        }

        Assert.Empty(inner.Sent);
    }

    [Fact]
    public async Task Inside_test_scope_mail_to_test_address_is_sent()
    {
        var inner = new CapturingEmail();
        var guard = new TestAccountMailGuard(inner, NullLogger<TestAccountMailGuard>.Instance, "lobsy.nl");
        using (TestAccountScope.Enter())
        {
            await guard.SendAsync(new EmailMessage("test-admin@lobsy.nl", "Hi", "<p>x</p>", "tpl"));
        }

        Assert.Single(inner.Sent);
    }

    [Fact]
    public async Task Outside_scope_mail_is_sent()
    {
        var inner = new CapturingEmail();
        var guard = new TestAccountMailGuard(inner, NullLogger<TestAccountMailGuard>.Instance, "lobsy.nl");
        await guard.SendAsync(new EmailMessage("real@example.com", "Hi", "<p>x</p>", "tpl"));
        Assert.Single(inner.Sent);
    }
}
