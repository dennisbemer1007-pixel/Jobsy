using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Jobsy.Core.Ai;
using Jobsy.Core.Authorization;
using Jobsy.Core.Diagnostics;
using Jobsy.Core.Entities;
using Jobsy.Core.Contracts;
using Jobsy.Core.Enums;
using Jobsy.Core.Features;
using Jobsy.Core.Interfaces;
using Jobsy.Core.Localization;
using Jobsy.Core.Rules;
using Jobsy.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Jobsy.Infrastructure.Services;

public sealed class AssistantChatService : IAssistantChatService
{
    public const int MaxHistoryMessages = 20;
    public const int MaxMessageChars = 1_500;

    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    private static readonly string[] SearchStopwords =
[
    "ik", "zoek", "zoeken", "een", "de", "het", "vacature", "vacatures", "baan", "banen",
        "job", "jobs", "als", "voor", "naar", "op", "kaart", "toon", "tonen", "vind", "vinden",
        "show", "find", "search", "looking", "want", "wil", "graag", "bij", "met", "van",
        "in", "mijn", "me", "kan", "je", "jij", "mij", "please", "for", "the", "a", "an", "and",
        "or", "is", "zijn", "daar", "hier", "lobsy", "jobsy", "open", "openen", "link", "doorlink",
        "buurt", "omgeving", "regio", "plaats", "stad", "dichtbij", "nearby", "area", "alle", "all",
        "iets", "passends", "graag", "heb", "bent", "ben",
        // Travel / proximity noise — these become map filters, not keyword search.
        "max", "binnen", "within", "min", "minuut", "minuten", "minute", "minutes",
        "lopen", "lopend", "loopafstand", "vandaan", "reistijd", "travel", "walking", "walk",
        "fiets", "fietsen", "bike", "cycling", "auto", "car", "rijden", "driving",
        "ov", "tram", "bus", "metro", "transit", "voet"
];

    private readonly JobsyDbContext _db;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IMetricsQueryService _metrics;
    private readonly ICandidateMetricsQueryService _candidateMetrics;
    private readonly ISalesManagerDashboardService _salesDashboard;
    private readonly IOpenAiEndpointResolver _openAi;
    private readonly IFeatureFlags _featureFlags;
    private readonly ILogger<AssistantChatService> _logger;
    private readonly IPlatformErrorLog? _platformLog;

    public AssistantChatService(
        JobsyDbContext db,
        IHttpClientFactory httpClientFactory,
        IMetricsQueryService metrics,
        ICandidateMetricsQueryService candidateMetrics,
        ISalesManagerDashboardService salesDashboard,
        IOpenAiEndpointResolver openAi,
        IFeatureFlags featureFlags,
        ILogger<AssistantChatService> logger,
        IPlatformErrorLog? platformLog = null)
    {
        _db = db;
        _httpClientFactory = httpClientFactory;
        _metrics = metrics;
        _candidateMetrics = candidateMetrics;
        _salesDashboard = salesDashboard;
        _openAi = openAi;
        _featureFlags = featureFlags;
        _logger = logger;
        _platformLog = platformLog;
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
            var employersOn = await _featureFlags.IsEnabledAsync(PlatformFeature.Employers, cancellationToken);
            return new AssistantChatResult(Greeting(context, employersOn), UsedAi: false, []);
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

        var endpoint = await _openAi.ResolveAsync(OpenAiFeature.AssistantChat, cancellationToken);
        if (endpoint.Unavailable)
        {
            return new AssistantChatResult(AiUnavailableCopy.For(context.Language), UsedAi: false, []);
        }

        var apiKey = endpoint.ApiKey;
        if (!string.IsNullOrWhiteSpace(apiKey))
        {
            try
            {
                using var aiCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                aiCts.CancelAfter(TimeSpan.FromSeconds(25));
                var (ai, rejectedFacts) = await CompleteWithOpenAiAsync(
                    context, sanitized, apiKey, endpoint.Model, endpoint.BaseUrl, aiCts.Token);
                if (!string.IsNullOrWhiteSpace(ai))
                {
                    return new AssistantChatResult(StripMarkup(ai), UsedAi: true, []);
                }

                if (rejectedFacts && string.Equals(context.Role, JobsyRoles.Candidate, StringComparison.Ordinal))
                {
                    return new AssistantChatResult(UnknownFactReply(context), UsedAi: false, []);
                }
            }
            catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
            {
                _logger.LogWarning("Assistant reply timed out; local fallback is used.");
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogWarning(ex, "Assistant OpenAI call failed; falling back.");
            }
        }

        var employersStillOn = await _featureFlags.IsEnabledAsync(PlatformFeature.Employers, cancellationToken);
        return new AssistantChatResult(FallbackHelp(context, employersStillOn), UsedAi: false, []);
    }

