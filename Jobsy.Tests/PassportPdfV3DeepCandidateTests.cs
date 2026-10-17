using Jobsy.Api.Passport;
using Jobsy.Core.Entities;
using Jobsy.Core.Enums;
using Jobsy.Core.Interfaces;
using Jobsy.Core.Options;
using Jobsy.Core.Reports.Competence;
using Jobsy.Core.Rules;
using Jobsy.Infrastructure.Data;
using Jobsy.Infrastructure.Services.Passport;
using Microsoft.EntityFrameworkCore;
using UglyToad.PdfPig;

namespace Jobsy.Tests;

/// <summary>
/// Regression: DNA paspoort PDF for candidates on Tests v3 + completed deep competence report.
/// </summary>
public class PassportPdfV3DeepCandidateTests
{
    [Fact]
    public async Task Api_me_lobsy_cv_pdf_generates_dna_passport_for_v3_deep_candidate()
    {
        await using var db = CreateDb();
        var userId = Guid.Parse("f3a10000-0000-0000-0000-000000000001");
        var completedAt = new DateTime(2026, 10, 10, 12, 0, 0, DateTimeKind.Utc);

        db.Users.Add(new User
        {
            Id = userId,
            Email = "v3-deep@jobsy.local",
            FullName = "V3 Deep Kandidaat",
            Role = UserRole.Candidate,
            IsActive = true,
            OpenForWork = true,
            EmailVerifiedAtUtc = completedAt,
            PreferencesJson =
                """{"language":"nl","roles":["Orderpicker"],"aboutMe":"Ik werk graag praktisch.","maxTravelMinutes":30}"""
        });

        db.CandidateCompetencies.Add(new CandidateCompetency
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Status = CandidateCompetencyStatuses.Completed,
            CompletedAtUtc = completedAt,
            QuickScanVersion = AssessmentTestVersions.V3,
            SamenwerkenPercent = 72,
            ResultaatgerichtheidPercent = 55,
            StressbestendigheidPercent = 48,
            InnovatiePercent = 41,
            ExtraversiePercent = 60
        });

        var deepAnswers = DeepAnalysisCatalog.Questions.ToDictionary(q => q.Id, _ => 4);
        var deepReport = CompetenceDeepReportBuilder.Build(
            deepAnswers,
            new FakeNormProvider(),
            jobTitle: null,
            occupations: null,
            aiSummary: null,
            aiPlan: null,
            fromOpenAi: false,
            generatedAtUtc: completedAt);

        db.CandidateDeepAnalyses.Add(new CandidateDeepAnalysis
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Kind = AssessmentKind.Competence,
            Status = CandidateDeepAnalysisStatuses.Completed,
            CompletedAtUtc = completedAt,
            ReportGeneratedAtUtc = completedAt,
            AnswersJson = DeepAnalysisCatalog.SerializeAnswers(deepAnswers, AssessmentKind.Competence),
            ReportJson = CompetenceDeepReportJson.Serialize(deepReport),
            ReportVersion = CompetenceDeepReportJson.CurrentReportVersion,
            UpdatedAtUtc = completedAt
        });

        await db.SaveChangesAsync();

        var user = await db.Users.AsNoTracking().SingleAsync(u => u.Id == userId);
        var download = new PassportPdfDownload(
            db,
            new PassportDnaReader(db),
            new PassportPdfService(new FakeCompanySettings()),
            new EmptyMatchesCompetencyService());

        var file = await download.ForCandidateAsync(user, phoneVerificationRequired: false, showDnaBlocks: true, CancellationToken.None);

        Assert.StartsWith("%PDF", System.Text.Encoding.ASCII.GetString(file.Pdf.AsSpan(0, 4)));
        Assert.Contains("DNA-paspoort", file.FileName, StringComparison.OrdinalIgnoreCase);

        using var doc = PdfDocument.Open(file.Pdf);
        Assert.True(doc.NumberOfPages >= 2);
        var text = string.Join("\n", doc.GetPages().Select(p => p.Text));
        Assert.Contains("V3 Deep Kandidaat", text, StringComparison.Ordinal);
        Assert.Contains("DNA-paspoort", text, StringComparison.Ordinal);
        Assert.DoesNotContain("%", text, StringComparison.Ordinal);
    }

    private static JobsyDbContext CreateDb()
    {
        var options = new DbContextOptionsBuilder<JobsyDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .Options;
        return new JobsyDbContext(options);
    }

    private sealed class FakeCompanySettings : IPlatformCompanySettingsService
    {
        public Task<PlatformCompanySnapshot> GetAsync(CancellationToken cancellationToken = default)
            => Task.FromResult(new PlatformCompanySnapshot(
                "Lobsy", "Test", null, null, null, null, null, null, null, null, null, null));

        public Task<PlatformCompanySnapshot> UpdateAsync(PlatformCompanyUpdate update, CancellationToken cancellationToken = default)
            => GetAsync(cancellationToken);

        public byte[] GetBrandLogoPng() => [];

        public byte[] GetBrandWatermarkPng() => [];
    }

    private sealed class FakeNormProvider : INormProvider
    {
        public bool IsAvailable => false;
        public BigFiveNormSet? Norms => null;
        public string? BandLabel(double score, string traitOrFacetKey) => null;
        public double? Mean(string traitOrFacetKey) => null;
        public string SourceDisclaimer => "Test norms.";
    }

    private sealed class EmptyMatchesCompetencyService : ICandidateCompetencyService
    {
        public Task<CandidateCompetencyStateDto> GetAsync(Guid userId, CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public Task<CandidateCompetencyStateDto> SaveAsync(
            Guid userId,
            IReadOnlyDictionary<int, int> answers,
            bool complete,
            CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public Task<CompetencyScores?> GetCompletedScoresAsync(Guid userId, CancellationToken cancellationToken = default)
            => Task.FromResult<CompetencyScores?>(null);

        public Task<IReadOnlyList<CandidateMatchedVacancyDto>> GetTopMatchesAsync(
            Guid userId,
            CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<CandidateMatchedVacancyDto>>([]);
    }
}
