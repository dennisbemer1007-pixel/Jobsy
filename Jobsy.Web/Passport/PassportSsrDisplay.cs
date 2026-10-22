using Jobsy.Core.Enums;
using Jobsy.Core.Rules;
using Jobsy.Web.Components.Candidate.Dna;
using Jobsy.Web.Localization;
using Jobsy.Web.Models;
using Jobsy.Web.Navigation;
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
        string? AboutMe,
        string? City,
        string? Travel,
        string? Hours,
        string? OneLiner,
        IReadOnlyList<string> Keywords);

    internal sealed record OverviewModel(
        double CompetenceArc,
        double CareerArc,
        double CultureArc,
        double ValuesArc,
        int CompletedTests,
        string? Strongest,
        string? WorkFit,
        string? CultureHome,
        string? Important);

    internal static CardModel BuildCard(
        CultureState culture,
        MeProfile? profile,
        CandidateKompasState? kompas,
        CandidateDnaSummary? dna,
        bool employersEnabled)
    {
        var completeness = CandidateDnaViewBuilder.EffectiveCompletenessPercent(kompas, dna);
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

        return new CardModel(
            displayName,
            InitialsFrom(profile),
            PassportMemberNumber.Format(profile?.Id ?? Guid.Empty),
            employersEnabled && (profile?.OpenForWork ?? false),
            completeness,
            string.IsNullOrWhiteSpace(prefs?.AboutMe) ? null : prefs!.AboutMe.Trim(),
            HomeAddressCity.From(prefs?.HomeAddress),
            travel,
            hours,
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
            return new OverviewModel(0, 0, 0, 0, 0, null, null, null, null);
        }

        static double Arc(CandidateDnaViewBuilder.DnaViewModel v, AssessmentKind kind)
        {
            var card = v.Cards.FirstOrDefault(c => c.Kind == kind);
            return card is null ? 0 : CandidateDnaViewBuilder.ArcProgress(card);
        }

        string? Top(AssessmentKind kind)
            => view.Cards.FirstOrDefault(c => c.Kind == kind)?.TopLabel;

        return new OverviewModel(
            Arc(view, AssessmentKind.Competence),
            Arc(view, AssessmentKind.Career),
            Arc(view, AssessmentKind.Culture),
            Arc(view, AssessmentKind.Values),
            view.CompletedCount,
            Top(AssessmentKind.Competence),
            Top(AssessmentKind.Career),
            Top(AssessmentKind.Culture),
            Top(AssessmentKind.Values));
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
}
