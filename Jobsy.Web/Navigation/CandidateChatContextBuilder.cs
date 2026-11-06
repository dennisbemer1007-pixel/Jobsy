using System.Globalization;
using Jobsy.Web.Localization;
using Jobsy.Web.Models;
using Jobsy.Web.Services;
using Microsoft.AspNetCore.Components;

namespace Jobsy.Web.Navigation;

public sealed record ChatSuggestionChip(string Label, string Message);

public sealed record CandidateChatBootstrap(
    string? PageContextLine,
    string Greeting,
    IReadOnlyList<ChatSuggestionChip> Chips);

/// <summary>
/// Page-aware greeting and chips for candidates — only facts from API data on the current route.
/// </summary>
public static class CandidateChatContextBuilder
{
    public static Task<CandidateChatBootstrap> BuildAsync(
        JobsyApiClient api,
        NavigationManager navigation,
        CultureState culture,
        CancellationToken ct = default)
        => BuildForPagePathAsync(api, RoutePath(navigation.Uri), culture, ct);

    public static async Task<CandidateChatBootstrap> BuildForPagePathAsync(
        JobsyApiClient api,
        string path,
        CultureState culture,
        CancellationToken ct = default)
    {
        path = NormalizePagePath(path);
        MeProfile? profile = null;
        IReadOnlyList<ApplicationItem> apps = [];

        try
        {
            profile = await api.GetMyProfileAsync(ct);
        }
        catch
        {
            // Neutral copy when profile is unavailable.
        }

        try
        {
            apps = await api.GetMyApplicationsAsync(ct);
        }
        catch
        {
            // Route-only context still works.
        }

        var firstName = FirstName(profile);
        var pageLabel = PageLabel(path, culture);
        var pageContext = pageLabel is null
            ? null
            : Format(culture, "Assistant.Context.OnPage", pageLabel);

        var upcoming = FindUpcomingInterview(apps);
        VacancyListItem? vacancy = null;
        if (TryVacancyId(path, out var vacancyId))
        {
            try
            {
                vacancy = await api.GetVacancyAsync(vacancyId, ct: ct);
            }
            catch
            {
                // Ignore — neutral vacancy copy.
            }
        }

        var greeting = Greeting(firstName, path, upcoming, vacancy, culture, culture.Language);
        var chips = Chips(path, upcoming, vacancy, culture);

        return new CandidateChatBootstrap(pageContext, greeting, chips);
    }

    private static string Greeting(
        string? firstName,
        string path,
        UpcomingInterview? upcoming,
        VacancyListItem? vacancy,
        CultureState culture,
        string language)
    {
        var hi = string.IsNullOrWhiteSpace(firstName)
            ? culture["Assistant.GreetingNeutral"]
            : string.Format(culture["Assistant.GreetingNamed"], firstName.Trim());

        if (upcoming is not null && path.StartsWith("/candidate/applications", StringComparison.OrdinalIgnoreCase))
        {
            var when = FormatInterviewWhen(upcoming.AtUtc, language);
            return string.Format(
                culture["Assistant.ContextGreeting.Interview"],
                hi,
                when,
                upcoming.CompanyName);
        }

        if (vacancy is not null && path.StartsWith("/vacancies/", StringComparison.OrdinalIgnoreCase))
        {
            var title = vacancy.Title?.Trim();
            if (!string.IsNullOrWhiteSpace(title))
            {
                return string.Format(culture["Assistant.ContextGreeting.Vacancy"], hi, title);
            }
        }

        return hi + " " + culture["Assistant.ContextGreeting.FallbackTail"];
    }

    private static IReadOnlyList<ChatSuggestionChip> Chips(
        string path,
        UpcomingInterview? upcoming,
        VacancyListItem? vacancy,
        CultureState culture)
    {
        var list = new List<ChatSuggestionChip>(3);

        if (upcoming is not null && path.StartsWith("/candidate/applications", StringComparison.OrdinalIgnoreCase))
        {
            list.Add(new ChatSuggestionChip(
                culture["Assistant.Chip.PrepareInterview"],
                culture["Assistant.Chip.PrepareInterviewMessage"]));
        }

        if (path.StartsWith("/candidate/applications", StringComparison.OrdinalIgnoreCase)
            || path.StartsWith("/vacancies/", StringComparison.OrdinalIgnoreCase))
        {
            list.Add(new ChatSuggestionChip(
                culture["Assistant.Chip.SeenMeaning"],
                culture["Assistant.Chip.SeenMeaningMessage"]));
        }

        if (path.StartsWith("/vacancies/", StringComparison.OrdinalIgnoreCase) && vacancy is not null)
        {
            list.Add(new ChatSuggestionChip(
                culture["Assistant.Chip.ApplyHelp"],
                culture["Assistant.Chip.ApplyHelpMessage"]));
        }
        else if (path.StartsWith("/candidate/paspoort", StringComparison.OrdinalIgnoreCase)
                 || path.Equals("/banenkaart", StringComparison.OrdinalIgnoreCase)
                 || path.Equals("/banen", StringComparison.OrdinalIgnoreCase))
        {
            list.Add(new ChatSuggestionChip(
                culture["Assistant.Chip.JobMapHelp"],
                culture["Assistant.Chip.JobMapHelpMessage"]));
        }
        else if (path.Equals("/carriere", StringComparison.OrdinalIgnoreCase)
                 || path.StartsWith("/carriere/", StringComparison.OrdinalIgnoreCase))
        {
            list.Add(new ChatSuggestionChip(
                culture["Assistant.Chip.CareerRoute"],
                culture["Assistant.Chip.CareerRouteMessage"]));
        }
        else if (path.StartsWith("/candidate/kompas", StringComparison.OrdinalIgnoreCase)
                 || path.StartsWith("/candidate/start/kompas", StringComparison.OrdinalIgnoreCase))
        {
            list.Add(new ChatSuggestionChip(
                culture["Assistant.Chip.KompasExplain"],
                culture["Assistant.Chip.KompasExplainMessage"]));
        }
        else
        {
            list.Add(new ChatSuggestionChip(
                culture["Assistant.Chip.Initiative"],
                culture["Assistant.Chip.InitiativeMessage"]));
        }

        return list.Take(3).ToList();
    }

