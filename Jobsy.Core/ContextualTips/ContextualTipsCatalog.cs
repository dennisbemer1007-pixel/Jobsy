namespace Jobsy.Core.ContextualTips;

public enum ContextualTipCategoryId
{
    Interview,
    Motivation,
    PassportCv,
    Contract,
    Salary,
    Development,
    WorkBenefits
}

public enum ContextualTipPlacement
{
    None,
    InterviewScheduled,
    OfferReceived,
    PassportEducationEmpty,
    CareerRoute,
    HubNowForYou
}

public sealed record ContextualTipCategory(
    ContextualTipCategoryId Id,
    string Slug,
    string TitleKey,
    string LeadKey,
    string ImageKey,
    int TipCount);

public sealed record ContextualTipBlock(string TitleKey, string BodyKey);

public sealed record ContextualTipDefinition(
    string Id,
    ContextualTipCategoryId Category,
    string Slug,
    string TitleKey,
    string SummaryKey,
    IReadOnlyList<ContextualTipBlock> Blocks,
    ContextualTipPlacement Placement = ContextualTipPlacement.None,
    string? RelatedTipId = null);

public static class ContextualTipsCatalog
{
    public static readonly IReadOnlyList<ContextualTipCategory> Categories =
    [
        new(ContextualTipCategoryId.Interview, "sollicitatiegesprek", "Tips.Cat.Interview.Title", "Tips.Cat.Interview.Lead", "Tips.Art.Interview", 8),
        new(ContextualTipCategoryId.Motivation, "motivatie", "Tips.Cat.Motivation.Title", "Tips.Cat.Motivation.Lead", "Tips.Art.Activities", 5),
        new(ContextualTipCategoryId.PassportCv, "cv-en-paspoort", "Tips.Cat.Passport.Title", "Tips.Cat.Passport.Lead", "Tips.Art.Cv", 6),
        new(ContextualTipCategoryId.Contract, "contract", "Tips.Cat.Contract.Title", "Tips.Cat.Contract.Lead", "Tips.Art.Contract", 7),
        new(ContextualTipCategoryId.Salary, "salaris", "Tips.Cat.Salary.Title", "Tips.Cat.Salary.Lead", "Tips.Art.Salary", 5),
        new(ContextualTipCategoryId.Development, "ontwikkeling", "Tips.Cat.Development.Title", "Tips.Cat.Development.Lead", "Tips.Art.Books", 6),
        new(ContextualTipCategoryId.WorkBenefits, "werk-en-uitkering", "Tips.Cat.WorkBenefits.Title", "Tips.Cat.WorkBenefits.Lead", "Tips.Art.WorkBenefits", 6)
    ];

    public static readonly IReadOnlyList<ContextualTipDefinition> Tips = BuildTips();

    public static ContextualTipDefinition? FindById(string id)
        => Tips.FirstOrDefault(t => string.Equals(t.Id, id, StringComparison.OrdinalIgnoreCase));

    public static ContextualTipDefinition? FindBySlug(string slug)
        => Tips.FirstOrDefault(t => string.Equals(t.Slug, slug, StringComparison.OrdinalIgnoreCase));

    public static ContextualTipCategory? FindCategory(ContextualTipCategoryId id)
        => Categories.FirstOrDefault(c => c.Id == id);

    public static ContextualTipCategory? FindCategoryBySlug(string slug)
        => Categories.FirstOrDefault(c => string.Equals(c.Slug, slug, StringComparison.OrdinalIgnoreCase));

    public static IReadOnlyList<ContextualTipDefinition> ForCategory(ContextualTipCategoryId category)
        => Tips.Where(t => t.Category == category).ToList();

    public static ContextualTipDefinition? ForPlacement(ContextualTipPlacement placement)
        => Tips.FirstOrDefault(t => t.Placement == placement);

