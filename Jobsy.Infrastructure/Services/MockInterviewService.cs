using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.RegularExpressions;
using Jobsy.Core.Enums;
using Jobsy.Core.Interfaces;
using Jobsy.Core.Options;
using Jobsy.Core.Rules;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Jobsy.Infrastructure.Services;

public sealed class MockInterviewService : IMockInterviewService
{
    public const int MaxHistoryMessages = 24;
    public const int MaxMessageChars = 2_000;

    private const string SystemPromptTemplate =
        """
        Je bent een warme, scherpe coach-recruiter in Nederland. Je helpt een jongere oefenen voor
        een sollicitatiegesprek via chat. Dit is géén echt gesprek en géén toezegging van werk.
        Doel: de kandidaat écht helpen — met gerichte vragen uit DEZE vacature en bruikbaar advies.

        Vacaturecontext:
        - Functie: {title}
        - Werkgever: {company}
        - Adres: {address}
        - Branches: {workTypes}
        - Startdatum: {startDate}
        - Vervoer: {transport}
        - Uurloon (alleen als zichtbaar/bekend): {wage}
        - Vacaturetekst:
        {description}

        Kernopdracht — vacature-eerst:
        - Haal 3–5 concrete taken, eisen of situaties uit de vacaturetekst (niet algemeen).
        - Stel vragen die daar letterlijk of duidelijk op aansluiten (noem een taak/eis uit de tekst).
        - Vermijd generieke vragen als “waarom wil je werken?” zonder koppeling aan deze rol.

        Antwoordstructuur NA elk kandidatenantwoord (verplicht, behalve bij de openingsbeurt):
        1) Regel die begint met "Sterk: " — wat goed ging, graag met een citaat of detail uit hun antwoord.
        2) Regel die begint met "Tip: " — één concrete verbetering voor een écht gesprek
           (bijv. STAR: situatie → actie → resultaat, of koppeling aan een vacaturetaak).
        3) Lege regel, daarna "Vraag: " + precies één volgende oefenvraag.

        Extra gedragsregels:
        1. Nederlands, natuurlijk, bemoedigend — alsof je écht meedenkt.
        2. Eén vraag per beurt; max ~140 woorden.
        3. Als het antwoord vaag/kort is: tip om een voorbeeld te geven, en stel eventueel een soft doorvraag
           als de "Vraag:" (nog over hetzelfde thema) i.p.v. meteen door naar een nieuw onderwerp.
        4. Geen harde toezeggingen over salaris, contract of aanname.
        5. Nooit BSN, bankgegevens, wachtwoorden of andere zeer gevoelige data vragen.
        6. Als gevraagd wordt of dit echt is: leg uit dat dit een oefengesprek via Jobsy is.
        7. Rond na 5–6 vragen af met een kort samenvattend compliment + 2 takeaways om te onthouden.
        """;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IIntegrationCredentialService _credentials;
    private readonly OpenAiOptions _options;
    private readonly ILogger<MockInterviewService> _logger;

