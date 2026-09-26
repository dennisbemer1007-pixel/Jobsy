using Jobsy.Core.Enums;
using Jobsy.Core.Interfaces;
using Jobsy.Core.Rules;

namespace Jobsy.Core.Reports.Competence;

/// <summary>
/// Pure builder that turns 150 raw competence-deep-analysis Likert answers into a complete
/// <see cref="CompetenceDeepReport"/>: trait/facet scores, Big Five norm comparison, fixed
/// Dutch copy per level, occupation suggestions, and an AI or template summary/action plan.
/// </summary>
public static class CompetenceDeepReportBuilder
{
    private const int TopOccupationTraitCount = 3;

    public static CompetenceDeepReport Build(
        IReadOnlyDictionary<int, int> answers,
        INormProvider norms,
        string? jobTitle,
        IReadOnlyList<(string Title, int MatchPercent, string Reason)>? occupations,
        string? aiSummary,
        IReadOnlyList<(string Title, string Body)>? aiPlan,
        bool fromOpenAi,
        DateTime generatedAtUtc)
    {
        var domainScores = DeepAnalysisCatalog.ScoreDomains(answers, AssessmentKind.Competence)
            .ToDictionary(s => s.Domain, s => s.Percent, StringComparer.OrdinalIgnoreCase);
        var facetScores = DeepAnalysisCatalog.ScoreFacets(answers);
        var facetsByDomain = facetScores
            .GroupBy(f => f.Domain, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.ToList(), StringComparer.OrdinalIgnoreCase);

        var traits = new List<CompetenceDeepTraitReport>();
        foreach (var domain in DeepAnalysisCompetenceFacets.ReportTraitOrder)
        {
            var score = domainScores.TryGetValue(domain, out var s) ? s : 0;
            var level = CompetenceDeepReportLevels.LevelFor(score);
            var texts = CompetenceDeepReportTexts.For(domain, level);
            var normKey = DeepAnalysisCompetenceFacets.TraitNormKey(domain);

            var traitReport = new CompetenceDeepTraitReport
            {
                Domain = domain,
                LabelNl = CompetenceDeepReportTexts.LabelNl(domain),
                Score = score,
                Level = level,
                NormMean = norms.IsAvailable ? norms.Mean(normKey) : null,
                NormBand = norms.IsAvailable ? norms.BandLabel(score, normKey) : null,
                Meaning = texts?.Meaning ?? "",
                WorkQuote = texts?.WorkQuote ?? "",
                Pitfall = texts?.Pitfall ?? "",
                Tip = texts?.Tip ?? "",
                Strength = texts?.Strength ?? "",
                ThriveAtWork = texts?.ThriveAtWork ?? "",
                FittingManager = texts?.FittingManager ?? "",
                InTeam = texts?.InTeam ?? ""
            };

            if (facetsByDomain.TryGetValue(domain, out var facetsForDomain))
            {
                foreach (var facetCode in DeepAnalysisCompetenceFacets.FacetCodesForTrait(domain))
                {
                    var facet = facetsForDomain.FirstOrDefault(f =>
                        f.Facet.Equals(facetCode, StringComparison.OrdinalIgnoreCase));
                    var facetScore = facet.Facet is null ? 0 : facet.Percent;

                    traitReport.Facets.Add(new CompetenceDeepFacetReport
                    {
                        Code = facetCode,
                        LabelNl = DeepAnalysisCompetenceFacets.LabelNl(facetCode),
                        Score = facetScore,
                        NormMean = norms.IsAvailable ? norms.Mean(facetCode) : null,
                        NormBand = norms.IsAvailable ? norms.BandLabel(facetScore, facetCode) : null
                    });
                }
            }

            traits.Add(traitReport);
        }

        var resolvedOccupations = occupations is { Count: > 0 }
            ? occupations.Select(o => new CompetenceDeepOccupation
            {
                Title = o.Title,
                MatchPercent = o.MatchPercent,
                Reason = o.Reason
            }).ToList()
            : FallbackOccupations(traits);

        var summary = !string.IsNullOrWhiteSpace(aiSummary)
            ? aiSummary!
            : CompetenceDeepReportTexts.TemplateSummary(traits);

        var actionPlan = aiPlan is { Count: > 0 }
            ? aiPlan.Select(p => new CompetenceDeepActionStep { Title = p.Title, Body = p.Body }).ToList()
            : CompetenceDeepReportTexts.TemplateActionPlan(traits)
                .Select(p => new CompetenceDeepActionStep { Title = p.Title, Body = p.Body })
                .ToList();

        return new CompetenceDeepReport
        {
            ReportVersion = CompetenceDeepReportJson.CurrentReportVersion,
            GeneratedAtUtc = generatedAtUtc,
            FromOpenAi = fromOpenAi && !string.IsNullOrWhiteSpace(aiSummary),
            Summary = summary,
            Traits = traits,
            Occupations = resolvedOccupations,
            ActionPlan = actionPlan,
            NormSourceLine = norms.IsAvailable ? norms.SourceDisclaimer : null
        };
    }

    private static List<CompetenceDeepOccupation> FallbackOccupations(
        IReadOnlyList<CompetenceDeepTraitReport> traits)
    {
        var topTraits = traits
            .OrderByDescending(t => t.Score)
            .Take(TopOccupationTraitCount)
            .ToList();

        var result = new List<CompetenceDeepOccupation>();
        foreach (var trait in topTraits)
        {
            var candidates = CompetenceDeepReportTexts.OccupationsFor(trait.Domain);
            var pick = candidates.FirstOrDefault();
            if (pick.Title is null)
            {
                continue;
            }

            result.Add(new CompetenceDeepOccupation
            {
                Title = pick.Title,
                MatchPercent = trait.Score,
                Reason = pick.Reason
            });
        }

        return result;
    }
}
