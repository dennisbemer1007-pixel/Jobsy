using Jobsy.Core.Email;
using Jobsy.Core.Email.Model;
using Jobsy.Core.Features;
using Jobsy.Core.Interfaces;
using Jobsy.Infrastructure.Data;
using Jobsy.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.Abstractions;

namespace Jobsy.Tests;

public class EmailLayoutGuardTests
{
    [Fact]
    public void New_EmailMessage_only_in_TransactionalMailer_outside_tests()
    {
        var root = FindRepoRoot();
        var hits = new List<string>();
        foreach (var project in new[] { "Jobsy.Api", "Jobsy.Infrastructure", "Jobsy.Core" })
        {
            var dir = Path.Combine(root, project);
            if (!Directory.Exists(dir))
            {
                continue;
            }

            foreach (var file in Directory.GetFiles(dir, "*.cs", SearchOption.AllDirectories))
            {
                if (file.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}")
                    || file.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}"))
                {
                    continue;
                }

                if (file.EndsWith("TransactionalMailer.cs", StringComparison.Ordinal))
                {
                    continue;
                }

                var source = File.ReadAllText(file);
                if (source.Contains("new EmailMessage(", StringComparison.Ordinal))
                {
                    hits.Add(Path.GetRelativePath(root, file));
                }
            }
        }

        Assert.True(hits.Count == 0, "new EmailMessage( outside TransactionalMailer:\n" + string.Join("\n", hits));
    }

    [Fact]
    public void No_bare_html_or_EmailLayout_Wrap_outside_renderer()
    {
        var root = FindRepoRoot();
        var hits = new List<string>();
        foreach (var file in Directory.GetFiles(root, "*.cs", SearchOption.AllDirectories))
        {
            if (file.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}")
                || file.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}")
                || file.Contains($"{Path.DirectorySeparatorChar}Jobsy.Tests{Path.DirectorySeparatorChar}"))
            {
                continue;
            }

            if (file.EndsWith("EmailRenderer.cs", StringComparison.Ordinal))
            {
                continue;
            }

            // Errors 01: the last-resort error document is deliberately hard-coded HTML,
            // because it must render when every template and stylesheet is unavailable.
            if (file.EndsWith("ErrorResponse.cs", StringComparison.Ordinal))
            {
                continue;
            }

            var source = File.ReadAllText(file);
            if (source.Contains("EmailLayout.Wrap(", StringComparison.Ordinal)
                || source.Contains("<html", StringComparison.OrdinalIgnoreCase)
                || source.Contains("<p style", StringComparison.OrdinalIgnoreCase)
                || source.Contains("<table", StringComparison.OrdinalIgnoreCase))
            {
                hits.Add(Path.GetRelativePath(root, file));
            }
        }

        Assert.True(hits.Count == 0, "Bare HTML / Wrap outside EmailRenderer:\n" + string.Join("\n", hits));
    }

    [Fact]
    public async Task Mailer_throws_in_testing_on_bare_html_composed_email()
    {
        await using var db = new JobsyDbContext(new DbContextOptionsBuilder<JobsyDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);
        var mailer = new TransactionalMailer(
            new RecordingEmail(),
            new AlwaysOnFlags(),
            new FakeFeatures(),
            new NoOpPreferences(),
            new FakeUnsubscribe(),
            Microsoft.Extensions.Options.Options.Create(new Jobsy.Core.Options.MailOptions()),
            db,
            new FakeHostEnvironment("Testing"),
            NullLogger<TransactionalMailer>.Instance);

        var bare = new ComposedEmail("MailTest", "MailTest", EmailKind.Essential, "nl",
            "Test", "Pre", "<p>bare</p>", "bare");
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            mailer.SendAsync(bare, "a@example.com"));
    }

    private sealed class RecordingEmail : IEmailService, ITransactionalMailer
    {
        public async Task<EmailSendOutcome> SendAsync(
            ComposedEmail mail,
            string to,
            EmailSendOptions? options = null,
            CancellationToken cancellationToken = default)
        {
            var delivery = await SendAsync(
                new EmailMessage(to, mail.Subject, mail.Html ?? string.Empty, mail.Category),
                cancellationToken);
            return new EmailSendOutcome(true, false, null, delivery.Kind);
        }

        public Task<EmailDeliveryResult> SendAsync(EmailMessage message, CancellationToken cancellationToken = default)
            => Task.FromResult(EmailDeliveryResult.Stub);
    }

    private sealed class AlwaysOnFlags : IFeatureFlags
    {
        public ValueTask<FeatureFlagSnapshot> GetAsync(CancellationToken cancellationToken = default)
            => ValueTask.FromResult(new FeatureFlagSnapshot(true, true));

        public ValueTask<bool> IsEnabledAsync(PlatformFeature feature, CancellationToken cancellationToken = default)
            => ValueTask.FromResult(true);

        public void Invalidate()
        {
        }
    }

    private sealed class FakeFeatures : IPlatformFeatureService
    {
        public Task<PlatformFeatureSnapshot> GetAsync(CancellationToken cancellationToken = default)
            => Task.FromResult(new PlatformFeatureSnapshot(true, false, "https://lobsy.nl", DateTime.UtcNow));

        public Task<PlatformFeatureSnapshot> UpdateAsync(PlatformFeatureUpdate update, CancellationToken cancellationToken = default)
            => GetAsync(cancellationToken);
    }

    private sealed class NoOpPreferences : IEmailPreferenceService
    {
        public Task<bool> IsOptedOutAsync(string email, string category, CancellationToken cancellationToken = default)
            => Task.FromResult(false);
        public Task OptOutAsync(string email, string category, string source, CancellationToken cancellationToken = default)
            => Task.CompletedTask;
        public Task OptInAsync(string email, string category, CancellationToken cancellationToken = default)
            => Task.CompletedTask;
        public Task OptOutByHashAsync(string emailHash, string category, string source, CancellationToken cancellationToken = default)
            => Task.CompletedTask;
        public Task OptInByHashAsync(string emailHash, string category, CancellationToken cancellationToken = default)
            => Task.CompletedTask;
        public Task<IReadOnlyList<EmailPreferenceItem>> GetForUserAsync(Guid userId, CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<EmailPreferenceItem>>([]);
    }

    private sealed class FakeUnsubscribe : IMailUnsubscribeTokenService
    {
        public string CreateToken(string email, string category, DateTime? issuedUtc = null) => "tok";
        public bool TryValidate(string? token, out string emailHash, out string category, out DateTime issuedUtc)
        {
            emailHash = "h"; category = "PushBom"; issuedUtc = DateTime.UtcNow; return true;
        }
        public string BuildUnsubscribeUrl(string publicWebBaseUrl, string email, string category)
            => publicWebBaseUrl.TrimEnd('/') + "/mail/afmelden?t=tok";
    }

    private sealed class FakeHostEnvironment : IHostEnvironment
    {
        public FakeHostEnvironment(string name) => EnvironmentName = name;
        public string EnvironmentName { get; set; }
        public string ApplicationName { get; set; } = "Jobsy.Tests";
        public string ContentRootPath { get; set; } = AppContext.BaseDirectory;
        public Microsoft.Extensions.FileProviders.IFileProvider ContentRootFileProvider { get; set; }
            = new Microsoft.Extensions.FileProviders.NullFileProvider();
    }

    private static string FindRepoRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null)
        {
            if (File.Exists(Path.Combine(dir.FullName, "Jobsy.sln")))
            {
                return dir.FullName;
            }

            dir = dir.Parent;
        }

        throw new InvalidOperationException("Jobsy.sln not found.");
    }
}
