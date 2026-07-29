using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Jobsy.Core.Authorization;
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

    private readonly JobsyDbContext _db;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IIntegrationCredentialService _credentials;
    private readonly IMetricsQueryService _metrics;
    private readonly ISalesManagerDashboardService _salesDashboard;
    private readonly OpenAiOptions _options;
    private readonly ILogger<AssistantChatService> _logger;

    public AssistantChatService(
        JobsyDbContext db,
        IHttpClientFactory httpClientFactory,
        IIntegrationCredentialService credentials,
        IMetricsQueryService metrics,
        ISalesManagerDashboardService salesDashboard,
        IOptions<OpenAiOptions> options,
        ILogger<AssistantChatService> logger)
    {
        _db = db;
        _httpClientFactory = httpClientFactory;
        _credentials = credentials;
        _metrics = metrics;
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

            var workType = DetectWorkType(text);
            if (workType is not null || LooksLikeVacancySearch(text))
            {
                return await CandidateVacancySearchAsync(context, workType, cancellationToken);
            }
        }

        if (JobsyRoles.EmployerRoles.Contains(context.Role) || string.Equals(context.Role, JobsyRoles.Admin, StringComparison.Ordinal))
        {
            if (LooksLikeNawRequest(text))
            {
                return new AssistantChatResult(RefuseNaw(context), false, []);
            }

            if (LooksLikeLeastClicks(text))
            {
                return await ManagerLeastClicksAsync(context, cancellationToken);
            }

            if (LooksLikeTractionAdvice(text))
            {
                return await ManagerTractionAdviceAsync(context, cancellationToken);
            }

            if (LooksLikeKpi(text))
            {
                return await ManagerKpisAsync(context, cancellationToken);
            }
        }

        if (string.Equals(context.Role, JobsyRoles.SalesManager, StringComparison.Ordinal))
        {
            if (LooksLikeSalesDashboard(text) || LooksLikeKpi(text) || text.Contains("commissie") || text.Contains("commission")
                || text.Contains("invoice") || text.Contains("factuur") || text.Contains("referral") || text.Contains("doorverwijs"))
            {
                return await SalesManagerSummaryAsync(context, cancellationToken);
            }
        }

        return null;
    }

    private async Task<AssistantChatResult> CandidateVacancySearchAsync(
        AssistantChatContext context,
        string? workType,
        CancellationToken cancellationToken)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var all = await _db.Vacancies.AsNoTracking()
            .Where(v => v.Status == VacancyStatus.Active && v.StartDate <= today && v.EndDate >= today)
            .Select(v => new { v.Id, v.WorkTypes, v.WorkTypeLabels })
            .ToListAsync(cancellationToken);

        var count = string.IsNullOrWhiteSpace(workType)
            ? all.Count
            : all.Count(v => WorkTypeLabels.MatchesFilter(v.WorkTypes, v.WorkTypeLabels, workType));

        var lang = JobsyLanguages.Normalize(context.Language);
        var branch = workType ?? (lang == "en" ? "all sectors" : "alle branches");
        var reply = lang switch
        {
            "en" => $"I found {count} vacancies for {branch}. I’m showing them on the job map with the right filters.",
            "pl" => $"Znalazłem {count} ofert dla {branch}. Pokazuję je na mapie z odpowiednimi filtrami.",
            "ro" => $"Am găsit {count} joburi pentru {branch}. Le afișez pe hartă cu filtrele potrivite.",
            "ar" => $"وجدت {count} وظائف لـ {branch}. سأعرضها على الخريطة بالفلاتر المناسبة.",
            _ => $"Ik heb {count} vacatures gevonden voor {branch}. Ik toon ze op de banenkaart met de juiste filters."
        };

        var url = string.IsNullOrWhiteSpace(workType) ? "/" : $"/?workType={Uri.EscapeDataString(workType)}";
        return new AssistantChatResult(
            reply,
            false,
            [
                new AssistantChatAction(AssistantActionTypes.SetFilters, Url: url, WorkType: workType, Count: count),
                new AssistantChatAction(AssistantActionTypes.Navigate, Url: url)
            ]);
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

    private async Task<AssistantChatResult> ManagerKpisAsync(
        AssistantChatContext context,
        CancellationToken cancellationToken)
    {
        var includePlatform = string.Equals(context.Role, JobsyRoles.Admin, StringComparison.Ordinal);
        var metrics = await _metrics.GetSummaryAsync(includePlatform, context.AccessibleCompanyIds, "month", cancellationToken);
        var lang = JobsyLanguages.Normalize(context.Language);
        var sb = new StringBuilder();
        sb.AppendLine(lang == "en" ? "KPI snapshot (this month):" : "KPI-overzicht (deze maand):");
        foreach (var m in metrics.Where(m => m.Key is "clicks" or "impressions" or "applications" or "active_vacancies" or "likes" or "shares" or "tokens_spent")
                     .Take(8))
        {
            sb.AppendLine($"• {m.Label}: {m.Value}");
        }

        sb.AppendLine(lang == "en"
            ? "I can also tell you which vacancy has the fewest clicks, or suggest how to improve traction."
            : "Ik kan ook zeggen welke vacature de minste clicks heeft, of tips geven bij weinig tractie.");

        return new AssistantChatResult(
            sb.ToString().Trim(),
            false,
            [new AssistantChatAction(AssistantActionTypes.Navigate, Url: "/home", Label: "Dashboard")]);
    }

    private async Task<AssistantChatResult> ManagerLeastClicksAsync(
        AssistantChatContext context,
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
            return new AssistantChatResult(
                JobsyLanguages.Normalize(context.Language) == "en"
                    ? "You have no active vacancies in scope."
                    : "Je hebt geen actieve vacatures in je bereik.",
                false,
                []);
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
            .OrderBy(x => x.Clicks)
            .ThenBy(x => x.Title)
            .ToList();

        var worst = ranked[0];
        var lang = JobsyLanguages.Normalize(context.Language);
        var reply = lang == "en"
            ? $"Over the last 30 days, “{worst.Title}” ({worst.Company}) has the fewest clicks: {worst.Clicks}. Want tips to improve traction?"
            : $"Over de laatste 30 dagen heeft “{worst.Title}” ({worst.Company}) de minste clicks: {worst.Clicks}. Wil je tips om de tractie te verbeteren?";

        return new AssistantChatResult(
            reply,
            false,
            [new AssistantChatAction(AssistantActionTypes.Navigate, Url: $"/vacancies/{worst.Id}", Label: worst.Title, VacancyId: worst.Id)]);
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

        var reply = lang == "en"
            ? $"Within your account: tracking code {dash.TrackingCode}. Uninvoiced commission (ex VAT): {dash.UninvoicedExVat:0.00}. Outstanding issued (ex VAT): {dash.OutstandingIssuedExVat:0.00}. Referred suppliers: {dash.Suppliers.Count}. I can open invoices or onboarding."
            : $"Binnen jouw account: trackingcode {dash.TrackingCode}. Nog niet gefactureerde commissie (ex btw): {dash.UninvoicedExVat:0.00}. Openstaand gefactureerd (ex btw): {dash.OutstandingIssuedExVat:0.00}. Doorverwezen leveranciers: {dash.Suppliers.Count}. Ik kan facturen of onboarding openen.";

        return new AssistantChatResult(
            reply,
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
        var system = BuildSystemPrompt(context, lang.LanguageName);
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

    private static string BuildSystemPrompt(AssistantChatContext context, string languageName)
    {
        var role = context.Role;
        var scope = role switch
        {
            JobsyRoles.Candidate =>
                "You help a JOBSEEKER on Lobsy only: job map filters, how Lobsy works, application status. Never invent vacancies. Refuse unrelated topics.",
            JobsyRoles.SalesManager =>
                "You help a SALESMANAGER on Lobsy only: their referrals, commissions, invoices, onboarding, tracking code. Stay inside their account. No candidate personal NAW data of third parties.",
            _ =>
                "You help an EMPLOYER/MANAGER on Lobsy only: KPIs (clicks, impressions, applications), which vacancy has few clicks, and traction improvement tips. NEVER reveal candidate NAW (name/address/email/phone/BSN). Refuse unrelated topics."
        };

        return
            $"You are Lobsy, a helpful lobster assistant. Reply in {languageName}. Be concise (max ~120 words). {scope} " +
            "If the user asks something outside Lobsy or outside their role permissions, politely refuse.";
    }

    private static string Greeting(AssistantChatContext context)
    {
        var lang = JobsyLanguages.Normalize(context.Language);
        return context.Role switch
        {
            JobsyRoles.Candidate => lang == "en"
                ? "Hi! I’m Lobsy. Ask me to find vacancies (e.g. hospitality), how Lobsy works, or the status of your applications."
                : "Hoi! Ik ben Lobsy. Vraag me om vacatures te zoeken (bijv. horeca), hoe Lobsy werkt, of de status van je sollicitaties.",
            JobsyRoles.SalesManager => lang == "en"
                ? "Hi! I can help with your salesmanager account: referrals, commissions, and invoices."
                : "Hoi! Ik help met je salesmanager-account: doorverwijzingen, commissies en facturen.",
            _ => lang == "en"
                ? "Hi! Ask me about your KPIs, which vacancy has the fewest clicks, or tips to improve traction. I won’t share candidate personal details."
                : "Hoi! Vraag me naar je KPI’s, welke vacature de minste clicks heeft, of tips bij weinig tractie. Kandidate NAW-gegevens geef ik niet."
        };
    }

    private static string FallbackHelp(AssistantChatContext context)
    {
        var lang = JobsyLanguages.Normalize(context.Language);
        return context.Role switch
        {
            JobsyRoles.Candidate => lang == "en"
                ? "I can search vacancies by sector (e.g. “show hospitality jobs”), explain how Lobsy works, or show your application status."
                : "Ik kan vacatures zoeken op branche (bijv. “toon horeca vacatures”), uitleggen hoe Lobsy werkt, of je sollicitatiestatus tonen.",
            JobsyRoles.SalesManager => lang == "en"
                ? "Try asking about your commissions, referred suppliers, or invoices."
                : "Probeer te vragen naar je commissies, doorverwezen leveranciers of facturen.",
            _ => lang == "en"
                ? "Try asking for your KPI overview, the vacancy with the fewest clicks, or tips for low traction."
                : "Probeer te vragen naar je KPI-overzicht, de vacature met de minste clicks, of tips bij weinig tractie."
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

        // Clear off-Lobsy topics
        var off = new[]
        {
            "weerbericht", "weather", "recept", "recipe", "voetbal", "crypto", "bitcoin",
            "schrijf een gedicht", "write a poem", "joke", "mop"
        };
        if (off.Any(o => text.Contains(o, StringComparison.OrdinalIgnoreCase)))
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
        ContainsAny(text, "vacature", "vacatures", "banen", "jobs", "zoek", "search", "toon", "show", "vind", "find");

    private static bool LooksLikeKpi(string text) =>
        ContainsAny(text, "kpi", "statistiek", "metrics", "clicks", "impressies", "impressions", "prestatie", "dashboard");

    private static bool LooksLikeLeastClicks(string text) =>
        ContainsAny(text, "minste click", "fewest click", "minste klik", "laagste click", "least click", "weinigste click");

    private static bool LooksLikeTractionAdvice(string text) =>
        ContainsAny(text, "tractie", "traction", "weinig reactie", "weinig click", "verbeter", "improve", "waarom weinig", "why low");

    private static bool LooksLikeSalesDashboard(string text) =>
        ContainsAny(text, "dashboard", "account", "overzicht", "summary", "stand van");

    private static string? DetectWorkType(string text)
    {
        foreach (var label in WorkTypeLabels.All)
        {
            if (text.Contains(label, StringComparison.OrdinalIgnoreCase))
            {
                return label;
            }
        }

        // Common synonyms
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
