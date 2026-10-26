using Jobsy.Core.Enums;
using Jobsy.Core.Rules;
using Jobsy.Web.Components.Candidate.Dna;
using Jobsy.Web.Localization;
using Jobsy.Web.Models;
using Jobsy.Web.Navigation;
using Jobsy.Web.Onboarding;
using Jobsy.Web.Services;

namespace Jobsy.Web.Passport;

internal static class PassportSsrDisplay
{
    internal sealed record CardModel(
        string DisplayName,
        string Initials,
        string MemberNumber,
        bool ShowOpenForWork,
        int CompletenessPercent,
        int FilledLayers,
        int LayerTotal,
        string? AboutMe,
        string? City,
        string? Travel,
        string? Hours,
        string? AvailableLabel,
        string? LanguagesLabel,
        IReadOnlyList<string> LookingFor,
        string? LicenseLabel,
        string? OneLiner,
        IReadOnlyList<string> Keywords);

    internal sealed record StatField(string? Value, string? Status);

    internal sealed record OverviewModel(
        double CompetenceArc,
        double CareerArc,
        double CultureArc,
        double ValuesArc,
        int LobsterStage,
        StatField Strongest,
        StatField WorkFit,
        StatField CultureHome,
        StatField Important);

    internal sealed record DnaPanelModel(
        CandidateDnaViewBuilder.DnaViewModel? View,
        string? Story,
        DateTime? StoryGeneratedAt,
        IReadOnlyList<string> Hobbies,
        int CompletedTests,
        bool ShowEncourageBanner);

    internal static CardModel BuildCard(
        CultureState culture,
        MeProfile? profile,
        CandidateKompasState? kompas,
        CandidateDnaSummary? dna,
        bool employersEnabled)
    {
        var completeness = CandidateDnaViewBuilder.EffectiveCompletenessPercent(kompas, dna);
        var filledLayers = KompasProfileCompleteness.FilledSteps(completeness);
        var prefs = profile?.Preferences;
        var displayName = string.IsNullOrWhiteSpace(profile?.FullName)
            ? culture["Passport.Title"]
            : profile!.FullName.Trim();
        var transportLabel = string.Join(
            " + ",
            TransportLabels.SplitMany(prefs?.PreferredTransport).Select(mode => UiLabels.Transport(culture, mode)));
        var travel = prefs?.MaxTravelMinutes is int m && transportLabel.Length > 0
            ? $"{transportLabel} {m} min"
            : null;
        string? hours = null;
        if (prefs?.MinHoursPerWeek is decimal || prefs?.MaxHoursPerWeek is decimal)
        {
            var min = prefs!.MinHoursPerWeek;
            var max = prefs.MaxHoursPerWeek;
            hours = min is not null && max is not null
                ? $"{min:0}-{max:0} u"
                : $"{(max ?? min):0} u";
        }

        var keywords = new List<string>();
        var who = kompas?.WhoAmI ?? dna?.WhoAmI;
        if (who?.Keywords is { Count: > 0 })
        {
            keywords.AddRange(who.Keywords.Where(k => !string.IsNullOrWhiteSpace(k)).Take(6));
        }

        var oneLiner = string.Join(" · ", new[]
        {
            HomeAddressCity.From(prefs?.HomeAddress),
            travel,
            hours,
        }.Where(s => !string.IsNullOrWhiteSpace(s)));

        var lookingFor = new List<string>();
        if (prefs?.Roles is { Count: > 0 })
        {
            lookingFor.AddRange(prefs.Roles
                .Where(r => !string.IsNullOrWhiteSpace(r))
                .Select(r => UiLabels.WorkType(culture, r.Trim())));
        }

        return new CardModel(
            displayName,
            InitialsFrom(profile),
            PassportMemberNumber.Format(profile?.Id ?? Guid.Empty),
            employersEnabled && (profile?.OpenForWork ?? false),
            completeness,
            filledLayers,
            KompasProfileCompleteness.StepCount,
            string.IsNullOrWhiteSpace(prefs?.AboutMe) ? null : prefs!.AboutMe.Trim(),
            HomeAddressCity.From(prefs?.HomeAddress),
            travel,
            hours,
            FormatAvailability(culture, profile, prefs),
            FormatLanguages(prefs),
            lookingFor,
            DrivingLicenseLabels.Combine(prefs?.DrivingLicenses),
            string.IsNullOrWhiteSpace(oneLiner) ? null : oneLiner,
            keywords);
    }

    internal static OverviewModel BuildOverview(CultureState culture, CandidateKompasState? kompas, CandidateDnaSummary? dna)
    {
        CandidateDnaViewBuilder.TextLookup t = k => culture[k];
        CandidateDnaViewBuilder.TextFormat format = (k, args) => culture.Format(k, args);
        var view = kompas is not null
            ? CandidateDnaViewBuilder.FromKompas(kompas, t, format, culture.Language)
            : dna is not null
                ? CandidateDnaViewBuilder.FromDnaSummary(dna, t, format, culture.Language)
                : null;

        if (view is null)
        {
            return new OverviewModel(
                0, 0, 0, 0,
                PassportShellRules.LobsterStage(0, anyTestStarted: false),
                new StatField(null, null),
                new StatField(null, null),
                new StatField(null, null),
                new StatField(null, null));
        }

        static double Arc(CandidateDnaViewBuilder.DnaViewModel v, AssessmentKind kind)
        {
            var card = v.Cards.FirstOrDefault(c => c.Kind == kind);
            return card is null ? 0 : CandidateDnaViewBuilder.ArcProgress(card);
        }

        static StatField Stat(CultureState c, CandidateDnaViewBuilder.DnaViewModel v, AssessmentKind kind)
        {
            var card = v.Cards.FirstOrDefault(x => x.Kind == kind);
            if (card is null || string.IsNullOrWhiteSpace(card.TopLabel))
            {
                return new StatField(null, null);
            }

            string? status = kind == AssessmentKind.Career && card.Summary.Contains('·', StringComparison.Ordinal)
                ? card.Summary
                : StatusLabel(c, card);
            return new StatField(card.TopLabel, status);
        }

        var completed = view.CompletedCount;
        var anyStarted = view.Cards.Any(c => c.Answered > 0 || c.Completed);
        return new OverviewModel(
            Arc(view, AssessmentKind.Competence),
            Arc(view, AssessmentKind.Career),
            Arc(view, AssessmentKind.Culture),
            Arc(view, AssessmentKind.Values),
            PassportShellRules.LobsterStage(completed, anyStarted),
            Stat(culture, view, AssessmentKind.Competence),
            Stat(culture, view, AssessmentKind.Career),
            Stat(culture, view, AssessmentKind.Culture),
            Stat(culture, view, AssessmentKind.Values));
    }

