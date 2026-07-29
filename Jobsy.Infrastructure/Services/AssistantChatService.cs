using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Jobsy.Core.Authorization;
using Jobsy.Core.Contracts;
using Jobsy.Core.Enums;
using Jobsy.Core.Interfaces;
using Jobsy.Core.Localization;
using Jobsy.Core.Options;
using Jobsy.Core.Rules;
using Jobsy.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Jobsy.Infrastructure.Services;

public sealed class AssistantChatService : IAssistantChatService
{
    public const int MaxHistoryMessages = 20;
    public const int MaxMessageChars = 1_500;

    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    private static readonly string[] SearchStopwords =
    [
        "ik", "zoek", "zoeken", "een", "de", "het", "een", "vacature", "vacatures", "baan", "banen",
        "job", "jobs", "als", "voor", "naar", "op", "de", "kaart", "toon", "tonen", "vind", "vinden",
        "show", "find", "search", "looking", "want", "wil", "graag", "graag", "bij", "met", "van",
        "in", "mijn", "me", "kan", "je", "jij", "mij", "please", "for", "the", "a", "an", "and",
        "or", "is", "zijn", "daar", "hier", "lobsy", "jobsy", "open", "openen", "link", "doorlink"
    ];

    private readonly JobsyDbContext _db;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IIntegrationCredentialService _credentials;
    private readonly IMetricsQueryService _metrics;
    private readonly ICandidateMetricsQueryService _candidateMetrics;
    private readonly ISalesManagerDashboardService _salesDashboard;
    private readonly OpenAiOptions _options;
    private readonly ILogger<AssistantChatService> _logger;