    public MockInterviewService(
        IHttpClientFactory httpClientFactory,
        IIntegrationCredentialService credentials,
        IOptions<OpenAiOptions> options,
        ILogger<MockInterviewService> logger)
    {
        _httpClientFactory = httpClientFactory;
        _credentials = credentials;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<MockInterviewTurnResult> ContinueAsync(
        MockInterviewVacancyContext vacancy,
        IReadOnlyList<MockInterviewMessage> history,
        CancellationToken cancellationToken = default)
    {
        var sanitized = SanitizeHistory(history);
        var apiKey = await ResolveApiKeyAsync(cancellationToken);
        if (!string.IsNullOrWhiteSpace(apiKey))
        {
            try
            {
                var model = await ResolveModelAsync(cancellationToken);
                var baseUrl = await ResolveBaseUrlAsync(cancellationToken);
                var reply = await CompleteWithOpenAiAsync(vacancy, sanitized, apiKey, model, baseUrl, cancellationToken);
                if (!string.IsNullOrWhiteSpace(reply))
                {
                    return new MockInterviewTurnResult(reply.Trim(), UsedAi: true);
                }
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogWarning(ex, "Mock interview OpenAI-aanroep mislukt; scripted fallback wordt gebruikt.");
            }
        }

        return new MockInterviewTurnResult(ScriptedFallback.NextReply(vacancy, sanitized), UsedAi: false);
    }

    public static IReadOnlyList<MockInterviewMessage> SanitizeHistory(IReadOnlyList<MockInterviewMessage> history)
    {
        if (history.Count == 0)
        {
            return [];
        }

        var cleaned = new List<MockInterviewMessage>(Math.Min(history.Count, MaxHistoryMessages));
        foreach (var msg in history.TakeLast(MaxHistoryMessages))
        {
            var role = NormalizeRole(msg.Role);
            if (role is null)
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

            cleaned.Add(new MockInterviewMessage(role, content));
        }

        return cleaned;
    }

    private static string? NormalizeRole(string? role)
    {
        if (string.IsNullOrWhiteSpace(role))
        {
            return null;
        }

        return role.Trim().ToLowerInvariant() switch
        {
            "user" or "candidate" => "user",
            "assistant" or "recruiter" or "bot" => "assistant",
            _ => null
        };
    }

    private async Task<string?> CompleteWithOpenAiAsync(
        MockInterviewVacancyContext vacancy,
        IReadOnlyList<MockInterviewMessage> history,
        string apiKey,
        string model,
        string baseUrl,
        CancellationToken cancellationToken)
    {
        var system = BuildSystemPrompt(vacancy);
        var messages = new List<object>(history.Count + 2)
        {
            new { role = "system", content = system }
        };

        if (history.Count == 0)
        {
            messages.Add(new
            {
                role = "user",
                content =
                    "Start het oefengesprek. Stel je kort voor als recruiter van dit bedrijf, " +
                    "zeg dat dit een oefengesprek is waarin je hen helpt scherper te antwoorden, " +
                    "noem in één zin een concrete taak/eis uit de vacaturetekst, " +
                    "en stel je eerste gerichte vraag (begin met 'Vraag: ')."
            });
        }
        else
        {
            foreach (var turn in history)
            {
                messages.Add(new { role = turn.Role, content = turn.Content });
            }
        }

        var client = _httpClientFactory.CreateClient("IntegrationProbe");
        using var request = new HttpRequestMessage(
            HttpMethod.Post,
            new Uri(new Uri(baseUrl, UriKind.Absolute), "chat/completions"));
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
        request.Content = JsonContent.Create(new
        {
            model,
            temperature = 0.65,
            max_tokens = 550,
            messages
        });

        using var response = await client.SendAsync(request, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync(cancellationToken);
            _logger.LogWarning(
                "OpenAI mock interview gaf {StatusCode}: {Body}",
                (int)response.StatusCode,
                body.Length > 400 ? body[..400] : body);
            return null;
        }

        var completion = await response.Content.ReadFromJsonAsync<ChatCompletionResponse>(JsonOptions, cancellationToken);
        return completion?.Choices?.FirstOrDefault()?.Message?.Content;
    }

    private static string BuildSystemPrompt(MockInterviewVacancyContext vacancy)
    {
        var description = HtmlSanitize.ToPlainPreview(vacancy.Description, maxLength: 3_500);
        var transport = vacancy.RequiredTransport is { Count: > 0 }
            ? string.Join(", ", vacancy.RequiredTransport)
            : "niet gespecificeerd";
        var workTypes = vacancy.WorkTypes is { Count: > 0 }
            ? string.Join(", ", vacancy.WorkTypes)
            : "niet gespecificeerd";
        var wage = vacancy.HourlyWage is null
            ? "niet getoond"
            : $"€ {vacancy.HourlyWage.Value:0.00} per uur";

        return SystemPromptTemplate
            .Replace("{title}", vacancy.Title.Trim(), StringComparison.Ordinal)
            .Replace("{company}", vacancy.CompanyName.Trim(), StringComparison.Ordinal)
            .Replace("{address}", string.IsNullOrWhiteSpace(vacancy.CompanyAddress) ? "onbekend" : vacancy.CompanyAddress.Trim(), StringComparison.Ordinal)
            .Replace("{workTypes}", workTypes, StringComparison.Ordinal)
            .Replace("{startDate}", vacancy.StartDate.ToString("dd-MM-yyyy"), StringComparison.Ordinal)
            .Replace("{transport}", transport, StringComparison.Ordinal)
            .Replace("{wage}", wage, StringComparison.Ordinal)
            .Replace("{description}", description, StringComparison.Ordinal);
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
        if (!string.IsNullOrWhiteSpace(fromDb))
        {
            return fromDb;
        }

        return string.IsNullOrWhiteSpace(_options.Model) ? "gpt-4o-mini" : _options.Model.Trim();
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

        var fallback = string.IsNullOrWhiteSpace(_options.BaseUrl)
            ? "https://api.openai.com/v1/"
            : _options.BaseUrl;
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

    /// <summary>Vacancy-aware practice flow when OpenAI is unavailable.</summary>
    public static class ScriptedFallback
    {
        public static string NextReply(
            MockInterviewVacancyContext vacancy,
            IReadOnlyList<MockInterviewMessage> history)
        {
            var plan = InterviewPlan.FromVacancy(vacancy);
            var userTurns = history.Count(m => m.Role == "user");

            if (history.Count == 0)
            {
                return
                    $"Hoi! Ik help je oefenen voor {plan.Title} bij {plan.Company}. " +
                    "Dit is een oefengesprek via Jobsy — geen echt gesprek, wél met vragen uit de vacature.\n\n" +
                    $"In de tekst zie ik vooral: {plan.ThemeSummary}.\n\n" +
                    $"Vraag: {plan.Questions[0]}";
            }

            var lastUser = history.LastOrDefault(m => m.Role == "user")?.Content?.Trim() ?? "";
            var coaching = BuildCoaching(lastUser, plan, userTurns);

            if (userTurns >= plan.Questions.Count)
            {
                return
                    $"{coaching}\n\n" +
                    $"Mooi geoefend voor {plan.Title}! Dit was geen echte beoordeling.\n" +
                    $"Onthoud: (1) koppel je antwoord aan taken uit de vacature, zoals {plan.PrimaryHook}. " +
                    "(2) Gebruik een kort voorbeeld: situatie → wat jij deed → resultaat. Succes!";
            }

            return $"{coaching}\n\nVraag: {plan.Questions[userTurns]}";
        }

        private static string BuildCoaching(string answer, InterviewPlan plan, int userTurnIndex)
        {
            var strong = BuildStrong(answer, plan);
            var tip = BuildTip(answer, plan, userTurnIndex);
            return $"Sterk: {strong}\nTip: {tip}";
        }

        private static string BuildStrong(string answer, InterviewPlan plan)
        {
            if (answer.Length < 12)
            {
                return "Je bent in ieder geval begonnen — dat telt. Nu maken we het concreter.";
            }

            var matchedTheme = plan.Themes.FirstOrDefault(t =>
                answer.Contains(t.Keyword, StringComparison.OrdinalIgnoreCase));
            if (matchedTheme is not null)
            {
                return $"Je noemt iets over {matchedTheme.Label} — dat sluit aan bij wat {plan.Company} zoekt.";
            }

            if (ContainsAny(answer, "bijvoorbeeld", "toen", "vorige", "school", "stage", "werk", "ik heb", "ik deed"))
            {
                return "Je geeft al een stukje ervaring of voorbeeld. Dat klinkt geloofwaardiger dan alleen ‘ik wil graag’.";
            }

            if (answer.Length >= 80)
            {
                return "Je antwoord is inhoudelijk; je denkt na over de rol. Dat merkt een recruiter.";
            }

            return $"Duidelijk dat je reageert op {plan.Title}. Je hebt een basis waarop we kunnen bouwen.";
        }

        private static string BuildTip(string answer, InterviewPlan plan, int userTurnIndex)
        {
            if (answer.Length < 25)
            {
                return $"Maak het langer met één voorbeeld. Koppel het aan: {plan.PrimaryHook}.";
            }

            if (!ContainsAny(answer, "omdat", "daardoor", "bijvoorbeeld", "toen", "daarna", "resultaat", "geleerd"))
            {
                return "Voeg een mini-structuur toe: situatie → wat jij deed → wat het opleverde. Dat blijft beter hangen.";
            }

            if (userTurnIndex <= 1)
            {
                return $"Noem expliciet een taak uit de vacature (bijv. {plan.PrimaryHook}) en zeg hoe jij dat aanpakt.";
            }

            if (plan.Themes.Count > 1 && userTurnIndex < plan.Themes.Count)
            {
                var next = plan.Themes[Math.Min(userTurnIndex, plan.Themes.Count - 1)];
                return $"In een echt gesprek kun je ook {next.Label} noemen — dat staat centraal in deze vacature.";
            }

            return "Houd het in een live gesprek iets korter en eindig met waarom dit bij deze vacature past.";
        }

        private static bool ContainsAny(string text, params string[] needles)
            => needles.Any(n => text.Contains(n, StringComparison.OrdinalIgnoreCase));
    }

    internal sealed record InterviewTheme(string Keyword, string Label, string Question);

    internal sealed class InterviewPlan
    {
        public required string Title { get; init; }
        public required string Company { get; init; }
        public required string PrimaryHook { get; init; }
        public required string ThemeSummary { get; init; }
        public required IReadOnlyList<InterviewTheme> Themes { get; init; }
        public required IReadOnlyList<string> Questions { get; init; }

        public static InterviewPlan FromVacancy(MockInterviewVacancyContext vacancy)
        {
            var company = string.IsNullOrWhiteSpace(vacancy.CompanyName) ? "ons bedrijf" : vacancy.CompanyName.Trim();
            var title = string.IsNullOrWhiteSpace(vacancy.Title) ? "deze functie" : vacancy.Title.Trim();
            var plain = HtmlSanitize.ToPlainPreview(vacancy.Description, maxLength: 2_500);
            var workTypes = vacancy.WorkTypes ?? [];
            var transportModes = vacancy.RequiredTransport ?? [];
            var haystack = $"{title} {plain} {string.Join(' ', workTypes)}".ToLowerInvariant();
            var transport = transportModes.FirstOrDefault() ?? "fiets of OV";

            var themes = new List<InterviewTheme>();

            // Domain cues from typical Westland / youth job ads
            TryAddMatch(haystack, themes, "klant", "klantcontact",
                $"In de vacature komt klantcontact terug. Vertel hoe jij met een lastige of drukke klant omgaat — graag met een voorbeeld.");
            TryAddMatch(haystack, themes, "gast", "gasten/horeca",
                $"Er staat werk met gasten in de tekst. Hoe zorg jij dat gasten zich welkom voelen, ook als het druk is?");
            TryAddMatch(haystack, themes, "kassa", "kassa/afrekenen",
                $"Afrekenen of kassa komt terug. Hoe blijf jij vriendelijk én nauwkeurig als er een rij staat?");
            TryAddMatch(haystack, themes, "magazijn", "magazijnwerk",
                $"Magazijnwerk hoort bij de rol. Hoe ga jij om met tillen, tempo en netjes werken?");
            TryAddMatch(haystack, themes, "inpak", "inpakken/orderpick",
                $"Inpakken of orders verzamelen staat in de vacature. Hoe zorg jij dat je snel wérkt zonder fouten?");
            TryAddMatch(haystack, themes, "bezorg", "bezorgen",
                $"Bezorgen speelt een rol. Hoe plan jij een route en wat doe je als iets niet lukt (weer, adres, vertraging)?");
            TryAddMatch(haystack, themes, "schoonmaak", "schoonmaak",
                $"Schoonmaak staat in de tekst. Hoe controleer jij of iets écht schoon/klaar is?");
            TryAddMatch(haystack, themes, "team", "samenwerken",
                $"Samenwerken is belangrijk hier. Vertel over een moment waarop je een collega hielp of om hulp vroeg.");
            TryAddMatch(haystack, themes, "collega", "samenwerken",
                $"De vacature noemt collega’s. Hoe ga jij om met feedback van een leidinggevende of teamgenoot?");
            TryAddMatch(haystack, themes, "verantwoord", "verantwoordelijkheid",
                $"Er wordt verantwoordelijkheid gevraagd. Geef een voorbeeld waarin jij iets zelf oppakte zonder dat iemand erachteraan hoefde.");
            TryAddMatch(haystack, themes, "weekend", "beschikbaarheid in weekenden",
                $"Weekenden komen terug. Hoe ziet jouw beschikbaarheid eruit, en hoe combineer je dat met school/andere plannen?");
            TryAddMatch(haystack, themes, "avond", "avonddiensten",
                $"Avondwerk speelt mee. Past dat bij jou, en hoe zorg je dat je fit en op tijd bent?");
            TryAddMatch(haystack, themes, "flexibel", "flexibiliteit",
                $"Flexibiliteit staat in de vacature. Kun je een voorbeeld geven waarin je snel schakelde toen plannen veranderden?");
            TryAddMatch(haystack, themes, "tuinbouw", "tuinbouw/productie",
                $"Tuinbouw of productie hoort bij de rol. Hoe ga jij om met herhalend werk en tempo op de werkvloer?");
            TryAddMatch(haystack, themes, "kas", "werk in de kas",
                $"Werk in/rond de kas komt terug. Wat spreekt jou daarin aan, en hoe houd je het vol op een drukke dag?");

            foreach (var wt in workTypes)
            {
                var key = wt.Trim().ToLowerInvariant();
                if (key.Contains("horeca", StringComparison.Ordinal))
                {
                    AddTheme(themes, "horeca", "horeca",
                        $"Dit is een horecarol. Hoe blijf jij vriendelijk als het tegelijk druk én warm is achter de bar/in de zaak?");
                }
                else if (key.Contains("logistiek", StringComparison.Ordinal))
                {
                    AddTheme(themes, "logistiek", "logistiek",
                        $"Logistiek staat centraal. Hoe voorkom jij fouten bij tellen, labels of orders?");
                }
                else if (key.Contains("winkel", StringComparison.Ordinal) || key.Contains("retail", StringComparison.Ordinal))
                {
                    AddTheme(themes, "winkel", "winkelwerk",
                        $"Winkelwerk hoort erbij. Wat doe je als een klant iets zoekt dat je niet meteen kunt vinden?");
                }
            }

            // Snippet-based hook from description
            var snippet = ExtractDutySnippet(plain) ?? title.ToLowerInvariant();

            if (themes.Count == 0)
            {
                themes.Add(new InterviewTheme(
                    "motivatie",
                    "motivatie voor deze rol",
                    $"Waarom past {title} bij jou — noem iets uit de vacaturetekst dat je aanspreekt."));
            }

            while (themes.Count < 4)
            {
                themes.Add(themes.Count switch
                {
                    1 => new InterviewTheme(
                        "ervaring",
                        "ervaring of school",
                        $"Welke ervaring (school, sport, bijbaan, stage) helpt jou bij {snippet}? Geef één concreet voorbeeld."),
                    2 => new InterviewTheme(
                        "vervoer",
                        "reistijd/vervoer",
                        $"In de vacature past vervoer als {transport}. Hoe kom jij meestal naar het werk, en hoe betrouwbaar is dat?"),
                    3 => new InterviewTheme(
                        "drukte",
                        "werken onder druk",
                        $"Stel: het is druk rond {snippet} en iemand vraagt tegelijk iets anders. Wat doe je eerst, en waarom?"),
                    _ => new InterviewTheme(
                        "start",
                        "beschikbaarheid",
                        $"Wanneer kun je starten (rond {vacancy.StartDate:dd-MM}) en hoeveel uur per week past bij jou?")
                });
            }

            // Always end with candidate questions
            var questions = themes.Take(4).Select(t => t.Question).ToList();
            questions.Add(
                "Laatste oefenvraag: welke vraag zou jij zelf aan de werkgever stellen over deze vacature — en waarom is die vraag slim?");

            var primary = themes[0];
            var summary = string.Join(", ", themes.Take(3).Select(t => t.Label));

            return new InterviewPlan
            {
                Title = title,
                Company = company,
                PrimaryHook = snippet,
                ThemeSummary = summary,
                Themes = themes,
                Questions = questions
            };
        }

        private static void TryAddMatch(
            string haystack,
            List<InterviewTheme> themes,
            string keyword,
            string label,
            string question)
        {
            if (!haystack.Contains(keyword, StringComparison.Ordinal))
            {
                return;
            }

            AddTheme(themes, keyword, label, question);
        }

        private static void AddTheme(
            List<InterviewTheme> themes,
            string keyword,
            string label,
            string question)
        {
            if (themes.Any(t => t.Label.Equals(label, StringComparison.OrdinalIgnoreCase)
                                || t.Keyword.Equals(keyword, StringComparison.OrdinalIgnoreCase)))
            {
                return;
            }

            themes.Add(new InterviewTheme(keyword, label, question));
        }

        private static string? ExtractDutySnippet(string plain)
        {
            if (string.IsNullOrWhiteSpace(plain))
            {
                return null;
            }

            var sentences = Regex.Split(plain, @"(?<=[\.\!\?\n])\s+")
                .Select(s => s.Trim().TrimStart('-', '•', '*', ' '))
                .Where(s => s.Length is >= 28 and <= 140)
                .ToList();

            var duty = sentences.FirstOrDefault(s =>
                ContainsDutyVerb(s));
            if (!string.IsNullOrWhiteSpace(duty))
            {
                return TrimSnippet(duty);
            }

            var first = sentences.FirstOrDefault() ?? plain.Trim();
            return TrimSnippet(first);
        }

        private static bool ContainsDutyVerb(string s)
        {
            var lower = s.ToLowerInvariant();
            return lower.Contains("je ") || lower.Contains("jij ")
                || lower.Contains("zorgen") || lower.Contains("helpen")
                || lower.Contains("werken") || lower.Contains("verantwoordelijk")
                || lower.Contains("taken") || lower.Contains("opdracht");
        }

        private static string TrimSnippet(string value)
        {
            var cleaned = Regex.Replace(value, @"\s+", " ").Trim();
            if (cleaned.Length > 90)
            {
                cleaned = cleaned[..87].TrimEnd(',', '.', ';', ' ') + "…";
            }

            return cleaned.ToLowerInvariant();
        }
    }
}
