using Jobsy.Core.Enums;
using Jobsy.Core.Rules;

namespace Jobsy.Web.Media;

public static class TestDepthValueRewardArt
{
    public static (string Url, string Position) ForVariant(string variant)
        => variant switch
        {
            "5" => (CandidateIllustratedImages.StepVragen, "62% 38%"),
            "10" => (CandidateIllustratedImages.StepDna, "58% 42%"),
            "25" => (CandidateIllustratedImages.KompasHeroWide, "72% 40%"),
            "150" => (CandidateIllustratedImages.FinaleHero, "55% 42%"),
            _ => (CandidateIllustratedImages.StepVragen, "62% 38%")
        };

    public static (string Url, string Position) ForState(TestDepthLevel? reached, bool deepCompleted)
    {
        var variant = TestDepthValueRewardCopy.IllustrationVariant(reached, deepCompleted);
        return ForVariant(variant);
    }
}
