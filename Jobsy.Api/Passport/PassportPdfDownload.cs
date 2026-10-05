using Jobsy.Api.Controllers;
using Jobsy.Core.Entities;
using Jobsy.Core.Interfaces;
using Jobsy.Core.Localization;
using Jobsy.Core.Passport;
using Jobsy.Core.Rules;
using Jobsy.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Jobsy.Api.Passport;

public sealed record PassportPdfFile(byte[] Pdf, string FileName);

/// <summary>Builds the DNA-paspoort from live facts. The AI story is never read.</summary>
public sealed class PassportPdfDownload
{
    private readonly JobsyDbContext _db;
    private readonly IPassportDnaReader _dna;
    private readonly IPassportPdfService _pdf;

    public PassportPdfDownload(JobsyDbContext db, IPassportDnaReader dna, IPassportPdfService pdf)
    {
        _db = db;
        _dna = dna;
        _pdf = pdf;
    }

    public async Task<PassportPdfFile> ForCandidateAsync(
        User user,
        bool phoneVerificationRequired,
        CancellationToken cancellationToken)
    {
        var preferences = MeController.ParsePreferences(user.PreferencesJson);
        var language = JobsyLanguages.Normalize(preferences.Language);
        var layers = await _dna.ReadAsync(user.Id, language, cancellationToken);
        var facts = PassportPdfFactsFactory.FromUser(
            user,
            preferences,
            layers,
            includeContact: true,
            phoneVerificationRequired,
            DateTime.UtcNow);
        facts = await WithShareFactsAsync(facts, user.Id, cancellationToken);
        return await RenderAsync(facts, cancellationToken);
    }

    public async Task<PassportPdfFile> ForApplicationAsync(
        Application application,
        bool includeDirectContact,
        bool phoneVerificationRequired,
        CancellationToken cancellationToken)
    {
        User? user = null;
        if (application.CandidateUserId is Guid userId)
        {
            user = await _db.Users.AsNoTracking()
                .FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);
        }

        var preferences = user is null ? null : MeController.ParsePreferences(user.PreferencesJson);
        var language = JobsyLanguages.Normalize(preferences?.Language);
        var layers = user is null
            ? PassportDnaLayer.None()
            : await _dna.ReadAsync(user.Id, language, cancellationToken);
        var facts = PassportPdfFactsFactory.FromApplication(
            application,
            user,
            preferences,
            layers,
            includeDirectContact,
            phoneVerificationRequired,
            DateTime.UtcNow);
        if (user is not null)
        {
            facts = await WithShareFactsAsync(facts, user.Id, cancellationToken);
        }

        return await RenderAsync(facts, cancellationToken);
    }

    private async Task<PassportPdfFacts> WithShareFactsAsync(
        PassportPdfFacts facts,
        Guid userId,
        CancellationToken cancellationToken)
    {
        if (userId == Guid.Empty)
        {
            return facts;
        }

        return facts with
        {
            DreamTitle = await LoadDreamTitleAsync(userId, cancellationToken),
            ReferenceQuotes = await LoadQuotesAsync(userId, cancellationToken)
        };
    }

    private async Task<string?> LoadDreamTitleAsync(Guid userId, CancellationToken cancellationToken)
    {
        var title = await _db.CandidateCareerPlans.AsNoTracking()
            .Where(plan => plan.UserId == userId && plan.Status == CareerPlanStatuses.Active)
            .OrderByDescending(plan => plan.UpdatedAtUtc)
            .Select(plan => plan.DreamTitle)
            .FirstOrDefaultAsync(cancellationToken);
        return string.IsNullOrWhiteSpace(title) ? null : title.Trim();
    }

    private async Task<IReadOnlyList<PassportReferenceQuote>> LoadQuotesAsync(
        Guid userId,
        CancellationToken cancellationToken)
    {
        var references = await _db.CandidateReferences.AsNoTracking()
            .Where(reference => reference.UserId == userId)
            .OrderBy(reference => reference.SortOrder)
            .Select(reference => new { reference.Id, reference.EmployerName, reference.ContactName })
            .ToListAsync(cancellationToken);
        if (references.Count == 0)
        {
            return [];
        }

        var ids = references.Select(reference => reference.Id).ToList();
        var confirmations = await _db.ReferenceConfirmations.AsNoTracking()
            .Where(confirmation => ids.Contains(confirmation.CandidateReferenceId))
            .ToListAsync(cancellationToken);
        var byReference = confirmations.ToDictionary(confirmation => confirmation.CandidateReferenceId);
        var quotes = new List<PassportReferenceQuote>();
        foreach (var reference in references)
        {
            if (!byReference.TryGetValue(reference.Id, out var confirmation))
            {
                continue;
            }

            var fact = ReferenceConfirmationRules.ForPartner(
                confirmation,
                reference.EmployerName,
                reference.ContactName);
            if (fact is null)
            {
                continue;
            }

            var quote = string.IsNullOrWhiteSpace(fact.DidWell) ? fact.Extra : fact.DidWell;
            if (string.IsNullOrWhiteSpace(quote))
            {
                continue;
            }

            var attribution = string.IsNullOrWhiteSpace(fact.RefereeName)
                ? fact.EmployerName.Trim()
                : fact.RefereeName.Trim() + " · " + fact.EmployerName.Trim();
            quotes.Add(new PassportReferenceQuote(attribution, quote.Trim()));
            if (quotes.Count == 2)
            {
                break;
            }
        }

        return quotes;
    }

    private async Task<PassportPdfFile> RenderAsync(PassportPdfFacts facts, CancellationToken cancellationToken)
    {
        var model = PassportPdfModelBuilder.Build(facts);
        var pdf = await _pdf.RenderAsync(model, cancellationToken);
        return new PassportPdfFile(pdf, _pdf.BuildFileName(model));
    }
}