    public AssistantChatService(
        JobsyDbContext db,
        IHttpClientFactory httpClientFactory,
        IIntegrationCredentialService credentials,
        IMetricsQueryService metrics,
        ICandidateMetricsQueryService candidateMetrics,
        ISalesManagerDashboardService salesDashboard,
        IOptions<OpenAiOptions> options,
        ILogger<AssistantChatService> logger)
    {
        _db = db;
        _httpClientFactory = httpClientFactory;
        _credentials = credentials;
        _metrics = metrics;
        _candidateMetrics = candidateMetrics;
        _salesDashboard = salesDashboard;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<AssistantChatResult> ChatAsync(
        AssistantChatContext context,
        IReadOnlyList<AssistantChatMessage> history,
        CancellationToken cancellationToken = default)
    {
        var sanitized = Sanitize(history);
        var lastUser = sanitized.LastOrDefault(m => m.Role == "user")?.Content?.Trim() ?? "";
        if (lastUser.Length == 0 && sanitized.Count == 0)
        {
            return new AssistantChatResult(Greeting(context), UsedAi: false, []);
        }

        if (IsOffTopicOrForbidden(lastUser, context.Role))
        {
            return new AssistantChatResult(RefuseMessage(context), UsedAi: false, []);
        }

        // Deterministic tool intents first (reliable map filters / KPIs).
        var scripted = await TryScriptedAsync(context, lastUser, cancellationToken);
        if (scripted is not null)
        {
            return scripted;
        }

        var apiKey = await ResolveApiKeyAsync(cancellationToken);
        if (!string.IsNullOrWhiteSpace(apiKey))
        {
            try
            {
                var ai = await CompleteWithOpenAiAsync(context, sanitized, apiKey, cancellationToken);
                if (!string.IsNullOrWhiteSpace(ai))
                {
                    return new AssistantChatResult(ai.Trim(), UsedAi: true, []);
                }
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogWarning(ex, "Assistant OpenAI call failed; falling back.");
            }
        }

        return new AssistantChatResult(FallbackHelp(context), UsedAi: false, []);
    }

    private async Task<AssistantChatResult?> TryScriptedAsync(
        AssistantChatContext context,
        string lastUser,
        CancellationToken cancellationToken)
    {
        var text = lastUser.ToLowerInvariant();

        if (string.Equals(context.Role, JobsyRoles.Candidate, StringComparison.Ordinal))
        {
            if (LooksLikeHowLobsy(text))
            {
                var lang = JobsyLanguages.Normalize(context.Language);
                var reply = lang switch
                {
                    "en" => "As a candidate on Lobsy you: (1) browse the job map, (2) complete your profile, (3) like/share jobs, (4) apply, (5) track applications, (6) wait for the employer. I can open the how-to page for you.",
                    "pl" => "Jako kandydat w Lobsy: (1) przeglądasz mapę ofert, (2) uzupełniasz profil, (3) lubisz/udostępniasz oferty, (4) aplikujesz, (5) śledzisz status, (6) czekasz na pracodawcę. Mogę otworzyć stronę z instrukcją.",
                    "ro" => "Ca și candidat pe Lobsy: (1) vezi harta joburilor, (2) completezi profilul, (3) like/share, (4) aplici, (5) urmărești statusul, (6) aștepți angajatorul. Pot deschide ghidul.",
                    "ar" => "كمترشح على Lobsy: (1) تستعرض خريطة الوظائف، (2) تكمل ملفك، (3) تعجب/تشارك، (4) تتقدم، (5) تتابع الطلبات، (6) تنتظر صاحب العمل. يمكنني فتح صفحة الشرح.",
                    _ => "Als kandidaat op Lobsy: (1) bekijk de banenkaart, (2) vul je profiel, (3) like/deel vacatures, (4) solliciteer, (5) volg je sollicitaties, (6) wacht op de werkgever. Ik kan de uitlegpagina openen."
                };
                return new AssistantChatResult(
                    reply,
                    false,
                    [new AssistantChatAction(AssistantActionTypes.Navigate, Url: "/candidate/hoe-werkt-lobsy", Label: "Hoe werkt Lobsy")]);
            }

            if (LooksLikeApplicationStatus(text))
            {
                return await CandidateApplicationsAsync(context, cancellationToken);
            }

            if (LooksLikeCandidateStats(text))
            {
                return await CandidateStatsAsync(context, DetectPeriod(text), cancellationToken);
            }

            var workType = DetectWorkType(text);
            var jobQuery = ExtractJobSearchQuery(lastUser, workType);
            if (workType is not null || jobQuery is not null || LooksLikeVacancySearch(text))
            {
                return await CandidateVacancySearchAsync(context, workType, jobQuery, cancellationToken);
            }
        }

        if (string.Equals(context.Role, JobsyRoles.Admin, StringComparison.Ordinal))
        {
            if (LooksLikeNawRequest(text))
            {
                return new AssistantChatResult(RefuseNaw(context), false, []);
            }

            if (LooksLikeSalesManagerActivity(text))
            {
                return await AdminMostActiveSalesManagerAsync(context, cancellationToken);
            }

            if (LooksLikeSiteVisits(text))
            {
                return await AdminSiteVisitsAsync(context, DetectPeriod(text), cancellationToken);
            }

            if (LooksLikeLeastClicks(text))
            {
                return await ManagerLeastClicksAsync(context, cancellationToken);
            }

            if (LooksLikeMostClicks(text))
            {
                return await ManagerMostClicksAsync(context, cancellationToken);
            }

            if (LooksLikeTractionAdvice(text))
            {
                return await ManagerTractionAdviceAsync(context, cancellationToken);
            }

            if (LooksLikeKpi(text) || LooksLikePlatformStats(text))
            {
                return await ManagerKpisAsync(context, DetectPeriod(text), cancellationToken);
            }
        }

        if (JobsyRoles.EmployerRoles.Contains(context.Role))
        {
            if (LooksLikeNawRequest(text))
            {
                return new AssistantChatResult(RefuseNaw(context), false, []);
            }

            if (LooksLikeLeastClicks(text))
            {
                return await ManagerLeastClicksAsync(context, cancellationToken);
            }

            if (LooksLikeMostClicks(text))
            {
                return await ManagerMostClicksAsync(context, cancellationToken);
            }

            if (LooksLikeTractionAdvice(text))
            {
                return await ManagerTractionAdviceAsync(context, cancellationToken);
            }

            if (LooksLikeActiveVacancies(text))
            {
                return await ManagerActiveVacanciesAsync(context, cancellationToken);
            }

            if (LooksLikeKpi(text) || LooksLikeApplicationCount(text))
            {
                return await ManagerKpisAsync(context, DetectPeriod(text), cancellationToken);
            }
        }

        if (string.Equals(context.Role, JobsyRoles.SalesManager, StringComparison.Ordinal))
        {
            if (LooksLikeSalesDashboard(text) || LooksLikeKpi(text) || text.Contains("commissie") || text.Contains("commission")
                || text.Contains("invoice") || text.Contains("factuur") || text.Contains("referral") || text.Contains("doorverwijs")
                || text.Contains("leverancier") || text.Contains("supplier") || text.Contains("tracking"))
            {
                return await SalesManagerSummaryAsync(context, cancellationToken);
            }
        }

        return null;
    }

    private async Task<AssistantChatResult> CandidateVacancySearchAsync(
        AssistantChatContext context,
        string? workType,
        string? searchQuery,
        CancellationToken cancellationToken)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var all = await _db.Vacancies.AsNoTracking()
            .Include(v => v.Company)
            .Where(v => v.Status == VacancyStatus.Active && v.StartDate <= today && v.EndDate >= today)
            .Select(v => new VacancySearchRow(
                v.Id,
                v.Title,
                v.Description,
                v.Company.Name,
                v.WorkTypes,
                v.WorkTypeLabels,
                v.RequiredDrivingLicense,
                v.RequiredEducation))
            .ToListAsync(cancellationToken);

        IEnumerable<VacancySearchRow> filtered = all;
        if (!string.IsNullOrWhiteSpace(workType))
        {
            filtered = all.Where(v => WorkTypeLabels.MatchesFilter(v.WorkTypes, v.WorkTypeLabels, workType));
        }

        var matched = filtered
            .Where(v => VacancyTextSearch.MatchesText(
                v.Title, v.Description, v.WorkTypeLabels, v.RequiredDrivingLicense, v.RequiredEducation, searchQuery))
            .ToList();

        var count = matched.Count;
        var matches = matched.Take(8).ToList();

        var lang = JobsyLanguages.Normalize(context.Language);
        var label = !string.IsNullOrWhiteSpace(searchQuery)
            ? searchQuery
            : workType ?? (lang == "en" ? "all sectors" : "alle branches");

        var sb = new StringBuilder();
        if (count == 0)
        {
            sb.Append(lang switch
            {
                "en" => $"I couldn’t find vacancies for “{label}”. Try another job title or sector.",
                _ => $"Ik kon geen vacatures vinden voor “{label}”. Probeer een andere functienaam of branche."
            });
        }
        else
        {
            sb.AppendLine(lang switch
            {
                "en" => $"I found {count} vacancies for “{label}”. Showing them on the job map (hidden search filter).",
                _ => $"Ik heb {count} vacatures gevonden voor “{label}”. Ik toon ze op de banenkaart (verborgen zoekfilter)."
            });

            foreach (var m in matches)
            {
                sb.AppendLine($"• {m.Title} — {m.Company}");
            }

            if (count > matches.Count)
            {
                sb.AppendLine(lang == "en"
                    ? $"…and {count - matches.Count} more on the map."
                    : $"…en nog {count - matches.Count} op de kaart.");
            }
        }

        var url = BuildMapFilterUrl(workType, searchQuery);
        var actions = new List<AssistantChatAction>
        {
            new(AssistantActionTypes.SetFilters, Url: url, WorkType: workType, SearchQuery: searchQuery, Count: count,
                Label: lang == "en" ? "Show on map" : "Toon op kaart"),
            new(AssistantActionTypes.Navigate, Url: url, Label: lang == "en" ? "Job map" : "Banenkaart")
        };

        foreach (var m in matches)
        {
            actions.Add(new AssistantChatAction(
                AssistantActionTypes.Navigate,
                Url: $"/vacancies/{m.Id}",
                Label: m.Title,
                VacancyId: m.Id));
        }

        return new AssistantChatResult(sb.ToString().Trim(), false, actions);
    }

    private sealed record VacancySearchRow(
        Guid Id,
        string Title,
        string Description,
        string Company,
        WorkType WorkTypes,
        string? WorkTypeLabels,
        string? RequiredDrivingLicense,
        string? RequiredEducation);

