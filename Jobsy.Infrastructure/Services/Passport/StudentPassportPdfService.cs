using System.Text.Json;
using Jobsy.Core.Contracts;
using Jobsy.Core.Entities;
using Jobsy.Core.Interfaces;
using Jobsy.Core.Localization;
using Jobsy.Core.Passport;

namespace Jobsy.Infrastructure.Services.Passport;

/// <summary>Student/stagiair DNA-paspoort (2-page v3 layout). Top vacancies from live matching when available.</summary>
public sealed class StudentPassportPdfService : IStudentPassportPdfService
{
    private static readonly JsonSerializerOptions PreferenceJson = new() { PropertyNameCaseInsensitive = true };

    private readonly IPassportDnaReader _dna;
    private readonly IPassportPdfService _pdf;
    private readonly ICandidateCompetencyService _competencies;

    public StudentPassportPdfService(
        IPassportDnaReader dna,
        IPassportPdfService pdf,
        ICandidateCompetencyService competencies)
    {
        _dna = dna;
        _pdf = pdf;
        _competencies = competencies;
    }

    public async Task<(byte[] Pdf, string FileName)> RenderForCandidateAsync(
        User user,
        bool phoneVerificationRequired,
        CancellationToken cancellationToken = default)
    {
        var preferences = ParsePreferences(user.PreferencesJson);
        var language = JobsyLanguages.Normalize(preferences.Language);
        var layers = await _dna.ReadAsync(user.Id, language, cancellationToken);
        var facts = PassportPdfFactsFactory.FromUser(
            user,
            preferences,
            layers,
            includeContact: true,
            phoneVerificationRequired,
            DateTime.UtcNow);

        var matches = await _competencies.GetTopMatchesAsync(user.Id, cancellationToken);
        var jobFits = matches.Take(3).Select(m => new PassportJobFit(
            m.Title,
            m.Why.Count > 0 ? string.Join(" ", m.Why) : m.CompanyName)).ToList();

        var baseModel = PassportPdfModelBuilder.Build(facts);
        var model = baseModel with
        {
            JobFits = jobFits.Count > 0 ? jobFits : baseModel.JobFits
        };

        var pdf = await _pdf.RenderAsync(model, cancellationToken);
        return (pdf, _pdf.BuildFileName(model));
    }

    public byte[] RenderDemoPdf(bool sparse = false)
    {
        var model = PassportPdfModelBuilder.Build(StudentPassportDemoSeed.YaraFacts(sparse));
        return _pdf.RenderAsync(model).GetAwaiter().GetResult();
    }

    public byte[] RenderDemoPagePng(int pageNumber, int dpi, bool sparse = false)
    {
        var model = PassportPdfModelBuilder.Build(StudentPassportDemoSeed.YaraFacts(sparse));
        var pages = _pdf.RenderPageImagesAsync(model, dpi).GetAwaiter().GetResult();
        var index = Math.Clamp(pageNumber, 1, pages.Count) - 1;
        return pages[index];
    }

    private static CandidatePreferencesDto ParsePreferences(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return EmptyPreferences();
        }

        try
        {
            return JsonSerializer.Deserialize<CandidatePreferencesDto>(json, PreferenceJson)
                   ?? EmptyPreferences();
        }
        catch (JsonException)
        {
            return EmptyPreferences();
        }
    }

    private static CandidatePreferencesDto EmptyPreferences() => new([], null, null);
}
