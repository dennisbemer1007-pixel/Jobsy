using System.Text.Json;
using Jobsy.Core;
using Jobsy.Core.Email;
using Jobsy.Core.Entities;
using Jobsy.Core.Enums;
using Jobsy.Core.Interfaces;
using Jobsy.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Jobsy.Infrastructure.Services;

public sealed class EmailCatalogService : IEmailCatalogService
{
    private readonly IEmailService _email;
    private readonly IPlatformFeatureService _features;
    private readonly JobsyDbContext _db;
    private readonly IConfiguration _configuration;
    private readonly ILogger<EmailCatalogService> _logger;

    public EmailCatalogService(
        IEmailService email,
        IPlatformFeatureService features,
        JobsyDbContext db,
        IConfiguration configuration,
        ILogger<EmailCatalogService> logger)
    {
        _email = email;
        _features = features;
        _db = db;
        _configuration = configuration;
        _logger = logger;
    }

    public IReadOnlyList<EmailTemplateInfo> ListTemplates()
        => TransactionalEmails.Templates;

    public async Task<EmailCatalogSendResult> SendAsync(
        string key,
        string to,
        CancellationToken cancellationToken = default)
    {
        var trimmed = (to ?? string.Empty).Trim();
        if (!LooksLikeEmail(trimmed))
        {
            return new EmailCatalogSendResult(
                key,
                key,
                "",
                "",
                false,
                false,
                "Vul een geldig e-mailadres in.");
        }

        if (!TransactionalEmails.TryGet(key, out var info))
        {
            return new EmailCatalogSendResult(
                key,
                key,
                "",
                "",
                false,
                false,
                "Onbekend mailtype.");
        }

        var ctx = await BuildContextAsync(trimmed, cancellationToken);
        var composed = TransactionalEmails.Compose(info.Key, ctx);
        var delivery = await _email.SendAsync(
            new EmailMessage(trimmed, composed.Subject, composed.Html, composed.Category),
            cancellationToken);

        var redacted = EmailServiceStub.RedactEmail(trimmed);
        _db.PlatformLogs.Add(new PlatformLog
        {
            Id = Guid.NewGuid(),
            Level = PlatformLogLevel.Info,
            Category = "EmailCatalogTest",
            Message = $"Admin testmail {info.Key} → {redacted}: {composed.Subject}",
            DetailsJson = JsonSerializer.Serialize(new
            {
                Template = info.Key,
                To = redacted,
                composed.Category,
                composed.Subject,
                BodyLength = composed.Html.Length,
                delivery.DeliveredViaProvider
            }),
            CreatedAt = DateTime.UtcNow
        });
        await _db.SaveChangesAsync(cancellationToken);

        var via = delivery.DeliveredViaProvider ? "Resend/SMTP" : "PlatformLog (stub)";
        var message = delivery.DeliveredViaProvider
            ? $"Verzonden naar {redacted} via {via}."
            : $"Alleen gelogd naar {redacted} via stub — configureer Mail in Integraties voor echte aflevering.";

        _logger.LogInformation(
            "Admin catalog mail {Key} to {To} via {Kind}",
            info.Key,
            redacted,
            delivery.Kind);

        return new EmailCatalogSendResult(
            info.Key,
            info.Title,
            info.Category,
            composed.Subject,
            true,
            delivery.DeliveredViaProvider,
            message);
    }

    public async Task<IReadOnlyList<EmailCatalogSendResult>> SendAllAsync(
        string to,
        CancellationToken cancellationToken = default)
    {
        var trimmed = (to ?? string.Empty).Trim();
        if (!LooksLikeEmail(trimmed))
        {
            return
            [
                new EmailCatalogSendResult(
                    "",
                    "",
                    "",
                    "",
                    false,
                    false,
                    "Vul een geldig e-mailadres in.")
            ];
        }

        var results = new List<EmailCatalogSendResult>(TransactionalEmails.Templates.Count);
        foreach (var template in TransactionalEmails.Templates)
        {
            cancellationToken.ThrowIfCancellationRequested();
            results.Add(await SendAsync(template.Key, trimmed, cancellationToken));
        }

        return results;
    }

    private async Task<EmailSampleContext> BuildContextAsync(string to, CancellationToken cancellationToken)
    {
        var features = await _features.GetAsync(cancellationToken);
        var preview = EmailSampleContext.ForPreview(features.PublicWebBaseUrl, to);

        var published = await _db.Vacancies.AsNoTracking()
            .Where(v => v.Status == VacancyStatus.Active)
            .OrderByDescending(v => v.PublishedAtUtc ?? v.CreatedAtUtc)
            .Select(v => new { v.Id, v.Title, CompanyName = v.Company.Name, v.Company.Address })
            .FirstOrDefaultAsync(cancellationToken);

        var apiBase = string.IsNullOrWhiteSpace(_configuration["PublicApiBaseUrl"])
            ? "https://api.lobsy.nl"
            : JobsyPublicUrl.NormalizeOrigin(_configuration["PublicApiBaseUrl"]).TrimEnd('/');

        if (published is null)
        {
            return preview with { ApiBaseUrl = apiBase };
        }

        return preview with
        {
            VacancyId = published.Id,
            VacancyTitle = published.Title,
            CompanyName = published.CompanyName,
            LocationLabel = string.IsNullOrWhiteSpace(published.Address) ? preview.LocationLabel : published.Address,
            EstablishmentName = published.CompanyName,
            ApiBaseUrl = apiBase
        };
    }

    internal static bool LooksLikeEmail(string value)
    {
        if (string.IsNullOrWhiteSpace(value) || value.Length > 254)
        {
            return false;
        }

        var at = value.IndexOf('@');
        return at > 0
            && at < value.Length - 1
            && value.IndexOf('@', at + 1) < 0
            && value.Contains('.', StringComparison.Ordinal);
    }
}
