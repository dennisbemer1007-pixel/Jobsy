using System.Text;
using System.Text.Json;
using Jobsy.Core.Interfaces;
using Jobsy.Core.Rules;
using Jobsy.Infrastructure.Data;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;

namespace Jobsy.Infrastructure.Services.CandidateExternalVacancies;

public sealed class ExternalVacancySuppressionService : IExternalVacancySuppressionService
{
    public const string ProtectorPurpose = "Lobsy.ExternalVacancy.Unsubscribe.v1";

    private readonly JobsyDbContext _db;
    private readonly IDataProtector _protector;

    public ExternalVacancySuppressionService(JobsyDbContext db, IDataProtectionProvider dataProtection)
    {
        _db = db;
        _protector = dataProtection.CreateProtector(ProtectorPurpose);
    }

    public async Task<bool> IsSuppressedAsync(string normalizedEmail, CancellationToken cancellationToken = default)
    {
        var email = CandidateExternalVacancyRules.NormalizeEmployerEmail(normalizedEmail);
        var domain = CandidateExternalVacancyRules.NormalizeDomain(email);
        return await _db.OutboundRecipientSuppressions.AsNoTracking()
            .AnyAsync(
                s => s.NormalizedEmail == email || s.NormalizedDomain == domain,
                cancellationToken);
    }

    public async Task SuppressAsync(string normalizedEmail, string reason, CancellationToken cancellationToken = default)
    {
        var email = CandidateExternalVacancyRules.NormalizeEmployerEmail(normalizedEmail);
        var domain = CandidateExternalVacancyRules.NormalizeDomain(email);
        if (await _db.OutboundRecipientSuppressions.AnyAsync(s => s.NormalizedEmail == email, cancellationToken))
        {
            return;
        }

        _db.OutboundRecipientSuppressions.Add(new Core.Entities.OutboundRecipientSuppression
        {
            NormalizedEmail = email,
            NormalizedDomain = domain,
            Reason = string.IsNullOrWhiteSpace(reason) ? "unsubscribe" : reason.Trim()[..Math.Min(500, reason.Trim().Length)],
            CreatedAtUtc = DateTime.UtcNow
        });
        await _db.SaveChangesAsync(cancellationToken);
    }

    public string CreateUnsubscribeToken(string normalizedEmail)
    {
        var payload = JsonSerializer.Serialize(new { email = CandidateExternalVacancyRules.NormalizeEmployerEmail(normalizedEmail) });
        var protectedBytes = _protector.Protect(Encoding.UTF8.GetBytes(payload));
        return Convert.ToBase64String(protectedBytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');
    }

    public bool TryValidateUnsubscribeToken(string? token, out string normalizedEmail)
    {
        normalizedEmail = "";
        if (string.IsNullOrWhiteSpace(token))
        {
            return false;
        }

        try
        {
            var padded = token.Replace('-', '+').Replace('_', '/');
            padded += new string('=', (4 - padded.Length % 4) % 4);
            var bytes = Convert.FromBase64String(padded);
            var json = Encoding.UTF8.GetString(_protector.Unprotect(bytes));
            using var doc = JsonDocument.Parse(json);
            if (!doc.RootElement.TryGetProperty("email", out var el) || el.ValueKind != JsonValueKind.String)
            {
                return false;
            }

            normalizedEmail = CandidateExternalVacancyRules.NormalizeEmployerEmail(el.GetString() ?? "");
            return normalizedEmail.Contains('@', StringComparison.Ordinal);
        }
        catch
        {
            return false;
        }
    }
}
