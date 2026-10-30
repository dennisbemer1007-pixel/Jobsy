namespace Jobsy.Core.Scholen.PupilPassport;

public static class PupilPassportWebMapper
{
    private static readonly string[] RoutePhaseKeys =
    [
        "LeerlingPassport.Route.Phase.Now",
        "LeerlingPassport.Route.Phase.Next",
        "LeerlingPassport.Route.Phase.Later",
        "LeerlingPassport.Route.Phase.Goal"
    ];

    public static PupilG78PassportWebDto ToGroep78Web(PupilPassportPdfModel m, bool enabled)
    {
        if (m.Variant != PupilPassportVariant.Groep78)
        {
            return new PupilG78PassportWebDto(Enabled: false);
        }

        var dateCulture = PupilVerhaalLocalized.IsEnglish(m.Language)
            ? System.Globalization.CultureInfo.GetCultureInfo("en-GB")
            : System.Globalization.CultureInfo.GetCultureInfo("nl-NL");
        var worlds = m.Worlds.Select(w => new PupilG78PassportWorldDto(
            PlaceName: w.Title,
            SectionLabel: w.Subtitle,
            Headline: w.Value,
            ImageFile: WorldImageFile(w.WorldNumber))).ToList();

        var steps = m.RouteSteps.Select((step, i) =>
        {
            var phaseKey = step.IsGoal || i >= RoutePhaseKeys.Length - 1
                ? RoutePhaseKeys[^1]
                : RoutePhaseKeys[Math.Min(i, RoutePhaseKeys.Length - 2)];
            var title = PupilPassportLocalizedCopy.RouteStepTitle(SplitTitle(step.Title), m.Language);
            return new PupilG78PassportRouteStepDto(
                PupilPassportLocalizedCopy.Get(phaseKey, m.Language),
                title,
                step.Detail,
                step.IsGoal);
        }).ToList();

        var cards = m.OccupationCards.Select(c => new PupilG78PassportCardDto(
            c.DomainLabel,
            c.Title,
            c.Blurb,
            c.FitTags)).ToList();

        var strengths = m.Strengths.Select(s => new PupilG78PassportStrengthDto(
            s.Title,
            s.Subtitle,
            s.IconKey)).ToList();

        var subjectGroups = (m.SubjectGroups ?? [])
            .Select(g => new PupilG78PassportSubjectGroupDto(
                g.Title,
                g.Intro,
                g.Subjects.Select(s => s.Label).ToList(),
                g.TraitLines,
                g.ConversationQuestions))
            .ToList();

        return new PupilG78PassportWebDto(
            Enabled: enabled,
            SchoolName: m.SchoolName,
            ClassLabel: m.ClassLabel,
            DisplayCode: m.DisplayCode,
            DateLabel: m.Date.ToString("d MMM yyyy", dateCulture),
            DisplayName: m.DisplayNameOnPdf,
            StoryBody: m.StoryBody,
            Worlds: worlds,
            Likes: m.Likes,
            Dislikes: m.Dislikes,
            LikeOwnWord: m.LikeOwnWord,
            DislikeOwnWord: m.DislikeOwnWord,
            Strengths: strengths,
            DreamJobTitle: string.IsNullOrWhiteSpace(m.DreamJobTitle) ? null : m.DreamJobTitle,
            DreamEncouragement: DreamEncouragement(m),
            RouteSteps: steps,
            DreamHaveItems: m.DreamHaveItems,
            DreamLearnItems: m.DreamLearnItems,
            AltRouteNote: m.AltRouteNote,
            OccupationCards: cards,
            DiscoveryQuestions: m.DiscoveryQuestions,
            HomeClassTips: m.HomeClassTips,
            FooterPrivacy: m.FooterPrivacy,
            MirrorBody: m.MirrorBody,
            SubjectGroups: subjectGroups.Count == 0 ? null : subjectGroups);
    }

    private static string? DreamEncouragement(PupilPassportPdfModel m)
    {
        if (string.IsNullOrWhiteSpace(m.DreamJobTitle))
        {
            return null;
        }

        var title = m.DreamJobTitle.TrimEnd('!', '.');
        return string.Format(
            PupilVerhaalLocalized.IsEnglish(m.Language)
                ? System.Globalization.CultureInfo.GetCultureInfo("en-GB")
                : System.Globalization.CultureInfo.GetCultureInfo("nl-NL"),
            PupilPassportLocalizedCopy.Get("LeerlingPassport.Dream.Bubble", m.Language),
            title);
    }

    private static string SplitTitle(string title)
    {
        var part = title.Split('·', 2, StringSplitOptions.TrimEntries)[0];
        var colon = part.IndexOf(':');
        if (colon >= 0 && colon < part.Length - 1)
        {
            part = part[(colon + 1)..].Trim();
        }

        return part.TrimEnd('!', '.');
    }

    private static string WorldImageFile(int worldNumber) => worldNumber switch
    {
        1 => "g78-rif.jpg",
        2 => "g78-grot.jpg",
        3 => "g78-vuurtoren.jpg",
        4 => "g78-lagune.jpg",
        _ => "g78-rif.jpg"
    };
}

public sealed record PupilG78PassportWorldDto(
    string PlaceName,
    string SectionLabel,
    string Headline,
    string ImageFile);

public sealed record PupilG78PassportRouteStepDto(
    string PhaseLabel,
    string Title,
    string Detail,
    bool IsGoal);

public sealed record PupilG78PassportCardDto(
    string DomainLabel,
    string Title,
    string Blurb,
    IReadOnlyList<string> FitTags);

public sealed record PupilG78PassportStrengthDto(string Title, string Subtitle, string IconKey);

public sealed record PupilG78PassportWebDto(
    bool Enabled = false,
    string SchoolName = "",
    string ClassLabel = "",
    string DisplayCode = "",
    string DateLabel = "",
    string? DisplayName = null,
    string StoryBody = "",
    IReadOnlyList<PupilG78PassportWorldDto>? Worlds = null,
    IReadOnlyList<string>? Likes = null,
    IReadOnlyList<string>? Dislikes = null,
    string? LikeOwnWord = null,
    string? DislikeOwnWord = null,
    IReadOnlyList<PupilG78PassportStrengthDto>? Strengths = null,
    string? DreamJobTitle = null,
    string? DreamEncouragement = null,
    IReadOnlyList<PupilG78PassportRouteStepDto>? RouteSteps = null,
    IReadOnlyList<string>? DreamHaveItems = null,
    IReadOnlyList<string>? DreamLearnItems = null,
    string? AltRouteNote = null,
    IReadOnlyList<PupilG78PassportCardDto>? OccupationCards = null,
    IReadOnlyList<string>? DiscoveryQuestions = null,
    IReadOnlyList<string>? HomeClassTips = null,
    string FooterPrivacy = "",
    string? MirrorBody = null,
    IReadOnlyList<PupilG78PassportSubjectGroupDto>? SubjectGroups = null);

public sealed record PupilG78PassportSubjectGroupDto(
    string Title,
    string Intro,
    IReadOnlyList<string> SubjectLabels,
    IReadOnlyList<string> TraitLines,
    IReadOnlyList<string> ConversationQuestions);
