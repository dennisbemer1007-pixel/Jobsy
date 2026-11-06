using Jobsy.Core.Enums;
using Jobsy.Core.Entities;
using Jobsy.Core.Interfaces;
using Jobsy.Core.Localization;
using Jobsy.Core.Options;
using Jobsy.Core.Reports.TestReflection;
using Jobsy.Core.Rules;
using Jobsy.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Jobsy.Infrastructure.Services;

public sealed class TestReflectionService : ITestReflectionService
{
    private readonly JobsyDbContext _db;
    private readonly ITestReflectionAiService _ai;
    private readonly JobsyFeatureOptions _features;

    public TestReflectionService(
        JobsyDbContext db,
        ITestReflectionAiService ai,
        IOptions<JobsyFeatureOptions> features)
    {
        _db = db;
        _ai = ai;
        _features = features.Value;
    }

    public async Task<TestReflectionState> GetFullMirrorAsync(
        Guid userId,
        AssessmentKind kind,
        string language,
        CancellationToken cancellationToken = default)
    {
        var enabled = _features.TestReflectionEnabled;
        if (!enabled)
        {
            return new TestReflectionState(null, null, false, false);
        }

        var snapshot = await TestReflectionScoreLoader.LoadFullAsync(_db, userId, kind, cancellationToken);
        if (snapshot is null)
        {
            return new TestReflectionState(null, null, true, false);
        }

        var lang = JobsyLanguages.Normalize(language);
        if (lang is "" or "nl")
        {
            lang = "nl";
        }
        else if (lang != "en")
        {
            lang = "en";
        }

        var hash = TestReflectionTemplateBuilder.ComputeSourceHash(snapshot);
        var row = await _db.CandidateTestReflections
            .FirstOrDefaultAsync(
                r => r.UserId == userId
                     && r.Kind == kind
                     && r.Level == TestDepthLevel.Full
                     && r.Language == lang,
                cancellationToken);

        TestReflectionFull? mirror;
        if (row is not null && row.SourceHash == hash)
        {
            mirror = TestReflectionJson.Deserialize(row.ContentJson);
            if (mirror is null)
            {
                mirror = await GenerateAndStoreAsync(userId, kind, lang, snapshot, hash, row, cancellationToken);
            }
        }
        else
        {
            mirror = await GenerateAndStoreAsync(userId, kind, lang, snapshot, hash, row, cancellationToken);
        }

        var feedback = ParseFeedback(row?.Feedback);
        return new TestReflectionState(mirror, feedback, true, true);
    }

    public async Task SaveFeedbackAsync(
        Guid userId,
        AssessmentKind kind,
        TestDepthLevel level,
        TestReflectionFeedback feedback,
        CancellationToken cancellationToken = default)
    {
        if (!_features.TestReflectionEnabled)
        {
            return;
        }

        var row = await _db.CandidateTestReflections
            .FirstOrDefaultAsync(
                r => r.UserId == userId && r.Kind == kind && r.Level == level,
                cancellationToken);
        if (row is null)
        {
            return;
        }

        row.Feedback = feedback.ToString();
        row.UpdatedAtUtc = DateTime.UtcNow;
        await _db.SaveChangesAsync(cancellationToken);
    }

    private async Task<TestReflectionFull> GenerateAndStoreAsync(
        Guid userId,
        AssessmentKind kind,
        string lang,
        TestReflectionScoreSnapshot snapshot,
        string hash,
        CandidateTestReflection? row,
        CancellationToken cancellationToken)
    {
        var testTitle = TestTitleForKind(kind, lang);
        var template = TestReflectionTemplateBuilder.BuildFull(snapshot, testTitle, lang);
        var sheet = BuildFactSheet(snapshot, lang);
        var mirror = await _ai.TryRewriteAsync(template, sheet, cancellationToken) ?? template;
        mirror = ValidateMirror(mirror, snapshot, sheet) ?? template;

        var now = DateTime.UtcNow;
        if (row is null)
        {
            row = new CandidateTestReflection
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                Kind = kind,
                Level = TestDepthLevel.Full,
                Language = lang,
                GeneratedAtUtc = now
            };
            _db.CandidateTestReflections.Add(row);
        }

        row.ContentJson = TestReflectionJson.Serialize(mirror);
        row.SourceHash = hash;
        row.FromOpenAi = mirror.FromOpenAi;
        row.UpdatedAtUtc = now;
        await _db.SaveChangesAsync(cancellationToken);
        return mirror;
    }

    private static TestReflectionFull? ValidateMirror(
        TestReflectionFull mirror,
        TestReflectionScoreSnapshot snapshot,
        CandidateFactSheet sheet)
    {
        var text = TestReflectionJson.FlattenForGuard(mirror);
        if (CandidateFactGuard.RejectionReason(text, sheet) is not null)
        {
            return null;
        }

        if (CandidateFactGuard.MirrorCopyRejection(text) is not null)
        {
            return null;
        }

        var traits = snapshot.Traits
            .Select(t => (Label: DimensionLabels.For(t.Code, mirror.Language), Score: t.Percent))
            .ToList();
        if (CandidateFactGuard.BelowAveragePraise(text, traits) is not null)
        {
            return null;
        }

        return mirror;
    }

    private static CandidateFactSheet BuildFactSheet(TestReflectionScoreSnapshot snapshot, string lang)
    {
        var scoreLines = snapshot.Traits
            .Select(t => $"{DimensionLabels.For(t.Code, lang)}: {t.Percent}% ({CandidateFactSheet.ScoreBand(t.Percent)})")
            .ToList();
        var jobs = string.IsNullOrWhiteSpace(snapshot.DreamTitle)
            ? Array.Empty<string>()
            : new[] { snapshot.DreamTitle.Trim() };
        return CandidateFactSheet.Personal([], [], [], jobs, scoreLines, checkJobTitles: false);
    }

    private static string TestTitleForKind(AssessmentKind kind, string lang)
        => kind switch
        {
            AssessmentKind.Career => lang == "en" ? "what you enjoy" : "wat je leuk vindt",
            AssessmentKind.Culture => lang == "en" ? "where you feel at home" : "waar je je thuis voelt",
            AssessmentKind.Values => lang == "en" ? "what matters to you" : "wat je belangrijk vindt",
            _ => lang == "en" ? "how you work" : "hoe je werkt"
        };

    private static TestReflectionFeedback? ParseFeedback(string? raw)
        => raw switch
        {
            nameof(TestReflectionFeedback.Yes) => TestReflectionFeedback.Yes,
            nameof(TestReflectionFeedback.Partial) => TestReflectionFeedback.Partial,
            nameof(TestReflectionFeedback.No) => TestReflectionFeedback.No,
            _ => null
        };
}
