using Jobsy.Core.Enums;
using Jobsy.Core.Rules;

namespace Jobsy.Web.Media;

public static class TestDepthValueRewardArt
{
    public static (string Url, string Position) ForVariant(string variant)
        => variant switch
        {
            "5" => (CandidateIllustratedImages.MobFiguresOpleiding, "center bottom"),
            "10" => (CandidateIllustratedImages.MobFiguresKompas, "center bottom"),
            "25" => (CandidateIllustratedImages.MobFiguresDroombaan, "center bottom"),
            "150" => (CandidateIllustratedImages.MobFiguresDroombaan, "center bottom"),
            _ => (CandidateIllustratedImages.MobFiguresOpleiding, "center bottom")
        };

    public static (string Url, string Position) ForState(TestDepthLevel? reached, bool deepCompleted)
    {
        var variant = TestDepthValueRewardCopy.IllustrationVariant(reached, deepCompleted);
        return ForVariant(variant);
    }
}
