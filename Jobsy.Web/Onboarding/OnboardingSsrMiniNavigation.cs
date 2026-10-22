using Jobsy.Web.Navigation;

namespace Jobsy.Web.Onboarding;

/// <summary>One-question-at-a-time SSR flow for onboarding mini steps 7–10 (?q=1..5).</summary>
internal static class OnboardingSsrMiniNavigation
{
    public static int ResolveActiveQuestionIndex(
        IReadOnlyList<OnboardingClientStepSeed.MiniQuestionSeed> questions,
        IReadOnlyDictionary<int, int> answers,
        int? qQuery)
    {
        if (questions.Count == 0)
        {
            return 0;
        }

        if (qQuery is int qv && qv >= 1 && qv <= questions.Count)
        {
            return qv - 1;
        }

        for (var i = 0; i < questions.Count; i++)
        {
            var id = questions[i].Id;
            if (!answers.TryGetValue(id, out var score) || score is < 1 or > 5)
            {
                return i;
            }
        }

        return questions.Count - 1;
    }

    public static string StepQuestionUrl(int step, int q1Based)
        => $"{OnboardingRoutes.StepUrl(step)}&q={q1Based}";

    public static int ReadQuestionFromRequest(HttpContext http)
    {
        if (http.Request.HasFormContentType
            && http.Request.Form.TryGetValue("obMiniQ", out var formQ)
            && int.TryParse(formQ.ToString(), out var fq)
            && fq is >= 1 and <= 5)
        {
            return fq;
        }

        if (http.Request.Query.TryGetValue("q", out var queryQ)
            && int.TryParse(queryQ.ToString(), out var qq)
            && qq is >= 1 and <= 5)
        {
            return qq;
        }

        return 1;
    }
}