    private async Task<AssistantChatResult?> TryScriptedAsync(
        AssistantChatContext context,
        string lastUser,
        CancellationToken cancellationToken)
    {
        var text = lastUser.ToLowerInvariant();

        if (string.Equals(context.Role, JobsyRoles.Candidate, StringComparison.Ordinal))
        {
            var employersOn = await _featureFlags.IsEnabledAsync(PlatformFeature.Employers, cancellationToken);
            if (LooksLikeHowLobsy(text))
            {
                var lang = JobsyLanguages.Normalize(context.Language);
                var reply = employersOn
                    ? lang switch
                    {
                        "en" => "As a candidate on Lobsy you: (1) browse the job map, (2) complete your profile, (3) like/share jobs, (4) apply, (5) track applications, (6) wait for the employer. I can open the how-to page for you.",
                        "pl" => "Jako kandydat w Lobsy: (1) przeglądasz mapę ofert, (2) uzupełniasz profil, (3) lubisz/udostępniasz oferty, (4) aplikujesz, (5) śledzisz status, (6) czekasz na pracodawcę. Mogę otworzyć stronę z instrukcją.",
                        "ro" => "Ca și candidat pe Lobsy: (1) vezi harta joburilor, (2) completezi profilul, (3) like/share, (4) aplici, (5) urmărești statusul, (6) aștepți angajatorul. Pot deschide ghidul.",
                        "ar" => "كمترشح على Lobsy: (1) تستعرض خريطة الوظائف، (2) تكمل ملفك، (3) تعجب/تشارك، (4) تتقدم، (5) تتابع الطلبات، (6) تنتظر صاحب العمل. يمكنني فتح صفحة الشرح.",
                        _ => "Als kandidaat op Lobsy: (1) bekijk de banenkaart, (2) vul je profiel, (3) like/deel vacatures, (4) solliciteer, (5) volg je sollicitaties, (6) wacht op de werkgever. Ik kan de uitlegpagina openen."
                    }
                    : lang switch
                    {
                        "en" => "As a candidate on Lobsy you work on your passport, tests and career plan. Jobs and applications come later. Nothing goes to an employer.",
                        "pl" => "Jako kandydat w Lobsy pracujesz nad paszportem, testami i planem kariery. Oferty i aplikacje pojawią się później. Nic nie trafia do pracodawcy.",
                        "ro" => "Ca și candidat pe Lobsy lucrezi la pașaport, teste și planul de carieră. Joburile și candidaturile vin mai târziu. Nimic nu ajunge la un angajator.",
                        "ar" => "كمترشح على Lobsy تعمل على جوازك واختباراتك وخطة مسارك. الوظائف والطلبات تأتي لاحقاً. لا شيء يذهب إلى صاحب عمل.",
                        _ => "Als kandidaat op Lobsy werk je aan je paspoort, tests en loopbaanplan. Banen en sollicitaties komen later. Niets gaat naar een werkgever."
                    };
                return new AssistantChatResult(
                    reply,
                    false,
                    [new AssistantChatAction(AssistantActionTypes.Navigate, Url: "/candidate/hoe-werkt-lobsy", Label: "Hoe werkt Lobsy")]);
            }

            if (LooksLikeDiplomaQuestion(text))
            {
                return await CandidateDiplomaAsync(context, cancellationToken);
            }

            if (LooksLikeDreamQuestion(text))
            {
                return await CandidateDreamAsync(context, cancellationToken);
            }

            if (LooksLikePassportHelp(text))
            {
                return await CandidatePassportHelpAsync(context, cancellationToken);
            }

            if (!employersOn && (LooksLikeApplicationStatus(text) || IsVacancySearchIntent(text, DetectWorkType(text), ExtractJobSearchQuery(lastUser, DetectWorkType(text)))))
            {
                var lang = JobsyLanguages.Normalize(context.Language);
                var soon = lang switch
                {
                    "en" => "Jobs and applications come later. Your passport, tests and career plan stay yours.",
                    "pl" => "Oferty i aplikacje pojawią się później. Twój paszport, testy i plan kariery zostają twoje.",
                    "ro" => "Joburile și candidaturile vin mai târziu. Pașaportul, testele și planul rămân ale tale.",
                    "ar" => "الوظائف والطلبات تأتي لاحقاً. جوازك واختباراتك وخطة مسارك تبقى لك.",
                    _ => "Banen en sollicitaties komen later. Je paspoort, tests en loopbaanplan blijven van jou."
                };
                return new AssistantChatResult(
                    soon,
                    false,
                    [new AssistantChatAction(AssistantActionTypes.Navigate, Url: "/candidate/paspoort", Label: "Mijn paspoort")]);
            }

            if (LooksLikeApplicationStatus(text))
            {
                return await CandidateApplicationsAsync(context, cancellationToken);
            }

            if (LooksLikeCandidateProfile(text) || LooksLikeCandidateStats(text))
            {
                return await CandidateProfileAsync(context, DetectPeriod(text), cancellationToken);
            }

            var workType = DetectWorkType(text);
            var maxTravelMinutes = DetectMaxTravelMinutes(text);
            var transport = DetectTransport(text);
            var jobQuery = ExtractJobSearchQuery(lastUser, workType);
            if (employersOn
                && (IsVacancySearchIntent(text, workType, jobQuery)
                    || (workType is not null && (maxTravelMinutes is not null || transport is not null))))
            {
                return await CandidateVacancySearchAsync(
                    context, workType, jobQuery, maxTravelMinutes, transport, cancellationToken);
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
        int? maxTravelMinutes,
        string? transport,
        CancellationToken cancellationToken)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var query = _db.Vacancies.AsNoTracking()
            .Where(v => v.Status == VacancyStatus.Active && v.StartDate <= today && v.EndDate >= today);

        var tokens = VacancyTextSearch.GetRequiredTokens(searchQuery);
        foreach (var token in tokens)
        {
            // Prefilter must stay looser than VacancyTextSearch: compound job titles
            // like "heftruckchauffeur" also match titles containing the root "heftruck".
            // Company matching uses the same public-display rules as the banenkaart
            // (masked end-client names are not searchable).
            var t = token;
            var root = TryJobRoot(token);
            if (root is null)
            {
                query = query.Where(v =>
                    v.Title.ToLower().Contains(t)
                    || v.Description.ToLower().Contains(t)
                    || (v.WorkTypeLabels != null && v.WorkTypeLabels.ToLower().Contains(t))
                    || (v.RequiredDrivingLicense != null && v.RequiredDrivingLicense.ToLower().Contains(t))
                    || (v.RequiredEducation != null && v.RequiredEducation.ToLower().Contains(t))
                    || ((v.IntermediaryCompanyId == null || v.ShowClientAddressOnMap)
                        && v.Company.Name.ToLower().Contains(t))
                    || (v.IntermediaryCompany != null && v.IntermediaryCompany.Name.ToLower().Contains(t)));
            }
            else
            {
                var r = root;
                query = query.Where(v =>
                    v.Title.ToLower().Contains(t)
                    || v.Title.ToLower().Contains(r)
                    || v.Description.ToLower().Contains(t)
                    || v.Description.ToLower().Contains(r)
                    || (v.WorkTypeLabels != null && (v.WorkTypeLabels.ToLower().Contains(t) || v.WorkTypeLabels.ToLower().Contains(r)))
                    || (v.RequiredDrivingLicense != null && (v.RequiredDrivingLicense.ToLower().Contains(t) || v.RequiredDrivingLicense.ToLower().Contains(r)))
                    || (v.RequiredEducation != null && (v.RequiredEducation.ToLower().Contains(t) || v.RequiredEducation.ToLower().Contains(r)))
                    || ((v.IntermediaryCompanyId == null || v.ShowClientAddressOnMap)
                        && (v.Company.Name.ToLower().Contains(t) || v.Company.Name.ToLower().Contains(r)))
                    || (v.IntermediaryCompany != null
                        && (v.IntermediaryCompany.Name.ToLower().Contains(t) || v.IntermediaryCompany.Name.ToLower().Contains(r))));
            }
        }

        // Cap DB payload; refine with exact VacancyTextSearch (suffix roots) in memory.
        var loadDescription = tokens.Count > 0;
        var all = await query
            .OrderBy(v => v.Title)
            .Take(500)
            .Select(v => new VacancySearchRow(
                v.Id,
                v.Title,
                loadDescription ? v.Description : string.Empty,
                // Public display name (masks end client when ShowClientAddressOnMap is false).
                v.IntermediaryCompanyId != null && !v.ShowClientAddressOnMap && v.IntermediaryCompany != null
                    ? v.IntermediaryCompany.Name
                    : v.Company.Name,
                // When open kaart, intermediary is also public via "Aangeboden door …".
                v.IntermediaryCompanyId != null && v.ShowClientAddressOnMap && v.IntermediaryCompany != null
                    ? v.IntermediaryCompany.Name
                    : null,
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
                v.Title,
                v.Description,
                v.WorkTypeLabels,
                v.RequiredDrivingLicense,
                v.RequiredEducation,
                searchQuery,
                companyName: v.Company,
                intermediaryName: v.ExtraPublicCompanyName))
            .ToList();

        var count = matched.Count;
        var matches = matched.Take(8).ToList();

        var lang = JobsyLanguages.Normalize(context.Language);
        var filterSummary = BuildFilterSummary(workType, searchQuery, maxTravelMinutes, transport, lang);
        var hasTravelFilters = maxTravelMinutes is not null || !string.IsNullOrWhiteSpace(transport);

        var sb = new StringBuilder();
        if (count == 0 && !string.IsNullOrWhiteSpace(searchQuery))
        {
            sb.Append(lang switch
            {
                "en" => $"I couldn’t find vacancies for “{searchQuery}”. Try another job title or sector.",
                _ => $"Ik kon geen vacatures vinden voor “{searchQuery}”. Probeer een andere functienaam of branche."
            });
        }
        else if (hasTravelFilters)
        {
            sb.AppendLine(lang switch
            {
                "en" => $"I’ve set the filters to {filterSummary}. Vacancies within your travel time appear on the job map (set your location if it isn’t active yet).",
                _ => $"Ik zet de filters op {filterSummary}. Vacatures binnen jouw reistijd verschijnen op de banenkaart (deel je locatie als die nog niet actief is)."
            });

            if (count > 0 && string.IsNullOrWhiteSpace(searchQuery))
            {
                sb.AppendLine(lang switch
                {
                    "en" => $"There are {count} vacancies in this sector overall — the map applies your travel filter.",
                    _ => $"Er zijn {count} vacatures in deze branche — de kaart past jouw reistijdfilter toe."
                });
            }

            foreach (var m in matches.Take(5))
            {
                sb.AppendLine($"• {m.Title} — {m.Company}");
            }
        }
        else if (count == 0)
        {
            sb.Append(lang switch
            {
                "en" => $"I couldn’t find vacancies for “{filterSummary}”. Try another job title or sector.",
                _ => $"Ik kon geen vacatures vinden voor “{filterSummary}”. Probeer een andere functienaam of branche."
            });
        }
        else
        {
            sb.AppendLine(lang switch
            {
                "en" => $"I found {count} vacancies for {filterSummary}. Showing them on the job map.",
                _ => $"Ik heb {count} vacatures gevonden voor {filterSummary}. Ik toon ze op de banenkaart."
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

        var url = BuildMapFilterUrl(workType, searchQuery, maxTravelMinutes, transport);
        var actions = new List<AssistantChatAction>
        {
            new(AssistantActionTypes.SetFilters, Url: url, WorkType: workType, SearchQuery: searchQuery, Count: count,
                Label: lang == "en" ? "Show on map" : "Toon op kaart",
                MaxTravelMinutes: maxTravelMinutes, Transport: transport),
            new(AssistantActionTypes.Navigate, Url: url, Label: lang == "en" ? "Job map" : "Banenkaart",
                MaxTravelMinutes: maxTravelMinutes, Transport: transport)
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

    private static readonly string[] JobSuffixesForPrefilter =
    [
        "chauffeur", "medewerker", "hulp", "assistent", "operator", "picker", "plukker",
        "driver", "worker", "helper", "assistant"
    ];

    private static string? TryJobRoot(string token)
    {
        foreach (var suffix in JobSuffixesForPrefilter)
        {
            if (token.EndsWith(suffix, StringComparison.Ordinal) && token.Length > suffix.Length + 2)
            {
                var root = token[..^suffix.Length];
                if (root.Length >= 3)
                {
                    return root;
                }
            }
        }

        return null;
    }

    private sealed record VacancySearchRow(
        Guid Id,
        string Title,
        string Description,
        string Company,
        string? ExtraPublicCompanyName,
        WorkType WorkTypes,
        string? WorkTypeLabels,
        string? RequiredDrivingLicense,
        string? RequiredEducation);

    private static string BuildMapFilterUrl(
        string? workType,
        string? searchQuery,
        int? maxTravelMinutes = null,
        string? transport = null)
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

        if (maxTravelMinutes is int minutes)
        {
            parts.Add($"maxMinutes={minutes}");
        }

        if (!string.IsNullOrWhiteSpace(transport))
        {
            parts.Add($"transport={Uri.EscapeDataString(transport)}");
        }

        return parts.Count == 0 ? "/" : "/?" + string.Join("&", parts);
    }

    private static string BuildFilterSummary(
        string? workType,
        string? searchQuery,
        int? maxTravelMinutes,
        string? transport,
        string lang)
    {
        var parts = new List<string>();
        if (!string.IsNullOrWhiteSpace(searchQuery))
        {
            parts.Add($"“{searchQuery}”");
        }
        else if (!string.IsNullOrWhiteSpace(workType))
        {
            parts.Add(workType);
        }
        else
        {
            parts.Add(lang == "en" ? "all sectors" : "alle branches");
        }

        if (maxTravelMinutes is int minutes)
        {
            parts.Add(lang == "en" ? $"max {minutes} min" : $"max {minutes} min");
        }

        if (!string.IsNullOrWhiteSpace(transport))
        {
            parts.Add(transport switch
            {
                TransportLabels.Walking => lang == "en" ? "walking" : "lopen",
                TransportLabels.Bike => lang == "en" ? "bike" : "fiets",
                TransportLabels.Car => lang == "en" ? "car" : "auto",
                TransportLabels.PublicTransport => lang == "en" ? "transit" : "OV",
                _ => transport
            });
        }

        return string.Join(", ", parts);
    }

    private async Task<AssistantChatResult> CandidateApplicationsAsync(
        AssistantChatContext context,
        CancellationToken cancellationToken)
    {
        var apps = await _db.Applications.AsNoTracking()
            .Include(a => a.Vacancy).ThenInclude(v => v.Company)
            .Where(a => a.CandidateUserId == context.UserId && a.EmailVerifiedAt != null)
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

    private async Task<AssistantChatResult> CandidateProfileAsync(
        AssistantChatContext context,
        string period,
        CancellationToken cancellationToken)
    {
        var user = await _db.Users.AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == context.UserId, cancellationToken);
        var metrics = await _candidateMetrics.GetSummaryAsync(context.UserId, period, cancellationToken);
        var apps = await _db.Applications.AsNoTracking()
            .Include(a => a.Vacancy).ThenInclude(v => v.Company)
            .Where(a => a.CandidateUserId == context.UserId)
            .OrderByDescending(a => a.CreatedAt)
            .Take(5)
            .ToListAsync(cancellationToken);

        var lang = JobsyLanguages.Normalize(context.Language);
        var periodLabel = PeriodLabel(period, lang);
        var sb = new StringBuilder();
        if (lang == "en")
        {
            sb.AppendLine("Within your candidate profile:");
            if (user is not null)
            {
                sb.AppendLine($"• Name: {user.FullName}");
                sb.AppendLine($"• Open for work: {(user.OpenForWork ? "yes" : "no")}");
            }

            sb.AppendLine($"Activity ({periodLabel}):");
        }
        else
        {
            sb.AppendLine("Binnen jouw kandidatenprofiel:");
            if (user is not null)
            {
                sb.AppendLine($"• Naam: {user.FullName}");
                sb.AppendLine($"• Open for work: {(user.OpenForWork ? "ja" : "nee")}");
            }

            sb.AppendLine($"Activiteit ({periodLabel}):");
        }

        foreach (var m in metrics)
        {
            sb.AppendLine($"• {m.Label}: {m.Value}");
        }

        if (apps.Count > 0)
        {
            sb.AppendLine(lang == "en" ? "Recent applications:" : "Recente sollicitaties:");
            foreach (var a in apps)
            {
                sb.AppendLine($"• {a.Vacancy.Title} — {a.Vacancy.Company.Name}: {a.Status}");
            }
        }
        else
        {
            sb.AppendLine(lang == "en"
                ? "You have no applications yet."
                : "Je hebt nog geen sollicitaties.");
        }

        return new AssistantChatResult(
            sb.ToString().Trim(),
            false,
            [
                new AssistantChatAction(AssistantActionTypes.Navigate, Url: "/home", Label: "Dashboard"),
                new AssistantChatAction(AssistantActionTypes.Navigate, Url: "/candidate/applications",
                    Label: lang == "en" ? "My applications" : "Mijn sollicitaties"),
                new AssistantChatAction(AssistantActionTypes.Navigate, Url: "/candidate/profile",
                    Label: lang == "en" ? "My profile" : "Mijn profiel")
            ]);
    }

    private async Task<AssistantChatResult> CandidatePassportHelpAsync(
        AssistantChatContext context,
        CancellationToken cancellationToken)
    {
        var competency = await _db.CandidateCompetencies.AsNoTracking()
            .FirstOrDefaultAsync(c => c.UserId == context.UserId, cancellationToken);
        var values = await _db.CandidateValuesProfiles.AsNoTracking()
            .FirstOrDefaultAsync(v => v.UserId == context.UserId, cancellationToken);
        var career = await _db.CandidateCareerInterests.AsNoTracking()
            .FirstOrDefaultAsync(c => c.UserId == context.UserId, cancellationToken);
        var dream = await _db.CandidateCareerPlans.AsNoTracking()
            .Where(p => p.UserId == context.UserId)
            .Select(p => p.DreamTitle)
            .FirstOrDefaultAsync(cancellationToken);

        var lang = JobsyLanguages.Normalize(context.Language);
        var en = lang == "en";
        var sb = new StringBuilder();
        var anyFact = false;
        sb.AppendLine(en ? "From your passport and tests:" : "Vanuit je paspoort en tests:");

        if (competency is not null && CandidateCompetencyStatuses.IsCompleted(competency.Status))
        {
            var scores = new CompetencyScores(
                competency.SamenwerkenPercent,
                competency.ResultaatgerichtheidPercent,
                competency.StressbestendigheidPercent,
                competency.InnovatiePercent,
                competency.ExtraversiePercent);
            var top = OnboardingImpressionComposer.TopCompetencyItems(scores, 1);
            if (top.Count > 0)
            {
                anyFact = true;
                sb.AppendLine(en
                    ? $"• Strongest competency: {top[0].Label}"
                    : $"• Sterkste competentie: {top[0].Label}");
            }
        }

        var riasec = CareerTestCatalog.CompletedScoresOrNull(
            career?.Status,
            career?.RealisticPercent,
            career?.InvestigativePercent,
            career?.ArtisticPercent,
            career?.SocialPercent,
            career?.EnterprisingPercent,
            career?.ConventionalPercent);
        if (riasec is { IsComplete: true })
        {
            var rankedRiasec = RiasecRanking.Rank(
                riasec.Realistic, riasec.Investigative, riasec.Artistic,
                riasec.Social, riasec.Enterprising, riasec.Conventional);
            if (rankedRiasec.Count > 0)
            {
                anyFact = true;
                var topCode = rankedRiasec[0].Code;
                sb.AppendLine(en
                    ? $"• Career direction: {CareerCompassBuilder.TypeLabel(topCode)}"
                    : $"• Beroepsrichting: {CareerCompassBuilder.TypeLabel(topCode)}");
            }
        }

        if (values is not null && CandidateCompetencyStatuses.IsCompleted(values.Status))
        {
            var ranked = DimensionRanking.Rank(
                [
                    (SchwartzValuesCatalog.Autonomy, values.AutonomyPercent),
                    (SchwartzValuesCatalog.Connection, values.ConnectionPercent),
                    (SchwartzValuesCatalog.Achievement, values.AchievementPercent),
                    (SchwartzValuesCatalog.Stability, values.StabilityPercent),
                    (SchwartzValuesCatalog.Impact, values.ImpactPercent)
                ],
                DimensionRanking.ValueTieBreak);
            if (ranked.Count > 0)
            {
                anyFact = true;
                sb.AppendLine(en
                    ? $"• Strongest value: {DimensionLabels.For(ranked[0].Code)}"
                    : $"• Sterkste waarde: {DimensionLabels.For(ranked[0].Code)}");
            }
        }

        if (!string.IsNullOrWhiteSpace(dream))
        {
            anyFact = true;
            sb.AppendLine(en ? $"• Dream: {dream.Trim()}" : $"• Droom: {dream.Trim()}");
        }

        if (!anyFact)
        {
            sb.AppendLine(en
                ? "I don’t see a finished test yet. Finish the free tests and I can name your strengths."
                : "Ik zie nog geen afgeronde test. Rond de gratis tests af, dan noem ik je sterke punten.");
        }

        sb.AppendLine(en
            ? "Jobs and applications stay off until employers are on. Nothing goes to an employer."
            : "Banen en sollicitaties blijven uit tot werkgevers aan staan. Niets gaat naar een werkgever.");

        return new AssistantChatResult(
            sb.ToString().Trim(),
            false,
            [
                new AssistantChatAction(AssistantActionTypes.Navigate, Url: "/candidate/paspoort", Label: en ? "My passport" : "Mijn paspoort"),
                new AssistantChatAction(AssistantActionTypes.Navigate, Url: "/carriere", Label: en ? "Career plan" : "Loopbaanplan")
            ]);
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
            [new AssistantChatAction(AssistantActionTypes.Navigate, Url: "/admin/gebruikers/sales", Label: "Salesmanagers")]);
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

    private async Task<(string? Reply, bool RejectedFacts)> CompleteWithOpenAiAsync(
        AssistantChatContext context,
        IReadOnlyList<AssistantChatMessage> history,
        string apiKey,
        string model,
        string baseUrl,
        CancellationToken cancellationToken)
    {
        var lang = MockInterviewLabels.For(context.Language);
        var (facts, sheet) = await BuildScopedFactsAsync(context, cancellationToken);
        var system = BuildSystemPrompt(context, lang.LanguageName, facts);
        var prompts = sheet is null
            ? new[] { system }
            : new[] { system, system + "\n" + CandidateFactGuard.StrictAddendum };
        string? rejected = null;
        for (var attempt = 1; attempt <= prompts.Length; attempt++)
        {
            var reply = await RequestAssistantReplyAsync(
                history,
                prompts[attempt - 1],
                apiKey,
                model,
                baseUrl,
                cancellationToken);
            if (string.IsNullOrWhiteSpace(reply))
            {
                continue;
            }

            if (!AssistantReplyGuard.Accepts(reply, facts))
            {
                rejected = "coach-rules";
            }
            else if (sheet is not null && CandidateFactGuard.RejectionReason(reply, sheet) is string reason)
            {
                rejected = reason;
            }
            else
            {
                return (reply, false);
            }

            await AiFactRejectionLog.WriteAsync(
                _platformLog,
                _logger,
                "coach",
                rejected,
                attempt,
                cancellationToken);
        }

        return (null, rejected is not null);
    }

    private async Task<string?> RequestAssistantReplyAsync(
        IReadOnlyList<AssistantChatMessage> history,
        string system,
        string apiKey,
        string model,
        string baseUrl,
        CancellationToken cancellationToken)
    {
        var messages = new List<object> { new { role = "system", content = system } };
        foreach (var turn in history.TakeLast(MaxHistoryMessages))
        {
            messages.Add(new { role = turn.Role, content = turn.Content });
        }

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

    private async Task<(string Facts, CandidateFactSheet? Sheet)> BuildScopedFactsAsync(
        AssistantChatContext context,
        CancellationToken cancellationToken)
    {
        try
        {
            if (string.Equals(context.Role, JobsyRoles.Candidate, StringComparison.Ordinal))
            {
                var employersOn = await _featureFlags.IsEnabledAsync(PlatformFeature.Employers, cancellationToken);
                var stats = await _candidateMetrics.GetSummaryAsync(context.UserId, "month", cancellationToken);
                var user = await _db.Users.AsNoTracking()
                    .FirstOrDefaultAsync(u => u.Id == context.UserId, cancellationToken);
                var prefs = user is null ? null : ParseAssistantPreferences(user.PreferencesJson);
                var dream = await _db.CandidateCareerPlans.AsNoTracking()
                    .Where(p => p.UserId == context.UserId)
                    .Select(p => p.DreamTitle)
                    .FirstOrDefaultAsync(cancellationToken);
                var sb = new StringBuilder();
                sb.Append(employersOn
                    ? "Candidate facts (own profile only): "
                    : "Candidate facts (own profile only; employers OFF, do not suggest vacancies, applications or employer contact): ");
                if (!employersOn)
                {
                    sb.Append("jobs and applications are hidden; ");
                }
                else
                {
                    var apps = await _db.Applications.AsNoTracking()
                        .CountAsync(a => a.CandidateUserId == context.UserId, cancellationToken);
                    sb.Append($"total applications={apps}; ");
                }

                var scoreLines = new List<string>();
                var jobs = new List<string>();
                AppendPreferenceFacts(sb, prefs, dream);
                await AppendTestFactsAsync(sb, context.UserId, context.Language, scoreLines, jobs, cancellationToken);
                sb.Append("month metrics: ");
                sb.Append(string.Join("; ", stats.Select(m => $"{m.Key}={m.Value}")));
                var work = WorkEntries(prefs);
                var education = (prefs?.Educations ?? [])
                    .Where(item => !string.IsNullOrWhiteSpace(item))
                    .Select(item => item.Trim())
                    .ToList();
                if (!string.IsNullOrWhiteSpace(prefs?.EducationDirection))
                {
                    education.Add(prefs.EducationDirection.Trim());
                }

                var certificates = (prefs?.Certificates ?? [])
                    .Where(item => !string.IsNullOrWhiteSpace(item.Name))
                    .Select(CandidateFactSheet.FormatCertificate)
                    .ToList();
                var confirmed = new List<string>();
                if (!string.IsNullOrWhiteSpace(dream))
                {
                    confirmed.Add(dream.Trim());
                    jobs.Add(dream.Trim());
                }

                if (!string.IsNullOrWhiteSpace(prefs?.PreferredTransport))
                {
                    confirmed.Add(prefs.PreferredTransport.Trim());
                }

                foreach (var occupation in CareerCompassBuilder.Occupations)
                {
                    if (!jobs.Exists(job => string.Equals(job, occupation.Title, StringComparison.OrdinalIgnoreCase)))
                    {
                        jobs.Add(occupation.Title);
                    }
                }

                var sheet = CandidateFactSheet.Personal(work, education, certificates, jobs, scoreLines, confirmed);
                sb.AppendLine();
                sb.Append(sheet.ToPrompt());
                return (sb.ToString(), sheet);
            }

            if (string.Equals(context.Role, JobsyRoles.SalesManager, StringComparison.Ordinal))
            {
                var dash = await _salesDashboard.GetDashboardAsync(context.UserId, cancellationToken);
                if (dash is null)
                {
                    return ("Salesmanager facts: onboarding incomplete / no dashboard.", null);
                }

                return ($"Salesmanager facts (own account only): tracking={dash.TrackingCode}; uninvoicedExVat={dash.UninvoicedExVat}; outstandingIssuedExVat={dash.OutstandingIssuedExVat}; suppliers={dash.Suppliers.Count}", null);
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

                return (sb.ToString(), null);
            }
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogDebug(ex, "Failed to build assistant scoped facts.");
        }

        return ("No extra facts.", null);
    }

    private static List<string> WorkEntries(CandidatePreferencesDto? prefs)
        => (prefs?.Employers ?? [])
            .Select(CandidateFactSheet.FormatWorkEntry)
            .Where(role => !string.IsNullOrWhiteSpace(role))
            .Cast<string>()
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Take(4)
            .ToList();

    private async Task AppendTestFactsAsync(
        StringBuilder sb,
        Guid userId,
        string? language,
        List<string> scoreLines,
        List<string> jobs,
        CancellationToken cancellationToken)
    {
        var lang = JobsyLanguages.Normalize(language);
        var completed = new List<string>();
        var competency = await _db.CandidateCompetencies.AsNoTracking()
            .FirstOrDefaultAsync(c => c.UserId == userId, cancellationToken);
        if (competency is not null && CandidateCompetencyStatuses.IsCompleted(competency.Status))
        {
            completed.Add(TestName(lang, "work"));
            var top = new (string Label, int Score)[]
                {
                    (DimensionLabels.For(CompetencyTestCatalog.Samenwerken, lang), competency.SamenwerkenPercent ?? 0),
                    (DimensionLabels.For(CompetencyTestCatalog.Resultaatgerichtheid, lang), competency.ResultaatgerichtheidPercent ?? 0),
                    (DimensionLabels.For(CompetencyTestCatalog.Stressbestendigheid, lang), competency.StressbestendigheidPercent ?? 0),
                    (DimensionLabels.For(CompetencyTestCatalog.Innovatie, lang), competency.InnovatiePercent ?? 0),
                    (DimensionLabels.For(CompetencyTestCatalog.Extraversie, lang), competency.ExtraversiePercent ?? 0)
                }
                .Where(x => x.Label.Length > 0)
                .OrderByDescending(x => x.Score)
                .ThenBy(x => x.Label, StringComparer.OrdinalIgnoreCase)
                .Take(3);
            var line = string.Join(", ", top.Select(x => $"{x.Label} {x.Score}%"));
            sb.Append("competenceTop3=").Append(line).Append("; ");
            scoreLines.Add("competenceTop3=" + line);
        }

        var career = await _db.CandidateCareerInterests.AsNoTracking()
            .FirstOrDefaultAsync(c => c.UserId == userId, cancellationToken);
        var riasec = CareerTestCatalog.CompletedScoresOrNull(
            career?.Status,
            career?.RealisticPercent,
            career?.InvestigativePercent,
            career?.ArtisticPercent,
            career?.SocialPercent,
            career?.EnterprisingPercent,
            career?.ConventionalPercent);
        if (riasec is { IsComplete: true })
        {
            completed.Add(TestName(lang, "career"));
            var top = CareerTestCatalog.RiasecCodes
                .Select(code => (Label: DimensionLabels.For(code, lang), Score: riasec.Get(code)))
                .OrderByDescending(x => x.Score)
                .ThenBy(x => x.Label, StringComparer.OrdinalIgnoreCase)
                .Take(3);
            var line = string.Join(", ", top.Select(x => $"{x.Label} {x.Score}%"));
            sb.Append("directionsTop3=").Append(line).Append("; ");
            scoreLines.Add("directionsTop3=" + line);
        }

        var culture = await _db.CandidateCulturePersonalityProfiles.AsNoTracking()
            .FirstOrDefaultAsync(c => c.UserId == userId, cancellationToken);
        if (culture is not null && CandidateCompetencyStatuses.IsCompleted(culture.Status))
        {
            completed.Add(TestName(lang, "culture"));
            var topCulture = CulturePersonalityCatalog.CultureDimensionCodes
                .Select(code => (Label: DimensionLabels.For(code, lang), Score: ReadCultureScore(culture, code)))
                .OrderByDescending(x => x.Score)
                .ThenBy(x => x.Label, StringComparer.OrdinalIgnoreCase)
                .First();
            sb.Append($"cultureTop={topCulture.Label} {topCulture.Score}%; ");
            scoreLines.Add($"cultureTop={topCulture.Label} {topCulture.Score}%");
        }

        var values = await _db.CandidateValuesProfiles.AsNoTracking()
            .FirstOrDefaultAsync(v => v.UserId == userId, cancellationToken);
        if (values is not null && CandidateCompetencyStatuses.IsCompleted(values.Status))
        {
            completed.Add(TestName(lang, "values"));
            var topValue = SchwartzValuesCatalog.CategoryCodes
                .Select(code => (Label: DimensionLabels.For(code, lang), Score: ReadValueScore(values, code)))
                .OrderByDescending(x => x.Score)
                .ThenBy(x => x.Label, StringComparer.OrdinalIgnoreCase)
                .First();
            sb.Append($"valuesTop={topValue.Label} {topValue.Score}%; ");
            scoreLines.Add($"valuesTop={topValue.Label} {topValue.Score}%");
        }

        var deep = await _db.CandidateDeepAnalyses.AsNoTracking()
            .Where(d => d.UserId == userId && d.Status == CandidateDeepAnalysisStatuses.Completed)
            .Select(d => d.Kind)
            .ToListAsync(cancellationToken);
        var completedLine = completed.Count == 0 ? "none" : string.Join(", ", completed);
        sb.Append("completedTests=").Append(completedLine).Append("; ");
        scoreLines.Add("completedTests=" + completedLine);
        var deepLine = deep.Count == 0
            ? "deepTests=none (no extra long test; this does not mean completedTests is empty)"
            : "deepTests=" + string.Join(",", deep.Select(kind => DeepTestName(lang, kind)));
        sb.Append(deepLine).Append("; ");
        scoreLines.Add(deepLine);

        var compass = CareerCompassJson.TryDeserialize(career?.CompassJson);
        if (compass is { HasOccupations: true })
        {
            var occupations = compass.AllOccupations
                .OrderByDescending(m => m.Percent)
                .ThenBy(m => m.Title, StringComparer.OrdinalIgnoreCase)
                .Take(CareerCompassSanitize.MaxCatalogueJobs);
            var listed = occupations.ToList();
            sb.Append("topOccupations=")
                .Append(string.Join(", ", listed.Select(m => $"{OccupationTitles.ForChat(m.Title, lang)} {m.Percent}%")))
                .Append("; ");
            jobs.AddRange(listed.Select(m => m.Title));
        }
    }

    private static string TestName(string lang, string key) => (lang, key) switch
    {
        ("en", "work") => "How you work",
        ("en", "career") => "Job test",
        ("en", "culture") => "Culture and personality",
        ("en", "values") => "Values at work",
        ("pl", "work") => "Jak pracujesz",
        ("pl", "career") => "Test zawodów",
        ("pl", "culture") => "Kultura i osobowość",
        ("pl", "values") => "Wartości w pracy",
        ("ro", "work") => "Cum lucrezi",
        ("ro", "career") => "Testul de meserii",
        ("ro", "culture") => "Cultură și personalitate",
        ("ro", "values") => "Valori la muncă",
        ("ar", "work") => "كيف تعمل",
        ("ar", "career") => "اختبار المهن",
        ("ar", "culture") => "الثقافة والشخصية",
        ("ar", "values") => "قيم في العمل",
        (_, "work") => "Test: hoe je werkt",
        (_, "career") => "Beroepentest",
        (_, "culture") => "Cultuur en persoonlijkheid",
        _ => "Waarden op werk"
    };

    private static string DeepTestName(string lang, AssessmentKind kind)
    {
        var extra = lang switch
        {
            "en" => "long",
            "pl" => "długi",
            "ro" => "lung",
            "ar" => "مطوّل",
            _ => "uitgebreid"
        };
        var name = kind switch
        {
            AssessmentKind.Career => TestName(lang, "career"),
            AssessmentKind.Culture => TestName(lang, "culture"),
            AssessmentKind.Values => TestName(lang, "values"),
            _ => TestName(lang, "work")
        };
        return extra + " " + name;
    }

    private static int ReadCultureScore(CandidateCulturePersonalityProfile row, string code)
        => code switch
        {
            _ when string.Equals(code, CulturePersonalityCatalog.Autonomy, StringComparison.OrdinalIgnoreCase) => row.AutonomyPercent ?? 0,
            _ when string.Equals(code, CulturePersonalityCatalog.Informal, StringComparison.OrdinalIgnoreCase) => row.InformalPercent ?? 0,
            _ when string.Equals(code, CulturePersonalityCatalog.Collaboration, StringComparison.OrdinalIgnoreCase) => row.CollaborationPercent ?? 0,
            _ when string.Equals(code, CulturePersonalityCatalog.Flexibility, StringComparison.OrdinalIgnoreCase) => row.FlexibilityPercent ?? 0,
            _ when string.Equals(code, CulturePersonalityCatalog.Innovation, StringComparison.OrdinalIgnoreCase) => row.InnovationPercent ?? 0,
            _ when string.Equals(code, CulturePersonalityCatalog.PeopleFirst, StringComparison.OrdinalIgnoreCase) => row.PeopleFirstPercent ?? 0,
            _ => 0
        };

    private static int ReadValueScore(CandidateValuesProfile row, string code)
        => code switch
        {
            _ when string.Equals(code, SchwartzValuesCatalog.Autonomy, StringComparison.OrdinalIgnoreCase) => row.AutonomyPercent ?? 0,
            _ when string.Equals(code, SchwartzValuesCatalog.Connection, StringComparison.OrdinalIgnoreCase) => row.ConnectionPercent ?? 0,
            _ when string.Equals(code, SchwartzValuesCatalog.Achievement, StringComparison.OrdinalIgnoreCase) => row.AchievementPercent ?? 0,
            _ when string.Equals(code, SchwartzValuesCatalog.Stability, StringComparison.OrdinalIgnoreCase) => row.StabilityPercent ?? 0,
            _ when string.Equals(code, SchwartzValuesCatalog.Impact, StringComparison.OrdinalIgnoreCase) => row.ImpactPercent ?? 0,
            _ => 0
        };

    private static void AppendPreferenceFacts(StringBuilder sb, CandidatePreferencesDto? prefs, string? dream)
    {
        if (!string.IsNullOrWhiteSpace(dream))
        {
            sb.Append($"dream={dream.Trim()}; ");
        }

        if (prefs is null)
        {
            return;
        }

        var city = HomeAddressCity.From(prefs.HomeAddress);
        if (!string.IsNullOrWhiteSpace(city))
        {
            sb.Append($"city={city}; ");
        }

        if (!string.IsNullOrWhiteSpace(prefs.PreferredTransport))
        {
            sb.Append($"transport={prefs.PreferredTransport.Trim()}; ");
        }

        if (prefs.MaxTravelMinutes is int minutes)
        {
            sb.Append($"maxTravelMinutes={minutes}; ");
        }

        if (prefs.Educations is { Count: > 0 })
        {
            sb.Append($"education={string.Join(", ", prefs.Educations)}; ");
        }

        if (!string.IsNullOrWhiteSpace(prefs.EducationDirection))
        {
            sb.Append($"direction={prefs.EducationDirection.Trim()}; ");
        }

        var roles = (prefs.Employers ?? [])
            .Select(CandidateFactSheet.FormatWorkEntry)
            .Where(r => !string.IsNullOrWhiteSpace(r))
            .Cast<string>();
        var experience = string.Join(", ", roles);
        sb.Append(experience.Length > 0 ? $"experience={experience}; " : "experience=none; ");
        if (prefs.Certificates is { Count: > 0 })
        {
            var names = prefs.Certificates
                .Where(c => !string.IsNullOrWhiteSpace(c.Name))
                .Select(CandidateFactSheet.FormatCertificate);
            var listed = string.Join(", ", names);
            if (listed.Length > 0)
            {
                sb.Append($"certificates={listed}; ");
            }
        }
        else
        {
            sb.Append("certificates=none; ");
        }
    }

    private static CandidatePreferencesDto? ParseAssistantPreferences(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return null;
        }

        try
        {
            return JsonSerializer.Deserialize<CandidatePreferencesDto>(json, JsonOptions);
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private static string BuildSystemPrompt(AssistantChatContext context, string languageName, string facts)
    {
        var role = context.Role;
        var scope = role switch
        {
            JobsyRoles.Candidate =>
                "You help a JOBSEEKER on Lobsy only: answer from their own profile (preferences, experience, education, dream job, tests, career plan). " +
                "If the scoped facts say employers are OFF, do not mention vacancies, applications, likes or employer contact, and do not claim you lack their preferences when the facts include them. " +
                "Never invent vacancies. Never access other users’ data. Never send the candidate's data to an employer.",
            JobsyRoles.SalesManager =>
                "You help a SALESMANAGER on Lobsy only: their referrals, commissions, invoices, onboarding, tracking code. Stay inside their account. No candidate personal NAW data of third parties.",
            JobsyRoles.Admin =>
                "You help an ADMIN on Lobsy only: platform KPIs, site visits, salesmanager activity, vacancy performance. NEVER reveal candidate NAW (name/address/email/phone/BSN). Refuse topics outside Lobsy.",
            _ =>
                "You help an EMPLOYER/MANAGER on Lobsy only: KPIs for their companies, vacancy performance, traction tips, active vacancies in scope. NEVER reveal candidate NAW. Never answer about other companies outside their access."
        };

        return
            $"You are Lobsy, a helpful lobster assistant. Reply in {languageName}, plain B1. " +
            "Plain text only: no markdown, no **, no __, no # headings, and no lines that start with a dash. Maximum 5 short sentences. " +
            $"{scope} " +
            "Answer using ONLY the scoped facts below and general Lobsy product knowledge. " +
            "The Feitenlijst is the only source for this person's tests, work, education and certificates. " +
            "Never mention an employer, sector, year, diploma or work experience that is not on that list. " +
            "If it says werkervaring: geen or experience=none, say nothing about past work. " +
            "If a personal fact is missing, say you do not know. Do not guess. " +
            "Never say tests are missing when the facts list completed tests, scores or occupations. " +
            "Zeg niet wat de persoon leuk vindt (dieren, planten, koken…) tenzij het in de feiten staat; leg een beroep alleen uit met de scores. " +
            "You may compare catalogue jobs that are not on the candidate's own list. Explain them with the scores. Do not say you do not know those jobs. " +
            "Zeg niet dat een opleiding is afgerond tenzij dat in de feiten staat. " +
            "completedTests is the list of finished tests. deepTests=none means there is no extra long test, not that the person did nothing. " +
            "Never use the words Riasec, RIASEC, Career-test, Holland or werksterkte. Use only labels that appear in the facts. " +
            "If the user asks something outside Lobsy or outside their role permissions, politely refuse. " +
            $"Scoped facts:\n{facts}";
    }

    private static string Greeting(AssistantChatContext context, bool employersOn = true)
    {
        var lang = JobsyLanguages.Normalize(context.Language);
        return context.Role switch
        {
            JobsyRoles.Candidate when !employersOn => lang == "en"
                ? "Hi! I’m Lobsy. Ask me about your passport, tests or career plan. Jobs and applications come later."
                : "Hoi! Ik ben Lobsy. Vraag me naar je paspoort, tests of loopbaanplan. Banen en sollicitaties komen later.",
            JobsyRoles.Candidate => lang == "en"
                ? "Hi! I’m Lobsy. Ask me anything in your profile (applications, likes), or search vacancies by keyword (e.g. chauffeur)."
                : "Hoi! Ik ben Lobsy. Stel me elke vraag binnen jouw profiel (sollicitaties, likes), of zoek vacatures op trefwoord (bijv. chauffeur).",
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

    private static string UnknownFactReply(AssistantChatContext context)
    {
        var lang = JobsyLanguages.Normalize(context.Language);
        return InLang(lang,
            "Dat staat niet in je gegevens. Ik weet het niet en vul het niet in.",
            "That is not in your details. I do not know and I will not guess.",
            "Tego nie ma w twoich danych. Nie wiem i nie zgaduję.",
            "Asta nu este în datele tale. Nu știu și nu ghicesc.",
            "هذا ليس في بياناتك. لا أعرف ولن أخمن.");
    }

    private static string FallbackHelp(AssistantChatContext context, bool employersOn = true)
    {
        var lang = JobsyLanguages.Normalize(context.Language);
        return context.Role switch
        {
            JobsyRoles.Candidate when !employersOn => InLang(lang,
                "Ik leg uit hoe Lobsy werkt en antwoord vanuit je profiel: voorkeuren, ervaring, opleiding en je loopbaanplan. Banen komen later.",
                "I can explain how Lobsy works and answer from your profile: preferences, experience, education and your career plan. Jobs come later.",
                "Wyjaśniam, jak działa Lobsy, i odpowiadam z twojego profilu: preferencje, doświadczenie, nauka i plan kariery. Oferty przyjdą później.",
                "Îți explic cum funcționează Lobsy și răspund din profilul tău: preferințe, experiență, studii și planul de carieră. Joburile vin mai târziu.",
                "أشرح كيف يعمل لوبسي وأجيب من ملفك: تفضيلاتك وخبرتك ودراستك وخطة مسارك. الوظائف تأتي لاحقاً."),
            JobsyRoles.Candidate => InLang(lang,
                "Ik kan vacatures zoeken op trefwoord (bijv. chauffeur), uitleggen hoe Lobsy werkt, of vragen beantwoorden over jouw profiel en sollicitaties.",
                "I can search vacancies by keyword (for example chauffeur), explain how Lobsy works, or answer questions about your profile and applications.",
                "Mogę szukać ofert po słowie (na przykład kierowca), wyjaśnić jak działa Lobsy albo odpowiedzieć o twoim profilu i aplikacjach.",
                "Pot căuta joburi după cuvânt (de exemplu șofer), explica cum funcționează Lobsy sau răspunde despre profilul și candidaturile tale.",
                "يمكنني البحث عن وظائف بكلمة، أو شرح لوبسي، أو الإجابة عن ملفك وطلباتك."),
            JobsyRoles.SalesManager => InLang(lang,
                "Probeer te vragen naar je commissies, doorverwezen leveranciers of facturen.",
                "Try asking about your commissions, referred suppliers, or invoices.",
                "Zapytaj o prowizje, poleconych dostawców albo faktury.",
                "Întreabă despre comisioane, furnizorii recomandați sau facturi.",
                "اسأل عن عمولاتك أو الموردين المحالين أو الفواتير."),
            JobsyRoles.Admin => InLang(lang,
                "Probeer te vragen hoe vaak Lobsy vandaag is bezocht, welke salesmanager het meest actief is, of om een KPI-overzicht.",
                "Try asking how often Lobsy was visited today, which sales manager is most active, or for a KPI overview.",
                "Zapytaj, jak często dziś odwiedzano Lobsy, który opiekun sprzedaży jest najaktywniejszy, albo o przegląd KPI.",
                "Întreabă cât de des a fost vizitat Lobsy azi, care manager de vânzări e cel mai activ, sau un rezumat KPI.",
                "اسأل كم مرة زار الناس لوبسي اليوم، أو أي مدير مبيعات أنشط، أو ملخص المؤشرات."),
            _ => InLang(lang,
                "Probeer te vragen naar je KPI-overzicht, de vacature met de minste of meeste clicks, actieve vacatures, of tips bij weinig tractie.",
                "Try asking for your KPI overview, the vacancy with the fewest or most clicks, active vacancies, or tips for low traction.",
                "Zapytaj o przegląd KPI, ofertę z najmniejszą lub największą liczbą kliknięć, aktywne oferty albo wskazówki przy małym ruchu.",
                "Întreabă despre rezumatul KPI, anunțul cu cele mai puține sau cele mai multe clicuri, anunțurile active sau sfaturi când e puțină tracțiune.",
                "اسأل عن ملخص المؤشرات، أو الإعلان الأقل أو الأكثر نقراً، أو الإعلانات النشطة، أو نصائح عند قلة التفاعل.")
        };
    }

    private static string RefuseMessage(AssistantChatContext context)
    {
        var lang = JobsyLanguages.Normalize(context.Language);
        return InLang(lang,
            "Ik help alleen met Lobsy-onderwerpen binnen jouw rol. Vraag gerust naar je profiel, tests of je account.",
            "I can only help with Lobsy topics within your role. Ask about your profile, tests, KPIs, or your account.",
            "Pomagam tylko w tematach Lobsy w twojej roli. Pytaj o profil, testy, KPI albo konto.",
            "Ajut doar cu subiecte Lobsy din rolul tău. Întreabă despre profil, teste, KPI sau cont.",
            "أساعد فقط في مواضيع لوبسي ضمن دورك. اسأل عن ملفك أو اختباراتك أو المؤشرات أو حسابك.");
    }

    private static string InLang(string lang, string nl, string en, string pl, string ro, string ar)
        => lang switch
        {
            "en" => en,
            "pl" => pl,
            "ro" => ro,
            "ar" => ar,
            _ => nl
        };

    /// <summary>Mistral often returns markdown. The bubble shows plain text.</summary>
    public static string StripMarkup(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return "";
        }

        var stripped = text.Replace("**", "", StringComparison.Ordinal).Replace("__", "", StringComparison.Ordinal);
        stripped = Regex.Replace(stripped, @"(?m)^#{1,6}\s*", "");
        stripped = Regex.Replace(stripped, @"(?m)^-\s+", "");
        return stripped.Trim();
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
        ContainsAny(text, "hoe werkt", "how does lobsy", "how lobsy", "uitleg", "how to use");

    private static bool LooksLikeApplicationStatus(string text) =>
        ContainsAny(text, "sollicitatie", "application status", "mijn sollicitat", "status van mijn", "my application");

    /// <summary>
    /// Vacancy search only when the user clearly asks for jobs — not every sentence with "zoek".
    /// </summary>
    private static bool IsVacancySearchIntent(string text, string? workType, string? jobQuery)
    {
        // Advice such as "dichter bij een baan in de zorg" or "which jobs suit me"
        // is not a vacancy search. Require a search verb or the word vacature.
        var explicitVacancy = ContainsAny(text, "vacature", "vacatures");
        var searchVerb = ContainsAny(text, "zoek", "search", "toon", "show", "vind", "find");
        if (!explicitVacancy && !searchVerb)
        {
            return false;
        }

        if (explicitVacancy && searchVerb)
        {
            return true;
        }

        var jobWord = !string.IsNullOrWhiteSpace(jobQuery)
                      || ContainsAny(text, "heftruck", "reachtruck", "chauffeur", "orderpicker", "barista", "plukker",
                          "magazijnmedewerker", "kasmedewerker", "baan", "banen", "job", "jobs", "werk");
        return jobWord || workType is not null;
    }

    private static bool LooksLikeDiplomaQuestion(string text) =>
        ContainsAny(text,
            "diploma", "welk diploma", "welke opleiding", "which diploma", "what diploma", "my diploma",
            "dyplom", "jaki dyplom", "diplomă", "ce diplomă", "شهادة", "ما شهادتي");

    private static bool LooksLikeDreamQuestion(string text) =>
        ContainsAny(text,
            "droombaan", "mijn droom", "dream job", "my dream", "wymarzon", "jobul visat", "وظيفة الأحلام", "وظيفة أحلام");

    private async Task<AssistantChatResult> CandidateDiplomaAsync(
        AssistantChatContext context,
        CancellationToken cancellationToken)
    {
        var user = await _db.Users.AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == context.UserId, cancellationToken);
        var prefs = user is null ? null : ParseAssistantPreferences(user.PreferencesJson);
        var education = (prefs?.Educations ?? [])
            .Where(item => !string.IsNullOrWhiteSpace(item))
            .Select(item => item.Trim())
            .ToList();
        var lang = JobsyLanguages.Normalize(context.Language);
        if (education.Count == 0)
        {
            var missing = lang switch
            {
                "en" => "That is not in your details.",
                "pl" => "Tego nie ma w twoich danych.",
                "ro" => "Asta nu este în datele tale.",
                "ar" => "هذا ليس في بياناتك.",
                _ => "Dat staat niet in je gegevens."
            };
            return new AssistantChatResult(missing, false, []);
        }

        var level = string.Join(", ", education);
        var reply = lang switch
        {
            "en" => $"You have {level}",
            "pl" => $"Masz {level}",
            "ro" => $"Ai {level}",
            "ar" => $"لديك {level}",
            _ => $"Je hebt {level}"
        };
        return new AssistantChatResult(reply, false, []);
    }

    private async Task<AssistantChatResult> CandidateDreamAsync(
        AssistantChatContext context,
        CancellationToken cancellationToken)
    {
        var dream = await _db.CandidateCareerPlans.AsNoTracking()
            .Where(p => p.UserId == context.UserId)
            .Select(p => p.DreamTitle)
            .FirstOrDefaultAsync(cancellationToken);
        var lang = JobsyLanguages.Normalize(context.Language);
        if (string.IsNullOrWhiteSpace(dream))
        {
            var empty = lang switch
            {
                "en" => "You have not chosen a dream job yet.",
                "pl" => "Nie wybrałeś jeszcze wymarzonej pracy.",
                "ro" => "Nu ai ales încă un job de vis.",
                "ar" => "لم تختر بعد وظيفة الأحلام.",
                _ => "Je hebt nog geen droombaan gekozen"
            };
            return new AssistantChatResult(
                empty,
                false,
                [new AssistantChatAction(AssistantActionTypes.Navigate, Url: "/carriere", Label: lang == "en" ? "Career" : "Carrière")]);
        }

        var title = OccupationTitles.ForChat(dream.Trim(), lang);
        var reply = lang switch
        {
            "en" => $"Your dream job is {title}.",
            "pl" => $"Twoja wymarzona praca to {title}.",
            "ro" => $"Jobul tău de vis este {title}.",
            "ar" => $"وظيفة أحلامك هي {title}.",
            _ => $"Je droombaan is {title}."
        };
        return new AssistantChatResult(
            reply,
            false,
            [new AssistantChatAction(AssistantActionTypes.Navigate, Url: "/carriere", Label: lang == "en" ? "Career" : "Carrière")]);
    }

    private static bool LooksLikePassportHelp(string text) =>
        ContainsAny(text,
            "sterke punt", "sterktes", "strength", "competentie", "vaardigheid",
            "volgens mijn test", "mijn test", "mijn tests", "waarden", "droombaan", "droom",
            "loopbaan", "carriere", "carrière", "paspoort", "passport",
            "wat kun je", "wat kan je", "wat kan lobsy", "what can you",
            "voor mij doen", "wie ben ik", "who am i");

    private static bool LooksLikeCandidateProfile(string text) =>
        ContainsAny(text,
            "mijn profiel", "my profile", "binnen mijn", "in mijn account", "over mij", "about me",
            "open for work", "mijn gegevens", "my details", "wat staat er in mijn", "alles over mijn");

    private static bool LooksLikeCandidateStats(string text) =>
        ContainsAny(text,
            "hoe vaak heb ik", "mijn likes", "mijn shares", "mijn statistiek", "my stats",
            "mijn activiteit", "how many likes", "mijn overzicht", "mijn dashboard");

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
    /// Pull a job-title style query from free text (e.g. "chauffeur" from
    /// "Ik zoek vacatures voor een chauffeur"), excluding branch labels and travel noise.
    /// </summary>
    public static string? ExtractJobSearchQuery(string raw, string? detectedWorkType)
    {
        var cleaned = StripTravelAndProximityNoise(raw);
        var drop = new List<string>(WorkTypeLabels.All);
        if (!string.IsNullOrWhiteSpace(detectedWorkType))
        {
            drop.Add(detectedWorkType);
        }

        drop.AddRange(
        [
            "horeca", "hospitality", "logistiek", "warehouse", "magazijn", "retail", "winkel", "shop",
            "tuinbouw", "zorg", "care", "healthcare", "kantoor", "office", "bouw", "construction",
            "schoonmaak", "cleaning", "productie", "production", "fabriek", "cafe", "restaurant", "bar"
        ]);

        return VacancyTextSearch.ExtractSearchPhrase(cleaned, SearchStopwords, drop);
    }

    /// <summary>Remove travel/proximity phrases so they don't become toxic ?q= keywords.</summary>
    public static string StripTravelAndProximityNoise(string raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
        {
            return raw;
        }

        var s = Regex.Replace(
            raw,
            @"\b(?:max|binnen|within)?\s*\d{1,3}\s*(?:min(?:uut|uten?)?|minutes?)\b",
            " ",
            RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
        s = Regex.Replace(
            s,
            @"\b(?:te\s+voet|lopen|lopend|loopafstand|vandaan|reistijd|walking|walk|fiets(?:en)?|bike|cycling|auto|car|rijden|driving|ov|tram|bus|metro|transit)\b",
            " ",
            RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
        return Regex.Replace(s, @"\s{2,}", " ").Trim();
    }

    public static int? DetectMaxTravelMinutes(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return null;
        }

        var match = Regex.Match(
            text,
            @"\b(?:max(?:imaal)?|binnen|within|tot)?\s*(\d{1,3})\s*(?:min(?:uut|uten?)?|minutes?)\b",
            RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
        if (!match.Success)
        {
            match = Regex.Match(
                text,
                @"\b(\d{1,3})\s*(?:min(?:uut|uten?)?|minutes?)\b",
                RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
        }

        if (!match.Success || !int.TryParse(match.Groups[1].Value, out var minutes))
        {
            return null;
        }

        return Math.Clamp(minutes, 5, 90);
    }

    public static string? DetectTransport(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return null;
        }

        // Prefer the most specific mode mentioned; walking phrases beat generic "rijden".
        if (ContainsAny(text, "lopen", "lopend", "te voet", "walking", "walk", "loopafstand"))
        {
            return TransportLabels.Walking;
        }

        if (ContainsAny(text, "fiets", "fietsen", "bike", "cycling"))
        {
            return TransportLabels.Bike;
        }

        if (Regex.IsMatch(text, @"\b(?:ov|tram|bus|metro|transit|openbaar)\b", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant))
        {
            return TransportLabels.PublicTransport;
        }

        if (ContainsAny(text, "auto", "car", "rijden", "driving"))
        {
            return TransportLabels.Car;
        }

        return null;
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