    internal static DnaPanelModel BuildDnaPanel(
        CultureState culture,
        MeProfile? profile,
        CandidateKompasState? kompas,
        CandidateDnaSummary? dna)
    {
        CandidateDnaViewBuilder.TextLookup t = k => culture[k];
        CandidateDnaViewBuilder.TextFormat format = (k, args) => culture.Format(k, args);
        var view = kompas is not null
            ? CandidateDnaViewBuilder.FromKompas(kompas, t, format, culture.Language)
            : dna is not null
                ? CandidateDnaViewBuilder.FromDnaSummary(dna, t, format, culture.Language)
                : null;

        var who = kompas?.WhoAmI ?? dna?.WhoAmI;
        var story = string.IsNullOrWhiteSpace(who?.Story) ? null : who!.Story.Trim();
        var storyAt = who?.GeneratedAtUtc;

        var hobbies = new List<string>();
        foreach (var hobby in profile?.Preferences?.Hobbies ?? [])
        {
            if (string.IsNullOrWhiteSpace(hobby))
            {
                continue;
            }

            hobbies.Add(DiscoveryCatalogs.IsKnownHobby(hobby)
                ? culture[DiscoveryCatalogs.HobbyLabelKey(hobby)]
                : hobby.Trim());
        }

        var completed = view?.CompletedCount ?? 0;
        var completeness = CandidateDnaViewBuilder.EffectiveCompletenessPercent(kompas, dna);
        var filled = KompasProfileCompleteness.FilledSteps(completeness);
        var showEncourage = filled < KompasProfileCompleteness.StepCount;

        return new DnaPanelModel(view, story, storyAt, hobbies, completed, showEncourage);
    }

    internal static string StatusLabel(CultureState culture, CandidateDnaViewBuilder.DnaCard card)
    {
        if (card.Extended)
        {
            return culture["Test.Status.Extended"];
        }

        if (card.Completed)
        {
            return culture["Dna.StatusComplete"];
        }

        if (card.IsProvisional)
        {
            return culture["Onboarding.ProvisionalBadge"];
        }

        return culture["Test.Status.NotDone"];
    }

    internal static string InitialsFrom(MeProfile? profile)
    {
        var first = profile?.FirstName?.Trim();
        var last = profile?.LastName?.Trim();
        if (!string.IsNullOrEmpty(first) || !string.IsNullOrEmpty(last))
        {
            var a = string.IsNullOrEmpty(first) ? "" : first[..1];
            var b = string.IsNullOrEmpty(last) ? "" : last[..1];
            return (a + b).ToUpperInvariant();
        }

        var name = profile?.FullName?.Trim();
        if (string.IsNullOrEmpty(name))
        {
            return "?";
        }

        var parts = name.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length == 1)
        {
            return parts[0][..1].ToUpperInvariant();
        }

        return (parts[0][..1] + parts[^1][..1]).ToUpperInvariant();
    }

    private static string? FormatAvailability(CultureState culture, MeProfile? profile, CandidatePreferences? prefs)
    {
        if (prefs is null)
        {
            return null;
        }

        if (profile?.AvailableFromDate is DateOnly fromDate)
        {
            return fromDate.ToString("dd-MM-yyyy");
        }

        if (prefs.AvailabilityPresets is { Count: > 0 })
        {
            return string.Join(", ", prefs.AvailabilityPresets.Take(2));
        }

        return culture["Profile.PresetImmediate"];
    }

    private static string? FormatLanguages(CandidatePreferences? prefs)
    {
        if (prefs is null)
        {
            return null;
        }

        var names = new List<string>();
        if (!string.IsNullOrWhiteSpace(prefs.DutchLevel)
            && DiscoveryCatalogs.IsKnownDutchLevel(prefs.DutchLevel))
        {
            names.Add(LanguageDisplayName("nl"));
        }

        foreach (var lang in prefs.SpokenLanguages ?? [])
        {
            if (string.IsNullOrWhiteSpace(lang.Code)
                || string.Equals(lang.Code, "nl", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            names.Add(LanguageDisplayName(lang.Code));
        }

        return names.Count == 0 ? null : string.Join(", ", names.Distinct(StringComparer.OrdinalIgnoreCase).Take(5));
    }

    private static string LanguageDisplayName(string code)
    {
        try
        {
            var info = System.Globalization.CultureInfo.GetCultureInfo(code);
            var name = info.NativeName;
            if (string.IsNullOrWhiteSpace(name))
            {
                name = info.EnglishName;
            }

            return string.IsNullOrWhiteSpace(name)
                ? code.ToUpperInvariant()
                : char.ToUpperInvariant(name[0]) + name[1..];
        }
        catch (System.Globalization.CultureNotFoundException)
        {
            return code.ToUpperInvariant();
        }
    }
}
