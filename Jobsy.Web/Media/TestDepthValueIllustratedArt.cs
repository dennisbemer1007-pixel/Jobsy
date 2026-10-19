using Jobsy.Core.Rules;

namespace Jobsy.Web.Media;

/// <summary>Stijl A spot art for test-depth mockup screens (soft fade, no hard crops).</summary>
public static class TestDepthValueIllustratedArt
{
    public static (string Url, string Position) ForCompetencyDomain(string domainCode)
    {
        var code = domainCode.Trim().ToUpperInvariant();
        if (code == CompetencyTestCatalog.Stressbestendigheid.ToUpperInvariant())
        {
            return (CandidateIllustratedImages.SpotJobZorg, "72% 38%");
        }

        if (code == CompetencyTestCatalog.Samenwerken.ToUpperInvariant())
        {
            return (CandidateIllustratedImages.SpotInclusiefZorgLogistiek, "68% 42%");
        }

        if (code == CompetencyTestCatalog.Extraversie.ToUpperInvariant())
        {
            return (CandidateIllustratedImages.StepBanen, "58% 40%");
        }

        if (code == CompetencyTestCatalog.Innovatie.ToUpperInvariant())
        {
            return (CandidateIllustratedImages.StepDna, "60% 44%");
        }

        if (code == CompetencyTestCatalog.Resultaatgerichtheid.ToUpperInvariant())
        {
            return (CandidateIllustratedImages.SpotJobLogistiek, "65% 36%");
        }

        return (CandidateIllustratedImages.SpotVragen, "62% 38%");
    }

    public static (string Url, string Position) ForMiniMirrorHero()
        => (CandidateIllustratedImages.StepVragen, "78% 42%");

    public static (string Url, string Position) ForUnlockNextPreview(string previewVariant)
        => previewVariant switch
        {
            "10" => (CandidateIllustratedImages.StepDna, "70% 40%"),
            "25" => (CandidateIllustratedImages.MobFiguresKompas, "88% 55%"),
            "150" => (CandidateIllustratedImages.DroombaanProfessionWide, "75% 45%"),
            _ => (CandidateIllustratedImages.TipVragen, "72% 38%")
        };

    public static (string Url, string Position) DeepReportWork()
        => (CandidateIllustratedImages.SpotWerkervaring, "70% 35%");

    public static (string Url, string Position) DeepReportOccupations()
        => (CandidateIllustratedImages.SpotJobLogistiek, "68% 40%");

    public static (string Url, string Position) DeepReportActionPlan()
        => (CandidateIllustratedImages.SpotOpleiding, "72% 38%");

    public static string InlineBackground(string url, string position)
        => $"background-image:url('{url}');background-position:{position};";
}