    public static IReadOnlyList<ContextualTipDefinition> Search(string? query)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            return Tips;
        }

        var q = query.Trim();
        return Tips.Where(t =>
                t.Id.Contains(q, StringComparison.OrdinalIgnoreCase)
                || t.Slug.Contains(q, StringComparison.OrdinalIgnoreCase)
                || t.TitleKey.Contains(q, StringComparison.OrdinalIgnoreCase))
            .ToList();
    }

    private static IReadOnlyList<ContextualTipDefinition> BuildTips()
    {
        var list = new List<ContextualTipDefinition>();

        list.Add(new(
            "interview-prep-scheduled",
            ContextualTipCategoryId.Interview,
            "gesprek-voorbereiden",
            "Tips.Placement.InterviewPrep.Title",
            "Tips.Placement.InterviewPrep.Summary",
            [],
            ContextualTipPlacement.InterviewScheduled));

        list.Add(new(
            "offer-contract-checklist",
            ContextualTipCategoryId.Contract,
            "aanbod-waar-let-je-op",
            "Tips.Placement.Offer.Title",
            "Tips.Placement.Offer.Summary",
            [],
            ContextualTipPlacement.OfferReceived));

        list.Add(new(
            "passport-education-course",
            ContextualTipCategoryId.PassportCv,
            "ook-een-cursus-telt",
            "Tips.Placement.PassportEducation.Title",
            "Tips.Placement.PassportEducation.Summary",
            [],
            ContextualTipPlacement.PassportEducationEmpty));

        list.Add(new(
            "career-dev-conversation",
            ContextualTipCategoryId.Development,
            "vraag-ontwikkelgesprek",
            "Tips.Placement.CareerDev.Title",
            "Tips.Placement.CareerDev.Summary",
            [],
            ContextualTipPlacement.CareerRoute));

        list.Add(new(
            "first-interview",
            ContextualTipCategoryId.Interview,
            "je-eerste-gesprek",
            "Tips.Detail.FirstInterview.Title",
            "Tips.Detail.FirstInterview.Summary",
            [
                new("Tips.Detail.FirstInterview.Block1.Title", "Tips.Detail.FirstInterview.Block1.Body"),
                new("Tips.Detail.FirstInterview.Block2.Title", "Tips.Detail.FirstInterview.Block2.Body"),
                new("Tips.Detail.FirstInterview.Block3.Title", "Tips.Detail.FirstInterview.Block3.Body")
            ],
            ContextualTipPlacement.HubNowForYou));

        foreach (var (id, cat, slug, title, summary) in HubTips())
        {
            if (list.Any(t => t.Id == id))
            {
                continue;
            }

            list.Add(new(id, cat, slug, title, summary, []));
        }

        return list;
    }

    private static IEnumerable<(string Id, ContextualTipCategoryId Cat, string Slug, string Title, string Summary)> HubTips()
    {
        yield return ("interview-questions-ask", ContextualTipCategoryId.Interview, "vragen-stellen", "Tips.Hub.InterviewAsk.Title", "Tips.Hub.InterviewAsk.Summary");
        yield return ("interview-outfit", ContextualTipCategoryId.Interview, "wat-trek-je-aan", "Tips.Hub.InterviewOutfit.Title", "Tips.Hub.InterviewOutfit.Summary");
        yield return ("interview-calm", ContextualTipCategoryId.Interview, "zenuwachtig-rustig", "Tips.Hub.InterviewCalm.Title", "Tips.Hub.InterviewCalm.Summary");
        yield return ("interview-after", ContextualTipCategoryId.Interview, "na-het-gesprek", "Tips.Hub.InterviewAfter.Title", "Tips.Hub.InterviewAfter.Summary");
        yield return ("motivation-why-job", ContextualTipCategoryId.Motivation, "waarom-deze-baan", "Tips.Hub.MotivationWhy.Title", "Tips.Hub.MotivationWhy.Summary");
        yield return ("passport-no-diploma", ContextualTipCategoryId.PassportCv, "zonder-diploma", "Tips.Hub.PassportNoDiploma.Title", "Tips.Hub.PassportNoDiploma.Summary");
        yield return ("contract-proeftijd", ContextualTipCategoryId.Contract, "proeftijd", "Tips.Hub.ContractProbation.Title", "Tips.Hub.ContractProbation.Summary");
        yield return ("salary-minimum", ContextualTipCategoryId.Salary, "minimumloon", "Tips.Hub.SalaryMinimum.Title", "Tips.Hub.SalaryMinimum.Summary");
        yield return ("development-course-employer", ContextualTipCategoryId.Development, "cursus-werkgever", "Tips.Hub.DevelopmentCourse.Title", "Tips.Hub.DevelopmentCourse.Summary");
        yield return ("work-benefits-change", ContextualTipCategoryId.WorkBenefits, "uitkering-werk", "Tips.Hub.WorkBenefitsChange.Title", "Tips.Hub.WorkBenefitsChange.Summary");
    }
}
