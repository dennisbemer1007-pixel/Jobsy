using Jobsy.Web.Media;

namespace Jobsy.Web.ContextualTips;

public static class ContextualTipsImages
{
    public static string UrlForKey(string? imageKey) => imageKey switch
    {
        "Tips.Art.Interview" => CandidateIllustratedImages.TipInterview,
        "Tips.Art.Contract" => CandidateIllustratedImages.TipContract,
        "Tips.Art.Cv" => CandidateIllustratedImages.TipCv,
        "Tips.Art.Activities" => CandidateIllustratedImages.TipActivities,
        "Tips.Art.Books" => CandidateIllustratedImages.TipBooks,
        "Tips.Art.Salary" => CandidateIllustratedImages.TipSalary,
        "Tips.Art.WorkBenefits" => CandidateIllustratedImages.TipWorkBenefits,
        _ => CandidateIllustratedImages.TipInterview
    };
}