    private static string BuildMapFilterUrl(string? workType, string? searchQuery)
    {
        var parts = new List<string>();
        if (!string.IsNullOrWhiteSpace(workType))
        {
            parts.Add($"workType={Uri.EscapeDataString(workType)}");
        }

        if (!string.IsNullOrWhiteSpace(searchQuery))
        {
            parts.Add($"q={Uri.EscapeDataString(searchQuery.Trim())}");
        }

        return parts.Count == 0 ? "/" : "/?" + string.Join("&", parts);
    }

    private async Task<AssistantChatResult> CandidateApplicationsAsync(
        AssistantChatContext context,
        CancellationToken cancellationToken)
    {
        var apps = await _db.Applications.AsNoTracking()
            .Include(a => a.Vacancy).ThenInclude(v => v.Company)
            .Where(a => a.CandidateUserId == context.UserId)
            .OrderByDescending(a => a.CreatedAt)
            .Take(8)
            .ToListAsync(cancellationToken);

        var lang = JobsyLanguages.Normalize(context.Language);
        if (apps.Count == 0)
        {
            var empty = lang switch
            {
                "en" => "You don’t have any applications yet. Browse the map and apply when you find a match.",
                _ => "Je hebt nog geen sollicitaties. Zoek op de banenkaart en solliciteer als je iets passends ziet."
            };
            return new AssistantChatResult(
                empty,
                false,
                [new AssistantChatAction(AssistantActionTypes.Navigate, Url: "/candidate/applications", Label: "Mijn sollicitaties")]);
        }

        var sb = new StringBuilder();
        sb.AppendLine(lang == "en"
            ? $"Here are your latest applications ({apps.Count}):"
            : $"Dit zijn je laatste sollicitaties ({apps.Count}):");
        var actions = new List<AssistantChatAction>
        {
            new(AssistantActionTypes.Navigate, Url: "/candidate/applications", Label: lang == "en" ? "My applications" : "Mijn sollicitaties")
        };

        foreach (var a in apps)
        {
            var status = a.Status.ToString();
            sb.AppendLine($"• {a.Vacancy.Title} — {a.Vacancy.Company.Name}: {status}");
            actions.Add(new AssistantChatAction(
                AssistantActionTypes.OpenApplication,
                Url: $"/vacancies/{a.VacancyId}",
                Label: a.Vacancy.Title,
                ApplicationId: a.Id,
                VacancyId: a.VacancyId));
        }

        sb.AppendLine(lang == "en"
            ? "Tap a vacancy below or open My applications for the full list."
            : "Tik op een vacature hieronder of open Mijn sollicitaties voor de volledige lijst.");

        return new AssistantChatResult(sb.ToString().Trim(), false, actions);
    }

    private async Task<AssistantChatResult> CandidateStatsAsync(
        AssistantChatContext context,
        string period,
        CancellationToken cancellationToken)
    {
        var metrics = await _candidateMetrics.GetSummaryAsync(context.UserId, period, cancellationToken);
        var lang = JobsyLanguages.Normalize(context.Language);
        var periodLabel = PeriodLabel(period, lang);
        var sb = new StringBuilder();
        sb.AppendLine(lang == "en"
            ? $"Your activity ({periodLabel}) — only your own profile:"
            : $"Jouw activiteit ({periodLabel}) — alleen binnen jouw profiel:");
        foreach (var m in metrics)
        {
            sb.AppendLine($"• {m.Label}: {m.Value}");
        }

        return new AssistantChatResult(
            sb.ToString().Trim(),
            false,
            [new AssistantChatAction(AssistantActionTypes.Navigate, Url: "/home", Label: "Dashboard")]);
    }

    private async Task<AssistantChatResult> ManagerKpisAsync(
        AssistantChatContext context,
        string period,
        CancellationToken cancellationToken)
    {
        var includePlatform = string.Equals(context.Role, JobsyRoles.Admin, StringComparison.Ordinal);
        var metrics = await _metrics.GetSummaryAsync(includePlatform, context.AccessibleCompanyIds, period, cancellationToken);
        var lang = JobsyLanguages.Normalize(context.Language);
        var periodLabel = PeriodLabel(period, lang);
        var sb = new StringBuilder();
        sb.AppendLine(lang == "en"
            ? $"KPI snapshot ({periodLabel}) — within your access:"
            : $"KPI-overzicht ({periodLabel}) — binnen jouw bereik:");

        IEnumerable<MetricCountDto> selected = includePlatform
            ? metrics.Where(m => m.Key is "clicks" or "impressions" or "applications" or "active_vacancies"
                or "likes" or "shares" or "tokens_spent" or "site_visits" or "site_visits_unique"
                or "users_active" or "companies_employers")
            : metrics.Where(m => m.Key is "clicks" or "impressions" or "applications" or "active_vacancies"
                or "likes" or "shares" or "tokens_spent");

        foreach (var m in selected.Take(10))
        {
            sb.AppendLine($"• {m.Label}: {m.Value}");
        }

        sb.AppendLine(lang == "en"
            ? "I can also tell you which vacancy has the fewest/most clicks, or suggest how to improve traction."
            : "Ik kan ook zeggen welke vacature de minste/meeste clicks heeft, of tips geven bij weinig tractie.");

        return new AssistantChatResult(
            sb.ToString().Trim(),
            false,
            [new AssistantChatAction(AssistantActionTypes.Navigate, Url: "/home", Label: "Dashboard")]);
    }

    private async Task<AssistantChatResult> AdminSiteVisitsAsync(
        AssistantChatContext context,
        string period,
        CancellationToken cancellationToken)
    {
        var metrics = await _metrics.GetSummaryAsync(true, null, period, cancellationToken);
        var visits = metrics.FirstOrDefault(m => m.Key == "site_visits");
        var unique = metrics.FirstOrDefault(m => m.Key == "site_visits_unique");
        var lang = JobsyLanguages.Normalize(context.Language);
        var periodLabel = PeriodLabel(period, lang);
        var reply = lang == "en"
            ? $"Lobsy site visits ({periodLabel}): {visits?.Value ?? 0} total, {unique?.Value ?? 0} unique visitors."
            : $"Sitebezoeken op Lobsy ({periodLabel}): {visits?.Value ?? 0} totaal, {unique?.Value ?? 0} unieke bezoekers.";

        return new AssistantChatResult(
            reply,
            false,
            [new AssistantChatAction(AssistantActionTypes.Navigate, Url: "/home", Label: "Dashboard")]);
    }