    private static string? PageLabel(string path, CultureState culture)
    {
        if (path.StartsWith("/candidate/applications", StringComparison.OrdinalIgnoreCase))
        {
            return culture["Assistant.Page.Applications"];
        }

        if (path.StartsWith("/vacancies/", StringComparison.OrdinalIgnoreCase))
        {
            return culture["Assistant.Page.Vacancy"];
        }

        if (path.Equals("/banenkaart", StringComparison.OrdinalIgnoreCase))
        {
            return culture["Assistant.Page.JobMap"];
        }

        if (path.StartsWith("/candidate/paspoort", StringComparison.OrdinalIgnoreCase))
        {
            return culture["Assistant.Page.Passport"];
        }

        if (path.StartsWith("/candidate/vacancies", StringComparison.OrdinalIgnoreCase))
        {
            return culture["Assistant.Page.MyVacancies"];
        }

        if (path.Equals("/carriere", StringComparison.OrdinalIgnoreCase)
            || path.StartsWith("/carriere/", StringComparison.OrdinalIgnoreCase))
        {
            return culture["Assistant.Page.Career"];
        }

        if (path.StartsWith("/candidate/kompas", StringComparison.OrdinalIgnoreCase)
            || path.StartsWith("/candidate/start/kompas", StringComparison.OrdinalIgnoreCase))
        {
            return culture["Assistant.Page.Kompas"];
        }

        return null;
    }

    private static UpcomingInterview? FindUpcomingInterview(IReadOnlyList<ApplicationItem> apps)
    {
        var now = DateTime.UtcNow;
        UpcomingInterview? best = null;
        foreach (var app in apps)
        {
            var step = app.Timeline?.FirstOrDefault(t =>
                string.Equals(t.Key, "Interview", StringComparison.OrdinalIgnoreCase)
                && string.Equals(t.State, "Current", StringComparison.OrdinalIgnoreCase));
            if (step?.OccurredAtUtc is not DateTime at)
            {
                continue;
            }

            if (at <= now)
            {
                continue;
            }

            if (best is null || at < best.AtUtc)
            {
                best = new UpcomingInterview(at, app.CompanyName?.Trim() ?? app.VacancyTitle?.Trim() ?? "");
            }
        }

        return best;
    }

    private static string FormatInterviewWhen(DateTime atUtc, string language)
    {
        var culture = language.StartsWith("en", StringComparison.OrdinalIgnoreCase)
            ? CultureInfo.GetCultureInfo("en-GB")
            : CultureInfo.GetCultureInfo("nl-NL");
        var local = atUtc.ToLocalTime();
        if (local.Date == DateTime.Today)
        {
            return language.StartsWith("en", StringComparison.OrdinalIgnoreCase) ? "today" : "vandaag";
        }

        if (local.Date == DateTime.Today.AddDays(1))
        {
            return language.StartsWith("en", StringComparison.OrdinalIgnoreCase) ? "tomorrow" : "morgen";
        }

        return local.ToString("dddd", culture).ToLower(culture);
    }

    private static string? FirstName(MeProfile? profile)
    {
        if (!string.IsNullOrWhiteSpace(profile?.FirstName))
        {
            return profile!.FirstName;
        }

        var full = profile?.FullName?.Trim();
        if (string.IsNullOrWhiteSpace(full))
        {
            return null;
        }

        var parts = full.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        return parts.Length > 0 ? parts[0] : null;
    }

    private static bool TryVacancyId(string path, out Guid id)
    {
        id = default;
        if (!path.StartsWith("/vacancies/", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        var segment = path["/vacancies/".Length..].Trim('/');
        var guidPart = segment.Split('/', StringSplitOptions.RemoveEmptyEntries).FirstOrDefault();
        return guidPart is not null && Guid.TryParse(guidPart, out id);
    }

    private static string RoutePath(string uri)
    {
        if (!Uri.TryCreate(uri, UriKind.Absolute, out var parsed))
        {
            return NormalizePagePath(uri);
        }

        return parsed.AbsolutePath.TrimEnd('/').ToLowerInvariant();
    }

    private static string NormalizePagePath(string? path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return string.Empty;
        }

        var trimmed = path.Trim();
        if (Uri.TryCreate(trimmed, UriKind.Absolute, out var absolute))
        {
            return absolute.AbsolutePath.TrimEnd('/').ToLowerInvariant();
        }

        var q = trimmed.IndexOf('?', StringComparison.Ordinal);
        if (q >= 0)
        {
            trimmed = trimmed[..q];
        }

        var hash = trimmed.IndexOf('#', StringComparison.Ordinal);
        if (hash >= 0)
        {
            trimmed = trimmed[..hash];
        }

        if (!trimmed.StartsWith('/'))
        {
            trimmed = "/" + trimmed;
        }

        return trimmed.TrimEnd('/').ToLowerInvariant();
    }

    private static string Format(CultureState culture, string key, string arg)
        => string.Format(culture[key], arg);

    private sealed record UpcomingInterview(DateTime AtUtc, string CompanyName);
}
