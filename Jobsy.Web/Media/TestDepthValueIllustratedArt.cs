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
            return (CandidateIllustratedImages.MobFiguresOpleiding, "center bottom");
        }

        if (code == CompetencyTestCatalog.Samenwerken.ToUpperInvariant())
        {
            return (CandidateIllustratedImages.MobFiguresKompas, "center bottom");
        }

        if (code == CompetencyTestCatalog.Extraversie.ToUpperInvariant())
        {
            return (CandidateIllustratedImages.MobFiguresDroombaan, "center bottom");
        }

        if (code == CompetencyTestCatalog.Innovatie.ToUpperInvariant())
        {
            return (CandidateIllustratedImages.MobFiguresKompas, "center bottom");
        }

        if (code == CompetencyTestCatalog.Resultaatgerichtheid.ToUpperInvariant())
        {
            return (CandidateIllustratedImages.MobFiguresSollicitaties, "center bottom");
        }

        return (CandidateIllustratedImages.MobFiguresOpleiding, "center bottom");
    }

    public static (string Url, string Position) ForUnlockNow(TestDepthLevel reached)
        => reached switch
        {
            TestDepthLevel.First => (CandidateIllustratedImages.MobFiguresOpleiding, "center bottom"),
            TestDepthLevel.Deeper => (CandidateIllustratedImages.MobFiguresKompas, "center bottom"),
            TestDepthLevel.Full => (CandidateIllustratedImages.MobFiguresDroombaan, "center bottom"),
            _ => (CandidateIllustratedImages.StepVragen, "center bottom")
        };

    public static bool ShowUnlockNextFigure(TestDepthLevel? next)
        => next is TestDepthLevel.Deeper or TestDepthLevel.Full or TestDepthLevel.Bottom;

    public static (string Url, string Position) ForUnlockNextPreview(string previewVariant)
        => previewVariant switch
        {
            "10" => (CandidateIllustratedImages.MobFiguresKompas, "center bottom"),
            "25" => (CandidateIllustratedImages.MobFiguresDroombaan, "center bottom"),
            "150" => (CandidateIllustratedImages.MobFiguresOpleiding, "center bottom"),
            _ => (CandidateIllustratedImages.MobFiguresOpleiding, "center bottom")
        };

    public static (string Url, string Position) DeepReportWork()
        => (CandidateIllustratedImages.MobFiguresSollicitaties, "center bottom");

    public static (string Url, string Position) DeepReportOccupations()
        => (CandidateIllustratedImages.MobFiguresDroombaan, "center bottom");

    public static (string Url, string Position) DeepReportActionPlan()
        => (CandidateIllustratedImages.MobFiguresOpleiding, "center bottom");

    public static string InlineBackground(string url, string position)
        => $"background-image:url('{url}');background-position:{position};";

    public static string InlineFigure(string url, string position)
        => $"background-image:url('{url}');background-position:{position};background-size:contain;background-repeat:no-repeat;";
}