    private async Task<AssistantChatResult> AdminMostActiveSalesManagerAsync(
        AssistantChatContext context,
        CancellationToken cancellationToken)
    {
        var managers = await _salesDashboard.ListSalesManagersAsync(cancellationToken);
        var lang = JobsyLanguages.Normalize(context.Language);
        if (managers.Count == 0)
        {
            return new AssistantChatResult(
                lang == "en" ? "There are no sales managers yet." : "Er zijn nog geen salesmanagers.",
                false,
                []);
        }

        var top = managers
            .OrderByDescending(m => m.SupplierCount)
            .ThenByDescending(m => m.BalanceExVat)
            .ThenBy(m => m.FullName)
            .First();

        var reply = lang == "en"
            ? $"Most active sales manager by referred suppliers: {top.FullName} ({top.Email}) — {top.SupplierCount} suppliers, balance ex VAT {top.BalanceExVat:0.00}."
            : $"Meest actieve salesmanager (op doorverwezen leveranciers): {top.FullName} ({top.Email}) — {top.SupplierCount} leveranciers, saldo ex btw {top.BalanceExVat:0.00}.";

        return new AssistantChatResult(
            reply,
            false,
            [new AssistantChatAction(AssistantActionTypes.Navigate, Url: "/admin/sales-managers", Label: "Salesmanagers")]);
    }

    private async Task<(Guid Id, string Title, string Company, int Clicks)?> RankVacanciesByClicksAsync(
        AssistantChatContext context,
        bool ascending,
        CancellationToken cancellationToken)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var from = DateTime.UtcNow.AddDays(-30);
        var vacancyQuery = _db.Vacancies.AsNoTracking()
            .Include(v => v.Company)
            .Where(v => v.Status == VacancyStatus.Active && v.EndDate >= today);

        if (context.AccessibleCompanyIds is not null)
        {
            vacancyQuery = vacancyQuery.Where(v => context.AccessibleCompanyIds.Contains(v.CompanyId));
        }

        var vacancies = await vacancyQuery.Select(v => new { v.Id, v.Title, Company = v.Company.Name }).ToListAsync(cancellationToken);
        if (vacancies.Count == 0)
        {
            return null;
        }

        var ids = vacancies.Select(v => v.Id).ToList();
        var clickCounts = await _db.VacancyClicks.AsNoTracking()
            .Where(c => ids.Contains(c.VacancyId) && c.CreatedAt >= from)
            .GroupBy(c => c.VacancyId)
            .Select(g => new { VacancyId = g.Key, Count = g.Count() })
            .ToListAsync(cancellationToken);

        var byId = clickCounts.ToDictionary(x => x.VacancyId, x => x.Count);
        var ranked = vacancies
            .Select(v => (v.Id, v.Title, v.Company, Clicks: byId.GetValueOrDefault(v.Id)))
            .OrderBy(x => ascending ? x.Clicks : -x.Clicks)
            .ThenBy(x => x.Title)
            .ToList();

