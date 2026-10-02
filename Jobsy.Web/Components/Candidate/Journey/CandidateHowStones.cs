using Jobsy.Web.Navigation;

namespace Jobsy.Web.Components.Candidate.Journey;

/// <summary>Which of Dennis' five stones a row on <c>/candidate/hoe-werkt-lobsy</c> represents (05 §2, D15).</summary>
public enum CandidateHowStoneKind
{
    Discovery,
    Passport,
    Career,
    JobMap,
    Applications
}

/// <summary>Link wording family: a stone you work through, or one you simply open.</summary>
public enum CandidateHowStoneAction
{
    /// <summary>"Kijk terug" / "Ga verder" / "Begin".</summary>
    Progress,

    /// <summary>Always "Open".</summary>
    Open
}

/// <summary>One row of the how-it-works list. Localization keys only; the page resolves copy.</summary>
public sealed record CandidateHowStone(
    CandidateHowStoneKind Kind,
    string TitleKey,
    string BodyKey,
    string ShortKey,
    string Href,
    CandidateHowStoneAction Action);

/// <summary>
/// The stone set and order for <c>/candidate/hoe-werkt-lobsy</c>. Pure and nav-independent:
/// the order is fixed here (D15) and is never derived from the role navigation.
/// </summary>
public static class CandidateHowStones
{
    public const string DiscoveryHref = OnboardingRoutes.DiscoveryPath;
    public const string OnboardingHref = OnboardingRoutes.ClassicStartPath;
    public const string PassportHref = "/candidate/paspoort";
    public const string ProfileHref = "/candidate/profile";
    public const string CareerHref = "/carriere";
    public const string JobMapHref = "/banenkaart";
    public const string ApplicationsHref = "/candidate/applications";

    /// <summary>The candidate routes that exist today (<c>docs/ROUTES.md</c>).</summary>
    public static readonly IReadOnlySet<string> DefaultRoutes =
        new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            DiscoveryHref,
            OnboardingHref,
            PassportHref,
            ProfileHref,
            CareerHref,
            JobMapHref,
            ApplicationsHref
        };

    /// <summary>
    /// Dennis' order: ontdekkingsreis · paspoort · carrière · banenkaart · sollicitaties.
    /// Werkgevers OFF drops the last two; the paspoort flag swaps stone 2 for "Mijn profiel";
    /// a missing route drops its stone (reported through <paramref name="onDropped"/>) instead
    /// of linking to a 404.
    /// </summary>
    public static IReadOnlyList<CandidateHowStone> Build(
        bool employersOn,
        bool passportOn,
        IReadOnlySet<string>? knownRoutes = null,
        Action<string>? onDropped = null)
    {
        var routes = knownRoutes ?? DefaultRoutes;
        var stones = new List<CandidateHowStone>(5);

        Add(Discovery(passportOn, routes));
        Add(Passport(passportOn, routes));
        Add(new CandidateHowStone(
            CandidateHowStoneKind.Career,
            "HowC.Stone.Career.Title",
            "HowC.Stone.Career.Body",
            "HowC.Short.Career",
            CareerHref,
            CandidateHowStoneAction.Progress));

        if (employersOn)
        {
            Add(new CandidateHowStone(
                CandidateHowStoneKind.JobMap,
                "HowC.Stone.JobMap.Title",
                "HowC.Stone.JobMap.Body",
                "HowC.Short.JobMap",
                JobMapHref,
                CandidateHowStoneAction.Open));
            Add(new CandidateHowStone(
                CandidateHowStoneKind.Applications,
                "HowC.Stone.Applications.Title",
                "HowC.Stone.Applications.Body",
                "HowC.Short.Applications",
                ApplicationsHref,
                CandidateHowStoneAction.Open));
        }

        return stones;

        void Add(CandidateHowStone? stone)
        {
            if (stone is null)
            {
                return;
            }

            if (!routes.Contains(stone.Href))
            {
                onDropped?.Invoke(stone.Href);
                return;
            }

            stones.Add(stone);
        }
    }

    /// <summary>
    /// Stone 1 is the ontdekkingsreis when that route exists and the paspoort flag is on;
    /// otherwise it becomes "Je profiel invullen" on the onboarding route.
    /// </summary>
    private static CandidateHowStone? Discovery(bool passportOn, IReadOnlySet<string> routes)
    {
        if (passportOn && routes.Contains(DiscoveryHref))
        {
            return new CandidateHowStone(
                CandidateHowStoneKind.Discovery,
                "HowC.Stone.Discovery.Title",
                "HowC.Stone.Discovery.Body",
                "HowC.Short.Discovery",
                DiscoveryHref,
                CandidateHowStoneAction.Progress);
        }

        var href = routes.Contains(OnboardingHref) ? OnboardingHref : ProfileHref;
        return new CandidateHowStone(
            CandidateHowStoneKind.Discovery,
            "HowC.Stone.Profile.Title",
            "HowC.Stone.Profile.Body",
            "HowC.Short.Discovery",
            href,
            CandidateHowStoneAction.Progress);
    }

    private static CandidateHowStone Passport(bool passportOn, IReadOnlySet<string> routes)
        => passportOn && routes.Contains(PassportHref)
            ? new CandidateHowStone(
                CandidateHowStoneKind.Passport,
                "HowC.Stone.Passport.Title",
                "HowC.Stone.Passport.Body",
                "HowC.Short.Passport",
                PassportHref,
                CandidateHowStoneAction.Open)
            : new CandidateHowStone(
                CandidateHowStoneKind.Passport,
                "HowC.Stone.MyProfile.Title",
                "HowC.Stone.MyProfile.Body",
                "HowC.Short.Passport",
                ProfileHref,
                CandidateHowStoneAction.Open);
}
