using Jobsy.Api.Controllers;
using Jobsy.Core.Entities;
using Jobsy.Core.Interfaces;
using Jobsy.Core.Localization;
using Jobsy.Core.Passport;
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
        return await RenderAsync(facts, cancellationToken);
    }

    private async Task<PassportPdfFile> RenderAsync(PassportPdfFacts facts, CancellationToken cancellationToken)
    {
        var model = PassportPdfModelBuilder.Build(facts);
        var pdf = await _pdf.RenderAsync(model, cancellationToken);
        return new PassportPdfFile(pdf, _pdf.BuildFileName(model));
    }
}