        return ranked[0];
    }

    private async Task<AssistantChatResult> ManagerLeastClicksAsync(
        AssistantChatContext context,
        CancellationToken cancellationToken)
    {
        var worst = await RankVacanciesByClicksAsync(context, ascending: true, cancellationToken);
        var lang = JobsyLanguages.Normalize(context.Language);
        if (worst is null)
        {
            return new AssistantChatResult(
                lang == "en"
                    ? "You have no active vacancies in scope."
                    : "Je hebt geen actieve vacatures in je bereik.",
                false,
                []);
        }

        var reply = lang == "en"
            ? $"Over the last 30 days, “{worst.Value.Title}” ({worst.Value.Company}) has the fewest clicks: {worst.Value.Clicks}. Want tips to improve traction?"
            : $"Over de laatste 30 dagen heeft “{worst.Value.Title}” ({worst.Value.Company}) de minste clicks: {worst.Value.Clicks}. Wil je tips om de tractie te verbeteren?";

        return new AssistantChatResult(
            reply,
            false,
            [new AssistantChatAction(AssistantActionTypes.Navigate, Url: $"/vacancies/{worst.Value.Id}", Label: worst.Value.Title, VacancyId: worst.Value.Id)]);
    }

    private async Task<AssistantChatResult> ManagerMostClicksAsync(
        AssistantChatContext context,
        CancellationToken cancellationToken)
    {
        var best = await RankVacanciesByClicksAsync(context, ascending: false, cancellationToken);
        var lang = JobsyLanguages.Normalize(context.Language);
        if (best is null)
        {
            return new AssistantChatResult(
                lang == "en"
                    ? "You have no active vacancies in scope."
                    : "Je hebt geen actieve vacatures in je bereik.",
                false,
                []);
        }

        var reply = lang == "en"
            ? $"Over the last 30 days, “{best.Value.Title}” ({best.Value.Company}) has the most clicks: {best.Value.Clicks}."
            : $"Over de laatste 30 dagen heeft “{best.Value.Title}” ({best.Value.Company}) de meeste clicks: {best.Value.Clicks}.";

        return new AssistantChatResult(
            reply,
            false,
            [new AssistantChatAction(AssistantActionTypes.Navigate, Url: $"/vacancies/{best.Value.Id}", Label: best.Value.Title, VacancyId: best.Value.Id)]);
    }

    private async Task<AssistantChatResult> ManagerActiveVacanciesAsync(
        AssistantChatContext context,
        CancellationToken cancellationToken)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var query = _db.Vacancies.AsNoTracking()
            .Include(v => v.Company)
            .Where(v => v.Status == VacancyStatus.Active && v.StartDate <= today && v.EndDate >= today);

        if (context.AccessibleCompanyIds is not null)
        {
            query = query.Where(v => context.AccessibleCompanyIds.Contains(v.CompanyId));
        }

        var list = await query
            .OrderBy(v => v.Title)
            .Select(v => new { v.Id, v.Title, Company = v.Company.Name })
            .Take(8)
            .ToListAsync(cancellationToken);

        var lang = JobsyLanguages.Normalize(context.Language);
        if (list.Count == 0)
        {
            return new AssistantChatResult(
                lang == "en" ? "No active vacancies in your scope." : "Geen actieve vacatures in jouw bereik.",
                false,
                [new AssistantChatAction(AssistantActionTypes.Navigate, Url: "/employer/vacancies", Label: "Vacatures")]);
        }

        var sb = new StringBuilder();
        sb.AppendLine(lang == "en"
            ? $"Active vacancies in your scope ({list.Count} shown):"
            : $"Actieve vacatures in jouw bereik ({list.Count} getoond):");
        var actions = new List<AssistantChatAction>
        {
            new(AssistantActionTypes.Navigate, Url: "/employer/vacancies", Label: lang == "en" ? "Vacancies" : "Vacatures")
        };
        foreach (var v in list)
        {
            sb.AppendLine($"• {v.Title} — {v.Company}");
            actions.Add(new AssistantChatAction(AssistantActionTypes.Navigate, Url: $"/vacancies/{v.Id}", Label: v.Title, VacancyId: v.Id));
        }

        return new AssistantChatResult(sb.ToString().Trim(), false, actions);
    }

    private async Task<AssistantChatResult> ManagerTractionAdviceAsync(
        AssistantChatContext context,
        CancellationToken cancellationToken)
    {
        var least = await ManagerLeastClicksAsync(context, cancellationToken);
        var lang = JobsyLanguages.Normalize(context.Language);
        var tips = lang == "en"
            ? """

Improvement ideas:
• Refresh the title and first lines — lead with a concrete task and hourly wage if allowed.
• Add a clear vacancy photo and complete hard requirements (license/education).
• Use Highlight or PushBom if you have tokens, to reach more candidates nearby.
• Check travel/transport filters: if only car is allowed, fewer candidates match.
• Share the vacancy link and ask current staff to refer.
"""
            : """

Verbetervoorstellen:
• Werk titel en eerste zinnen bij — noem een concrete taak en (indien toegestaan) het uurloon.
• Voeg een duidelijke foto toe en vul harde eisen volledig in.
• Gebruik Highlight of PushBom als je tokens hebt, om meer kandidaten in de buurt te bereiken.
• Check vervoerseisen: alleen auto = minder matches.
• Deel de vacaturelink en vraag collega’s om door te sturen.
""";

        return new AssistantChatResult(
            least.Reply + tips,
            false,
            least.Actions);
    }

    private async Task<AssistantChatResult> SalesManagerSummaryAsync(
        AssistantChatContext context,
        CancellationToken cancellationToken)
    {
        var dash = await _salesDashboard.GetDashboardAsync(context.UserId, cancellationToken);
        var lang = JobsyLanguages.Normalize(context.Language);
        if (dash is null)
        {
            return new AssistantChatResult(
                lang == "en"
                    ? "I couldn’t load your salesmanager dashboard. Complete onboarding first."
                    : "Ik kon je salesmanager-dashboard niet laden. Rond eerst de onboarding af.",
                false,
                [new AssistantChatAction(AssistantActionTypes.Navigate, Url: "/salesmanager/onboarding")]);
        }

        var sb = new StringBuilder();
        if (lang == "en")
        {
            sb.AppendLine($"Within your account: tracking code {dash.TrackingCode}.");
            sb.AppendLine($"Uninvoiced commission (ex VAT): {dash.UninvoicedExVat:0.00}.");
            sb.AppendLine($"Outstanding issued (ex VAT): {dash.OutstandingIssuedExVat:0.00}.");
            sb.AppendLine($"Referred suppliers: {dash.Suppliers.Count}.");
            foreach (var s in dash.Suppliers.Take(5))
            {
                sb.AppendLine($"• {s.Name} (KVK {s.KvkNumber})");
            }
        }
        else
        {
            sb.AppendLine($"Binnen jouw account: trackingcode {dash.TrackingCode}.");
            sb.AppendLine($"Nog niet gefactureerde commissie (ex btw): {dash.UninvoicedExVat:0.00}.");
            sb.AppendLine($"Openstaand gefactureerd (ex btw): {dash.OutstandingIssuedExVat:0.00}.");
            sb.AppendLine($"Doorverwezen leveranciers: {dash.Suppliers.Count}.");
            foreach (var s in dash.Suppliers.Take(5))
            {
                sb.AppendLine($"• {s.Name} (KVK {s.KvkNumber})");
            }
        }

        return new AssistantChatResult(
            sb.ToString().Trim(),
            false,
            [
                new AssistantChatAction(AssistantActionTypes.Navigate, Url: "/salesmanager", Label: "Dashboard"),
                new AssistantChatAction(AssistantActionTypes.Navigate, Url: "/salesmanager/invoices", Label: "Facturen")
            ]);
    }

    private async Task<string?> CompleteWithOpenAiAsync(
        AssistantChatContext context,
        IReadOnlyList<AssistantChatMessage> history,
        string apiKey,
        CancellationToken cancellationToken)
    {
        var lang = MockInterviewLabels.For(context.Language);
        var facts = await BuildScopedFactsAsync(context, cancellationToken);
        var system = BuildSystemPrompt(context, lang.LanguageName, facts);
        var messages = new List<object> { new { role = "system", content = system } };
        foreach (var turn in history.TakeLast(MaxHistoryMessages))
        {
            messages.Add(new { role = turn.Role, content = turn.Content });
        }

        var model = await ResolveModelAsync(cancellationToken);
        var baseUrl = await ResolveBaseUrlAsync(cancellationToken);
        var client = _httpClientFactory.CreateClient("IntegrationProbe");
        using var request = new HttpRequestMessage(
            HttpMethod.Post,
            new Uri(new Uri(baseUrl, UriKind.Absolute), "chat/completions"));
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
        request.Content = JsonContent.Create(new
        {
            model,
            temperature = 0.4,
            max_tokens = 500,
            messages
        });

        using var response = await client.SendAsync(request, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            return null;
        }

        var completion = await response.Content.ReadFromJsonAsync<ChatCompletionResponse>(JsonOptions, cancellationToken);
        return completion?.Choices?.FirstOrDefault()?.Message?.Content;
    }

    private async Task<string> BuildScopedFactsAsync(AssistantChatContext context, CancellationToken cancellationToken)
    {
        try
        {
            if (string.Equals(context.Role, JobsyRoles.Candidate, StringComparison.Ordinal))
            {
                var stats = await _candidateMetrics.GetSummaryAsync(context.UserId, "month", cancellationToken);
                var apps = await _db.Applications.AsNoTracking()
                    .CountAsync(a => a.CandidateUserId == context.UserId, cancellationToken);
                return $"Candidate facts (own profile only): total applications={apps}; month metrics: "
                       + string.Join("; ", stats.Select(m => $"{m.Key}={m.Value}"));
            }

            if (string.Equals(context.Role, JobsyRoles.SalesManager, StringComparison.Ordinal))
            {
                var dash = await _salesDashboard.GetDashboardAsync(context.UserId, cancellationToken);
                if (dash is null)
                {
                    return "Salesmanager facts: onboarding incomplete / no dashboard.";
                }

                return $"Salesmanager facts (own account only): tracking={dash.TrackingCode}; uninvoicedExVat={dash.UninvoicedExVat}; outstandingIssuedExVat={dash.OutstandingIssuedExVat}; suppliers={dash.Suppliers.Count}";
            }

            if (string.Equals(context.Role, JobsyRoles.Admin, StringComparison.Ordinal)
                || JobsyRoles.EmployerRoles.Contains(context.Role))
            {
                var includePlatform = string.Equals(context.Role, JobsyRoles.Admin, StringComparison.Ordinal);
                var metrics = await _metrics.GetSummaryAsync(includePlatform, context.AccessibleCompanyIds, "month", cancellationToken);
                var sb = new StringBuilder();
                sb.Append(includePlatform ? "Admin platform facts: " : "Employer facts (company scope only): ");
                sb.Append(string.Join("; ", metrics.Take(12).Select(m => $"{m.Key}={m.Value}")));
                if (includePlatform)
                {
                    var managers = await _salesDashboard.ListSalesManagersAsync(cancellationToken);
                    var top = managers.OrderByDescending(m => m.SupplierCount).FirstOrDefault();
                    if (top is not null)
                    {
                        sb.Append($"; topSalesManager={top.FullName} suppliers={top.SupplierCount}");
                    }
                }

                return sb.ToString();
            }
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogDebug(ex, "Failed to build assistant scoped facts.");
        }

        return "No extra facts.";
    }

    private static string BuildSystemPrompt(AssistantChatContext context, string languageName, string facts)
    {
        var role = context.Role;
        var scope = role switch
        {
            JobsyRoles.Candidate =>
                "You help a JOBSEEKER on Lobsy only: targeted vacancy search (job titles like forklift/heftruck), job map filters, how Lobsy works, own application status and own activity stats. Never invent vacancies. Never access other users’ data.",
            JobsyRoles.SalesManager =>
                "You help a SALESMANAGER on Lobsy only: their referrals, commissions, invoices, onboarding, tracking code. Stay inside their account. No candidate personal NAW data of third parties.",
            JobsyRoles.Admin =>
                "You help an ADMIN on Lobsy only: platform KPIs, site visits, salesmanager activity, vacancy performance. NEVER reveal candidate NAW (name/address/email/phone/BSN). Refuse topics outside Lobsy.",
            _ =>
                "You help an EMPLOYER/MANAGER on Lobsy only: KPIs for their companies, vacancy performance, traction tips, active vacancies in scope. NEVER reveal candidate NAW. Never answer about other companies outside their access."
        };

        return
            $"You are Lobsy, a helpful lobster assistant. Reply in {languageName}. Be concise (max ~120 words). {scope} " +
            "Answer using ONLY the scoped facts below and general Lobsy product knowledge. " +
            "If the user asks something outside Lobsy or outside their role permissions, politely refuse. " +
            $"Scoped facts:\n{facts}";
    }

    private static string Greeting(AssistantChatContext context)
    {
        var lang = JobsyLanguages.Normalize(context.Language);
        return context.Role switch
        {
            JobsyRoles.Candidate => lang == "en"
                ? "Hi! I’m Lobsy. Ask me to find a specific job (e.g. forklift driver), how Lobsy works, or your application status."
                : "Hoi! Ik ben Lobsy. Vraag me om een gerichte vacature (bijv. heftruckchauffeur), hoe Lobsy werkt, of de status van je sollicitaties.",
            JobsyRoles.SalesManager => lang == "en"
                ? "Hi! I can help with your salesmanager account: referrals, commissions, and invoices — only your data."
                : "Hoi! Ik help met je salesmanager-account: doorverwijzingen, commissies en facturen — alleen jouw gegevens.",
            JobsyRoles.Admin => lang == "en"
                ? "Hi! Ask me anything within Lobsy: site visits today, most active sales manager, KPIs, vacancy performance. I won’t share candidate personal details."
                : "Hoi! Vraag me alles binnen Lobsy: sitebezoeken vandaag, meest actieve salesmanager, KPI’s, vacatureprestaties. Kandidate NAW geef ik niet.",
            _ => lang == "en"
                ? "Hi! Ask me about your KPIs, vacancies in your companies, clicks, or tips to improve traction. I only use data in your profile scope."
                : "Hoi! Vraag me naar je KPI’s, vacatures in jouw bedrijven, clicks, of tips bij weinig tractie. Ik kijk alleen binnen jouw bereik."
        };
    }

    private static string FallbackHelp(AssistantChatContext context)
    {
        var lang = JobsyLanguages.Normalize(context.Language);
        return context.Role switch
        {
            JobsyRoles.Candidate => lang == "en"
                ? "I can search vacancies by job title or sector (e.g. “heftruckchauffeur”), explain how Lobsy works, or show your application status."
                : "Ik kan vacatures zoeken op functie of branche (bijv. “heftruckchauffeur”), uitleggen hoe Lobsy werkt, of je sollicitatiestatus tonen.",
            JobsyRoles.SalesManager => lang == "en"
                ? "Try asking about your commissions, referred suppliers, or invoices."
                : "Probeer te vragen naar je commissies, doorverwezen leveranciers of facturen.",
            JobsyRoles.Admin => lang == "en"
                ? "Try asking how often Lobsy was visited today, which sales manager is most active, or for a KPI overview."
                : "Probeer te vragen hoe vaak Lobsy vandaag is bezocht, welke salesmanager het meest actief is, of om een KPI-overzicht.",
            _ => lang == "en"
                ? "Try asking for your KPI overview, the vacancy with the fewest/most clicks, active vacancies, or tips for low traction."
                : "Probeer te vragen naar je KPI-overzicht, de vacature met de minste/meeste clicks, actieve vacatures, of tips bij weinig tractie."
        };
    }

    private static string RefuseMessage(AssistantChatContext context)
    {
        var lang = JobsyLanguages.Normalize(context.Language);
        return lang == "en"
            ? "I can only help with Lobsy topics within your role. Please ask about jobs, applications, KPIs, or your account."
            : "Ik help alleen met Lobsy-onderwerpen binnen jouw rol. Vraag gerust naar vacatures, sollicitaties, KPI’s of je account.";
    }

    private static string RefuseNaw(AssistantChatContext context)
    {
        var lang = JobsyLanguages.Normalize(context.Language);
        return lang == "en"
            ? "I can’t share candidate personal details (name, address, email, phone). I can help with KPIs and vacancy performance instead."
            : "Ik mag geen NAW-gegevens van kandidaten delen (naam, adres, e-mail, telefoon). Wel help ik met KPI’s en vacatureprestaties.";
    }

    private static bool IsOffTopicOrForbidden(string text, string role)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return false;
        }

        var off = new[]
        {
            "weerbericht", "weather", "recept", "recipe", "voetbal", "crypto", "bitcoin",
            "schrijf een gedicht", "write a poem", "joke", "mop"
        };
        if (off.Any(o => text.Contains(o, StringComparison.OrdinalIgnoreCase)))
        {
            return true;
        }

        // Cross-role leakage: candidates must not ask for platform/admin/sales stats.
        if (string.Equals(role, JobsyRoles.Candidate, StringComparison.Ordinal)
            && ContainsAny(text, "salesmanager", "sitebezoek", "site visit", "alle bedrijven", "platform kpi", "welke manager"))
        {
            return true;
        }

        // Employers (non-admin) must not ask platform-wide / other-company questions.
        if (JobsyRoles.EmployerRoles.Contains(role)
            && ContainsAny(text, "sitebezoek", "site visit", "salesmanager", "alle bedrijven", "heel lobsy", "platformbreed"))
        {
            return true;
        }

        return false;
    }

    private static bool LooksLikeNawRequest(string text)
    {
        if (ContainsAny(text, "naw", "bsn", "persoonsgegevens", "personal details", "privacygegevens"))
        {
            return true;
        }

        var aboutCandidate = ContainsAny(text, "kandidaat", "candidate", "sollicitant", "applicant");
        if (!aboutCandidate)
        {
            return false;
        }

        return ContainsAny(text,
            "adres", "address", "telefoon", "phone", "email", "e-mail",
            "woonplaats", "naam", "name", "wachtwoord", "password");
    }

    private static bool LooksLikeHowLobsy(string text) =>
        ContainsAny(text, "hoe werkt", "how does lobsy", "how lobsy", "uitleg", "how to use", "wat kan ik");

    private static bool LooksLikeApplicationStatus(string text) =>
        ContainsAny(text, "sollicitatie", "application status", "mijn sollicitat", "status van mijn", "my application");

    private static bool LooksLikeVacancySearch(string text) =>
        ContainsAny(text, "vacature", "vacatures", "banen", "jobs", "zoek", "search", "toon", "show", "vind", "find",
            "heftruck", "reachtruck", "chauffeur", "magazijn", "orderpicker", "barista", "plukker");

    private static bool LooksLikeCandidateStats(string text) =>
        ContainsAny(text, "hoe vaak heb ik", "mijn likes", "mijn shares", "mijn statistiek", "my stats", "mijn activiteit", "how many likes");

    private static bool LooksLikeKpi(string text) =>
        ContainsAny(text, "kpi", "statistiek", "metrics", "clicks", "klik", "impressies", "impressions", "prestatie", "dashboard", "overzicht");

    private static bool LooksLikePlatformStats(string text) =>
        ContainsAny(text, "gebruikers", "bedrijven", "users", "companies", "tokens", "open for work");

    private static bool LooksLikeSiteVisits(string text) =>
        ContainsAny(text, "sitebezoek", "sitebezoeken", "bezocht", "bezoeken", "site visit", "visits", "hoe vaak is lobsy", "how often");

    private static bool LooksLikeSalesManagerActivity(string text) =>
        ContainsAny(text, "salesmanager", "sales manager", "meest actief", "most active", "actiefste");

    private static bool LooksLikeLeastClicks(string text) =>
        ContainsAny(text, "minste click", "fewest click", "minste klik", "laagste click", "least click", "weinigste click");

    private static bool LooksLikeMostClicks(string text) =>
        ContainsAny(text, "meeste click", "most click", "meeste klik", "hoogste click", "best performing", "beste vacature");

    private static bool LooksLikeTractionAdvice(string text) =>
        ContainsAny(text, "tractie", "traction", "weinig reactie", "weinig click", "verbeter", "improve", "waarom weinig", "why low");

    private static bool LooksLikeActiveVacancies(string text) =>
        ContainsAny(text, "actieve vacature", "mijn vacature", "welke vacature", "which vacancy", "openstaande vacature");

    private static bool LooksLikeApplicationCount(string text) =>
        ContainsAny(text, "sollicitat", "application", "reactie", "aanmeld");

    private static bool LooksLikeSalesDashboard(string text) =>
        ContainsAny(text, "dashboard", "account", "overzicht", "summary", "stand van");

    private static string DetectPeriod(string text)
    {
        if (ContainsAny(text, "vandaag", "today", "dag"))
        {
            return "day";
        }

        if (ContainsAny(text, "week", "deze week", "this week"))
        {
            return "week";
        }

        if (ContainsAny(text, "jaar", "year"))
        {
            return "year";
        }

        if (ContainsAny(text, "kwartaal", "quarter"))
        {
            return "quarter";
        }

        // Default month for generic KPI questions; day when "vandaag" already handled.
        return "month";
    }

    private static string PeriodLabel(string period, string lang) =>
        (period, lang == "en") switch
        {
            ("day", true) => "today",
            ("day", false) => "vandaag",
            ("week", true) => "this week",
            ("week", false) => "deze week",
            ("year", true) => "this year",
            ("year", false) => "dit jaar",
            ("quarter", true) => "this quarter",
            ("quarter", false) => "dit kwartaal",
            (_, true) => "this month",
            _ => "deze maand"
        };

    /// <summary>
    /// Pull a job-title style query from free text (e.g. "heftruckchauffeur"), excluding branch labels.
    /// </summary>
    public static string? ExtractJobSearchQuery(string raw, string? detectedWorkType)
    {
        if (string.IsNullOrWhiteSpace(raw))
        {
            return null;
        }

        var tokens = VacancyTextSearch.Normalize(raw)
            .Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(t => t.Length >= 3)
            .Where(t => !SearchStopwords.Contains(t, StringComparer.OrdinalIgnoreCase))
            .Where(t => detectedWorkType is null
                        || !t.Equals(VacancyTextSearch.Normalize(detectedWorkType), StringComparison.OrdinalIgnoreCase))
            .Where(t => WorkTypeLabels.All.All(w =>
                !t.Equals(VacancyTextSearch.Normalize(w), StringComparison.OrdinalIgnoreCase)))
            .ToList();

        // Drop common English/Dutch branch synonyms already mapped to work types.
        var branchSynonyms = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "horeca", "hospitality", "logistiek", "warehouse", "magazijn", "retail", "winkel", "shop",
            "tuinbouw", "zorg", "care", "healthcare", "kantoor", "office", "bouw", "construction",
            "schoonmaak", "cleaning", "productie", "production", "fabriek", "cafe", "restaurant", "bar"
        };
        tokens = tokens.Where(t => !branchSynonyms.Contains(t)).ToList();

        if (tokens.Count == 0)
        {
            return null;
        }

        // Prefer the longest token (often the compound job title).
        var best = tokens.OrderByDescending(t => t.Length).First();
        return best.Length >= 4 ? best : string.Join(' ', tokens);
    }

    private static string? DetectWorkType(string text)
    {
        foreach (var label in WorkTypeLabels.All)
        {
            if (text.Contains(label, StringComparison.OrdinalIgnoreCase))
            {
                return label;
            }
        }

        if (ContainsAny(text, "horeca", "hospitality", "café", "cafe", "restaurant", "bar"))
        {
            return WorkTypeLabels.Horeca;
        }

        if (ContainsAny(text, "logistiek", "warehouse", "magazijn", "bezorg"))
        {
            return WorkTypeLabels.Logistiek;
        }

        if (ContainsAny(text, "retail", "winkel", "shop"))
        {
            return WorkTypeLabels.Winkel;
        }

        if (ContainsAny(text, "tuinbouw", "kas", "greenhouse"))
        {
            return WorkTypeLabels.Tuinbouw;
        }

        if (ContainsAny(text, "zorg", "care", "healthcare"))
        {
            return WorkTypeLabels.Zorg;
        }

        if (ContainsAny(text, "kantoor", "office"))
        {
            return WorkTypeLabels.Kantoor;
        }

        if (ContainsAny(text, "bouw", "construction"))
        {
            return WorkTypeLabels.Bouw;
        }

        if (ContainsAny(text, "schoonmaak", "cleaning"))
        {
            return WorkTypeLabels.Schoonmaak;
        }

        if (ContainsAny(text, "productie", "production", "fabriek"))
        {
            return WorkTypeLabels.Productie;
        }

        return null;
    }

    private static bool ContainsAny(string text, params string[] needles)
        => needles.Any(n => text.Contains(n, StringComparison.OrdinalIgnoreCase));

    public static IReadOnlyList<AssistantChatMessage> Sanitize(IReadOnlyList<AssistantChatMessage> history)
    {
        var cleaned = new List<AssistantChatMessage>();
        foreach (var msg in history.TakeLast(MaxHistoryMessages))
        {
            var role = msg.Role?.Trim().ToLowerInvariant();
            if (role is not ("user" or "assistant"))
            {
                continue;
            }

            var content = (msg.Content ?? string.Empty).Trim();
            if (content.Length == 0)
            {
                continue;
            }

            if (content.Length > MaxMessageChars)
            {
                content = content[..MaxMessageChars];
            }

            cleaned.Add(new AssistantChatMessage(role, content));
        }

        return cleaned;
    }

    private async Task<string?> ResolveApiKeyAsync(CancellationToken cancellationToken)
    {
        var fromDb = await _credentials.GetRawApiKeyAsync(IntegrationKey.OpenAI, cancellationToken);
        if (!string.IsNullOrWhiteSpace(fromDb))
        {
            return fromDb;
        }

        return string.IsNullOrWhiteSpace(_options.ApiKey) ? null : _options.ApiKey.Trim();
    }

    private async Task<string> ResolveModelAsync(CancellationToken cancellationToken)
    {
        var fromDb = await _credentials.GetModelAsync(IntegrationKey.OpenAI, cancellationToken);
        return !string.IsNullOrWhiteSpace(fromDb)
            ? fromDb
            : (string.IsNullOrWhiteSpace(_options.Model) ? "gpt-4o-mini" : _options.Model.Trim());
    }

    private async Task<string> ResolveBaseUrlAsync(CancellationToken cancellationToken)
    {
        var fromDb = await _credentials.GetBaseUrlAsync(IntegrationKey.OpenAI, cancellationToken);
        if (!string.IsNullOrWhiteSpace(fromDb)
            && IntegrationEndpointUrl.TryNormalizeBaseUrl(fromDb, out var normalized, out _)
            && !string.IsNullOrWhiteSpace(normalized))
        {
            return normalized;
        }

        var fallback = string.IsNullOrWhiteSpace(_options.BaseUrl) ? "https://api.openai.com/v1/" : _options.BaseUrl;
        if (IntegrationEndpointUrl.TryNormalizeBaseUrl(fallback, out var normalizedFallback, out _)
            && !string.IsNullOrWhiteSpace(normalizedFallback))
        {
            return normalizedFallback;
        }

        return "https://api.openai.com/v1/";
    }

    private sealed class ChatCompletionResponse
    {
        public List<ChatChoice>? Choices { get; set; }
    }

    private sealed class ChatChoice
    {
        public ChatMessage? Message { get; set; }
    }

    private sealed class ChatMessage
    {
        public string? Content { get; set; }
    }
}
