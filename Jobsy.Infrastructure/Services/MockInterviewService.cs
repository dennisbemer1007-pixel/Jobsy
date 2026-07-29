using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.RegularExpressions;
using Jobsy.Core.Enums;
using Jobsy.Core.Interfaces;
using Jobsy.Core.Localization;
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
        You are a warm, practical coach-recruiter helping a young person practice a job interview in chat.
        This is NOT a real interview and NOT a job offer. React to WHAT THEY SAY — no generic fluff.

        LANGUAGE (mandatory): Reply entirely in {languageName}. Every label and sentence must be in {languageName}.

        Vacancy context:
        - Role: {title}
        - Employer: {company}
        - Address: {address}
        - Sectors: {workTypes}
        - Start date: {startDate}
        - Transport: {transport}
        - Hourly wage (only if known): {wage}
        - Vacancy text:
        {description}

        Core task — vacancy-first + interactive:
        - Pull 3–5 concrete tasks/requirements from the vacancy text.
        - React to details from their last answer (quote or paraphrase). No generic praise.
        - Vary tips: sometimes STAR, tone, vacancy task link, or a rewrite example.
        - If the answer is vague/short: stay on the same theme with a soft follow-up as "{questionLabel} ".

        Reply structure AFTER each candidate answer (required, except the opening turn):
        1) Optional line "{cautionLabel} " — ONLY for insulting/rude language. Friendly, no shaming.
        2) Line "{strongLabel} " — what went well, with a short quote from THEIR answer.
        3) Line "{tipLabel} " — one concrete improvement.
        4) Optional line "{rewriteLabel} " — one rewritten example sentence (max ~35 words).
        5) Blank line, then "{questionLabel} " + exactly one next practice question.

        Extra rules:
        1. Natural, encouraging {languageName} — as if you are thinking with them.
        2. One question per turn; max ~160 words.
        3. Do not repeat the same tip two turns in a row.
        4. No hard promises about salary, contract, or hiring.
        5. Never ask for SSN/BSN, bank details, passwords, or other highly sensitive data.
        6. If asked whether this is real: explain it is a Lobsy practice chat.
        7. After 5–6 questions, wrap up with a compliment + 2 takeaways + one "use this tomorrow" line.
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
        string? language = null,
        CancellationToken cancellationToken = default)
    {
        var lang = JobsyLanguages.Normalize(language);
        var labels = Jobsy.Core.Localization.MockInterviewLabels.For(lang);
        var sanitized = SanitizeHistory(history);
        var apiKey = await ResolveApiKeyAsync(cancellationToken);
        if (!string.IsNullOrWhiteSpace(apiKey))
        {
            try
            {
                var model = await ResolveModelAsync(cancellationToken);
                var baseUrl = await ResolveBaseUrlAsync(cancellationToken);
                var reply = await CompleteWithOpenAiAsync(
                    vacancy, sanitized, labels, apiKey, model, baseUrl, cancellationToken);
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

        return new MockInterviewTurnResult(
            ScriptedFallback.NextReply(vacancy, sanitized, labels),
            UsedAi: false);
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
        MockInterviewLabels.Pack labels,
        string apiKey,
        string model,
        string baseUrl,
        CancellationToken cancellationToken)
    {
        var system = BuildSystemPrompt(vacancy, labels);
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
                    $"Start the practice interview in {labels.LanguageName}. " +
                    "Briefly introduce yourself as the coach-recruiter for this company, " +
                    "say this is a practice chat where you help them answer more sharply, " +
                    "mention in one sentence a concrete task/requirement from the vacancy text, " +
                    $"and ask your first focused question (start with '{labels.Question} '). " +
                    $"No {labels.Strong.TrimEnd(':')}/{labels.Tip.TrimEnd(':')} in this opening turn."
            });
        }
        else
        {
            foreach (var turn in history)
            {
                messages.Add(new { role = turn.Role, content = turn.Content });
            }

            var lastUser = history.LastOrDefault(m => m.Role == "user")?.Content ?? "";
            messages.Add(new
            {
                role = "user",
                content =
                    $"Reply now as coach in {labels.LanguageName} to my last answer above. " +
                    "Quote or paraphrase something from my text. " +
                    (DutchInterviewAnswerHeuristics.LooksInsulting(lastUser)
                        ? $"My tone may have been too sharp: start with '{labels.Caution} ' (friendly, no shaming), " +
                          $"then {labels.Tip.TrimEnd(':')} + '{labels.Rewrite} ' with a polite rewrite, and ask the same practice question again. "
                        : $"Use {labels.Strong.TrimEnd(':')} + {labels.Tip.TrimEnd(':')}, and add '{labels.Rewrite} ' if my answer was vague or short. ") +
                    $"End with exactly one '{labels.Question} '."
            });
        }

        var client = _httpClientFactory.CreateClient("IntegrationProbe");
        using var request = new HttpRequestMessage(
            HttpMethod.Post,
            new Uri(new Uri(baseUrl, UriKind.Absolute), "chat/completions"));
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
        request.Content = JsonContent.Create(new
        {
            model,
            temperature = 0.85,
            max_tokens = 650,
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

    private static string BuildSystemPrompt(MockInterviewVacancyContext vacancy, MockInterviewLabels.Pack labels)
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

        static string Safe(string? value)
            => (value ?? string.Empty).Replace("{", "{{", StringComparison.Ordinal).Replace("}", "}}", StringComparison.Ordinal);

        return SystemPromptTemplate
            .Replace("{languageName}", labels.LanguageName, StringComparison.Ordinal)
            .Replace("{cautionLabel}", labels.Caution, StringComparison.Ordinal)
            .Replace("{strongLabel}", labels.Strong, StringComparison.Ordinal)
            .Replace("{tipLabel}", labels.Tip, StringComparison.Ordinal)
            .Replace("{rewriteLabel}", labels.Rewrite, StringComparison.Ordinal)
            .Replace("{questionLabel}", labels.Question, StringComparison.Ordinal)
            .Replace("{title}", Safe(vacancy.Title.Trim()), StringComparison.Ordinal)
            .Replace("{company}", Safe(vacancy.CompanyName.Trim()), StringComparison.Ordinal)
            .Replace("{address}", Safe(string.IsNullOrWhiteSpace(vacancy.CompanyAddress) ? "onbekend" : vacancy.CompanyAddress.Trim()), StringComparison.Ordinal)
            .Replace("{workTypes}", Safe(workTypes), StringComparison.Ordinal)
            .Replace("{startDate}", vacancy.StartDate.ToString("dd-MM-yyyy"), StringComparison.Ordinal)
            .Replace("{transport}", Safe(transport), StringComparison.Ordinal)
            .Replace("{wage}", Safe(wage), StringComparison.Ordinal)
            .Replace("{description}", Safe(description), StringComparison.Ordinal);
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
            IReadOnlyList<MockInterviewMessage> history,
            MockInterviewLabels.Pack? labels = null)
        {
            var requested = labels ?? MockInterviewLabels.For("nl");
            // Scripted path is fully authored in NL + EN. Other UI languages get English
            // coach prose (labels parsed by the UI into the active Culture). OpenAI covers pl/ro/ar.
            var useEnglish = !string.Equals(requested.LanguageCode, "nl", StringComparison.OrdinalIgnoreCase);
            labels = useEnglish ? MockInterviewLabels.For("en") : requested;

            var plan = InterviewPlan.FromVacancy(vacancy);
            var userTurns = history.Count(m => m.Role == "user");

            if (history.Count == 0)
            {
                if (useEnglish)
                {
                    return
                        $"Hi! I'll help you practice for {plan.Title} at {plan.Company}. " +
                        "This is a Lobsy practice chat — not a real interview, but with questions from the vacancy.\n\n" +
                        $"Key themes I see: {plan.ThemeSummary}.\n\n" +
                        $"{labels.Question} {EnglishQuestion(plan, 0)}";
                }

                return
                    $"Hoi! Ik help je oefenen voor {plan.Title} bij {plan.Company}. " +
                    "Dit is een oefengesprek via Lobsy — geen echt gesprek, wél met vragen uit de vacature.\n\n" +
                    $"In de tekst zie ik vooral: {plan.ThemeSummary}.\n\n" +
                    $"{labels.Question} {plan.Questions[0]}";
            }

            var lastUser = history.LastOrDefault(m => m.Role == "user")?.Content?.Trim() ?? "";
            var coaching = BuildCoaching(lastUser, plan, userTurns, labels, useEnglish);
            var insulting = DutchInterviewAnswerHeuristics.LooksInsulting(lastUser);
            var vague = DutchInterviewAnswerHeuristics.LooksVague(lastUser);

            if (userTurns >= plan.Questions.Count && !insulting)
            {
                if (useEnglish)
                {
                    return
                        $"{coaching}\n\n" +
                        $"Nice practice for {plan.Title}! This was not a real assessment.\n" +
                        $"Remember: (1) link your answer to tasks from the vacancy, such as {plan.PrimaryHook}. " +
                        "(2) Use a short example: situation → what you did → result. " +
                        "Tomorrow you can rehearse this out loud in front of a mirror. Good luck!";
                }

                return
                    $"{coaching}\n\n" +
                    $"Mooi geoefend voor {plan.Title}! Dit was geen echte beoordeling.\n" +
                    $"Onthoud: (1) koppel je antwoord aan taken uit de vacature, zoals {plan.PrimaryHook}. " +
                    "(2) Gebruik een kort voorbeeld: situatie → wat jij deed → resultaat. " +
                    "Morgen kun je dit letterlijk zo oefenen hardop voor de spiegel. Succes!";
            }

            var questionIndex = insulting || vague
                ? Math.Max(0, Math.Min(userTurns - 1, plan.Questions.Count - 1))
                : Math.Min(userTurns, plan.Questions.Count - 1);
            if (questionIndex < 0)
            {
                questionIndex = 0;
            }

            var question = useEnglish
                ? EnglishQuestion(plan, questionIndex)
                : plan.Questions[questionIndex];

            return $"{coaching}\n\n{labels.Question} {question}";
        }

        private static string EnglishQuestion(InterviewPlan plan, int index)
        {
            var hook = plan.PrimaryHook;
            return (index % 5) switch
            {
                0 => $"What draws you to {plan.Title} at {plan.Company}? Mention something concrete from the vacancy (e.g. {hook}).",
                1 => $"Give one concrete example (school, sports, side job, internship) that helps you with: {hook}.",
                2 => $"It's busy around {hook} and someone asks for something else at the same time. What do you do first, and why?",
                3 => $"How will you get to work reliably, and when can you start around {plan.Themes.LastOrDefault()?.Label ?? "the start date"}?",
                _ => "Last practice question: what would you ask the employer about this vacancy — and why is that a smart question?"
            };
        }

        private static string BuildCoaching(
            string answer,
            InterviewPlan plan,
            int userTurnIndex,
            MockInterviewLabels.Pack labels,
            bool useEnglish)
        {
            if (DutchInterviewAnswerHeuristics.LooksInsulting(answer))
            {
                if (useEnglish)
                {
                    return
                        $"{labels.Caution} Please keep a respectful tone — that works better in a real interview.\n" +
                        $"{labels.Tip} Say what you mean without swear words — that comes across stronger.\n" +
                        $"{labels.Rewrite} \"For {plan.PrimaryHook} I stay calm under pressure. For example when it got busy, I did X first and then checked everything was right.\"";
                }

                var rewrite = DutchInterviewAnswerHeuristics.BuildRewriteSuggestion(answer, plan.PrimaryHook);
                return
                    $"{labels.Caution} {DutchInterviewAnswerHeuristics.FriendlyToneRedirect(plan.PrimaryHook)}\n" +
                    $"{labels.Tip} Formuleer wat je bedoelt zonder scheldwoorden — dat komt sterker over.\n" +
                    rewrite.Replace("Probeer zo:", labels.Rewrite, StringComparison.OrdinalIgnoreCase);
            }

            if (useEnglish)
            {
                var rewriteLine = DutchInterviewAnswerHeuristics.LooksVague(answer)
                    || !DutchInterviewAnswerHeuristics.HasStarCue(answer)
                    ? $"\n{labels.Rewrite} \"For example with {plan.PrimaryHook}: when [situation], I did [action], and that led to [result].\""
                    : string.Empty;
                return
                    $"{labels.Strong} You shared something we can build on.\n" +
                    $"{labels.Tip} Link your answer explicitly to: {plan.PrimaryHook}.{rewriteLine}";
            }

            var strong = BuildStrong(answer, plan, userTurnIndex);
            var tip = BuildTip(answer, plan, userTurnIndex);
            var nlRewrite = DutchInterviewAnswerHeuristics.LooksVague(answer)
                || !DutchInterviewAnswerHeuristics.HasStarCue(answer)
                ? "\n" + DutchInterviewAnswerHeuristics.BuildRewriteSuggestion(answer, plan.PrimaryHook)
                    .Replace("Probeer zo:", labels.Rewrite, StringComparison.OrdinalIgnoreCase)
                : string.Empty;

            return $"{labels.Strong} {strong}\n{labels.Tip} {tip}{nlRewrite}";
        }

        private static string BuildStrong(string answer, InterviewPlan plan, int userTurnIndex)
        {
            var quote = DutchInterviewAnswerHeuristics.ExtractQuote(answer);
            var quoteBit = string.IsNullOrWhiteSpace(quote) ? null : $" (“{quote}”)";

            if (answer.Length < 12)
            {
                return Pick(userTurnIndex, answer,
                    "Je bent in ieder geval begonnen — dat telt. Nu maken we het concreter.",
                    "Kort maar duidelijk: je doet mee. Laten we er één voorbeeld onder zetten.",
                    "Goed dat je reageert. Met één zin extra wordt het al sterker.");
            }

            var matchedTheme = plan.Themes.FirstOrDefault(t =>
                answer.Contains(t.Keyword, StringComparison.OrdinalIgnoreCase));
            if (matchedTheme is not null)
            {
                return Pick(userTurnIndex, answer,
                    $"Je noemt iets over {matchedTheme.Label}{quoteBit} — dat sluit aan bij wat {plan.Company} zoekt.",
                    $"Mooi dat {matchedTheme.Label} terugkomt in je antwoord{quoteBit}. Dat hoort bij deze vacature.",
                    $"Je koppelt al richting {matchedTheme.Label}{quoteBit}. Een recruiter hoort dat graag.");
            }

            if (ContainsAny(answer, "bijvoorbeeld", "toen", "vorige", "school", "stage", "werk", "ik heb", "ik deed"))
            {
                return Pick(userTurnIndex, answer,
                    $"Je geeft al een stukje ervaring of voorbeeld{quoteBit}. Dat klinkt geloofwaardiger dan alleen ‘ik wil graag’.",
                    $"Sterk dat je een voorbeeld aansnijdt{quoteBit}. Dat blijft beter hangen dan vage beloftes.",
                    $"Je deelt iets echts uit je ervaring{quoteBit}. Dat is precies wat een gesprek levend maakt.");
            }

            if (answer.Length >= 80)
            {
                return Pick(userTurnIndex, answer,
                    $"Je antwoord is inhoudelijk{quoteBit}; je denkt na over de rol. Dat merkt een recruiter.",
                    $"Je legt al best wat uit{quoteBit}. Met één resultaat erbij wordt het nóg scherper.",
                    $"Duidelijk dat je de moeite neemt{quoteBit}. Laten we het afronden met wat het opleverde.");
            }

            return Pick(userTurnIndex, answer,
                $"Duidelijk dat je reageert op {plan.Title}{quoteBit}. Je hebt een basis waarop we kunnen bouwen.",
                $"Je pakt de vraag serieus op{quoteBit}. Nu maken we hem vacature-specifieker.",
                $"Heldere start over {plan.Title}{quoteBit}. Eén concreet detail maakt het af.");
        }

        private static string BuildTip(string answer, InterviewPlan plan, int userTurnIndex)
        {
            if (answer.Length < 25)
            {
                return Pick(userTurnIndex, answer,
                    $"Maak het langer met één voorbeeld. Koppel het aan: {plan.PrimaryHook}.",
                    $"Voeg 1 zin toe: wat deed jij precies bij {plan.PrimaryHook}?",
                    $"Noem één moment (school/werk/sport) dat past bij {plan.PrimaryHook}.");
            }

            if (!DutchInterviewAnswerHeuristics.HasStarCue(answer))
            {
                return Pick(userTurnIndex, answer,
                    "Voeg een mini-structuur toe: situatie → wat jij deed → wat het opleverde.",
                    "Eindig met het resultaat: wat ging er beter door jouw actie?",
                    "Begin met ‘Toen …’ en zeg daarna wat jij deed. Dat klinkt als een verhaal.");
            }

            if (userTurnIndex <= 1)
            {
                return Pick(userTurnIndex, answer,
                    $"Noem expliciet een taak uit de vacature (bijv. {plan.PrimaryHook}) en zeg hoe jij dat aanpakt.",
                    $"Koppel je voorbeeld aan {plan.PrimaryHook} — dan hoort de recruiter meteen de match.",
                    $"Zeg in één zin waarom dit past bij {plan.Company}.");
            }

            if (plan.Themes.Count > 1 && userTurnIndex < plan.Themes.Count)
            {
                var next = plan.Themes[Math.Min(userTurnIndex, plan.Themes.Count - 1)];
                return Pick(userTurnIndex, answer,
                    $"In een echt gesprek kun je ook {next.Label} noemen — dat staat centraal in deze vacature.",
                    $"Bonuspunt: breng {next.Label} kort in; dat staat in de tekst van {plan.Company}.",
                    $"Je mag straks ook {next.Label} aantikken — dat versterkt je verhaal.");
            }

            return Pick(userTurnIndex, answer,
                "Houd het in een live gesprek iets korter en eindig met waarom dit bij deze vacature past.",
                "Oefen hardop: max 30 seconden, met één voorbeeld en één resultaat.",
                "Sluit af met een korte vraag terug — dat maakt je actief in het gesprek.");
        }

        private static string Pick(int turn, string answer, params string[] options)
        {
            if (options.Length == 0)
            {
                return string.Empty;
            }

            var hash = turn * 397;
            unchecked
            {
                foreach (var ch in answer)
                {
                    hash = (hash * 31) + ch;
                }
            }

            var idx = Math.Abs(hash) % options.Length;
            return options[idx];
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
