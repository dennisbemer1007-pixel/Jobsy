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
        - Hard requirements (if any): {requirements}
        - Hours/week (if known): {hours}
        - Vacancy text:
        {description}

        Candidate profile (from their Lobsy account — may be incomplete):
        {candidateProfile}

        Known profile↔vacancy gaps (soft coaching, NOT gatekeeping):
        {mismatchGaps}

        Core task — vacancy-first + interactive:
        - Pull 3–5 concrete tasks/requirements from the vacancy text and ASK about them.
        - First question MUST quote or closely paraphrase a concrete sentence/task from the vacancy text.
        - Prefer real recruiter questions ("In the vacancy it says … — how do you handle that?") over generic motivation questions.
        - When gaps are listed: ask about 1–2 of them during the practice (curious coaching tone; help them prepare an honest answer).
        - React to details from their last answer (quote or paraphrase). No generic praise.
        - If they skip a vacancy detail, ask a short clarifying follow-up on that detail.
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
        MockInterviewCandidateContext? candidate = null,
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
                    vacancy, candidate, sanitized, labels, apiKey, model, baseUrl, cancellationToken);
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
            ScriptedFallback.NextReply(vacancy, sanitized, labels, candidate),
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
        MockInterviewCandidateContext? candidate,
        IReadOnlyList<MockInterviewMessage> history,
        MockInterviewLabels.Pack labels,
        string apiKey,
        string model,
        string baseUrl,
        CancellationToken cancellationToken)
    {
        var system = BuildSystemPrompt(vacancy, candidate, labels);
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
                    "quote or closely paraphrase ONE concrete task/requirement from the vacancy text, " +
                    $"and ask your first focused question about that task (start with '{labels.Question} '). " +
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
            var gapHint = candidate?.Gaps is { Count: > 0 }
                ? " If a listed profile↔vacancy gap has not been discussed yet, make the next question about one gap (curious coaching, not gatekeeping)."
                : " Prefer the next question about another concrete detail from the vacancy text.";
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
                    gapHint + " " +
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

    private static string BuildSystemPrompt(
        MockInterviewVacancyContext vacancy,
        MockInterviewCandidateContext? candidate,
        MockInterviewLabels.Pack labels)
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
        var requirements = BuildRequirementsLine(vacancy);
        var hours = vacancy.MinHoursPerWeek is not null && vacancy.MaxHoursPerWeek is not null
            ? $"{vacancy.MinHoursPerWeek:0.#}–{vacancy.MaxHoursPerWeek:0.#} u/w"
            : "niet gespecificeerd";
        var candidateProfile = FormatCandidateProfile(candidate);
        var mismatchGaps = candidate?.Gaps is { Count: > 0 } gaps
            ? string.Join("\n", gaps.Select(g => $"- {g.Summary}"))
            : "- geen duidelijke gaps gevonden (blijf vacaturetekst-vragen stellen)";

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
            .Replace("{requirements}", Safe(requirements), StringComparison.Ordinal)
            .Replace("{hours}", Safe(hours), StringComparison.Ordinal)
            .Replace("{candidateProfile}", Safe(candidateProfile), StringComparison.Ordinal)
            .Replace("{mismatchGaps}", Safe(mismatchGaps), StringComparison.Ordinal)
            .Replace("{description}", Safe(description), StringComparison.Ordinal);
    }

    private static string BuildRequirementsLine(MockInterviewVacancyContext vacancy)
    {
        var parts = new List<string>();
        if (!string.IsNullOrWhiteSpace(vacancy.RequiredDrivingLicense))
        {
            parts.Add($"rijbewijs {vacancy.RequiredDrivingLicense.Trim()}");
        }

        if (!string.IsNullOrWhiteSpace(vacancy.RequiredEducation))
        {
            parts.Add($"opleiding {vacancy.RequiredEducation.Trim()}");
        }

        if (vacancy.MinimumEmployers is > 0)
        {
            parts.Add($"min. {vacancy.MinimumEmployers} werkgever(s)");
        }

        return parts.Count == 0 ? "geen harde eisen vermeld" : string.Join("; ", parts);
    }

    private static string FormatCandidateProfile(MockInterviewCandidateContext? candidate)
    {
        if (candidate is null)
        {
            return "- (geen profielgegevens beschikbaar — stel alleen vacaturetekst-vragen)";
        }

        var lines = new List<string>
        {
            $"- About me: {(string.IsNullOrWhiteSpace(candidate.AboutMe) ? "(leeg)" : Truncate(candidate.AboutMe!, 280))}",
            $"- Licenses: {(candidate.DrivingLicenses.Count == 0 ? "(niet ingevuld)" : string.Join(", ", candidate.DrivingLicenses))}",
            $"- Education: {(candidate.Educations.Count == 0 ? "(niet ingevuld)" : string.Join(", ", candidate.Educations))}",
            $"- Employers: {(candidate.EmployerSummaries.Count == 0 ? "(geen)" : string.Join("; ", candidate.EmployerSummaries))}",
            $"- Hours preference: {(string.IsNullOrWhiteSpace(candidate.HoursSummary) ? "(niet ingevuld)" : candidate.HoursSummary)}"
        };
        return string.Join("\n", lines);
    }

    private static string Truncate(string value, int max)
        => value.Length <= max ? value : value[..(max - 1)].TrimEnd() + "…";

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
            MockInterviewLabels.Pack? labels = null,
            MockInterviewCandidateContext? candidate = null)
        {
            var requested = labels ?? MockInterviewLabels.For("nl");
            // Scripted path is fully authored in NL + EN. Other UI languages get English
            // coach prose (labels parsed by the UI into the active Culture). OpenAI covers pl/ro/ar.
            var useEnglish = !string.Equals(requested.LanguageCode, "nl", StringComparison.OrdinalIgnoreCase);
            labels = useEnglish ? MockInterviewLabels.For("en") : requested;

            var plan = InterviewPlan.FromVacancy(vacancy, candidate);
            var userTurns = history.Count(m => m.Role == "user");

            if (history.Count == 0)
            {
                if (useEnglish)
                {
                    return
                        $"Hi! I'll help you practice for {plan.Title} at {plan.Company}. " +
                        "This is a Lobsy practice chat — not a real interview, but with questions from the vacancy text" +
                        (plan.HasGapQuestions ? " and your profile fit" : "") + ".\n\n" +
                        $"Key themes I see: {plan.ThemeSummary}.\n\n" +
                        $"{labels.Question} {PickQuestion(plan, 0, useEnglish: true)}";
                }

                return
                    $"Hoi! Ik help je oefenen voor {plan.Title} bij {plan.Company}. " +
                    "Dit is een oefengesprek via Lobsy — geen echt gesprek, wél met vragen uit de vacaturetekst" +
                    (plan.HasGapQuestions ? " én over verschillen met jouw profiel" : "") + ".\n\n" +
                    $"In de tekst zie ik vooral: {plan.ThemeSummary}.\n\n" +
                    $"{labels.Question} {PickQuestion(plan, 0, useEnglish: false)}";
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

            var question = PickQuestion(plan, questionIndex, useEnglish);
            return $"{coaching}\n\n{labels.Question} {question}";
        }

        private static string PickQuestion(InterviewPlan plan, int index, bool useEnglish)
        {
            if (index < 0 || index >= plan.Questions.Count)
            {
                return useEnglish
                    ? "Last practice question: what would you ask the employer about this vacancy — and why is that a smart question?"
                    : "Laatste oefenvraag: welke vraag zou jij zelf aan de werkgever stellen over deze vacature — en waarom is die vraag slim?";
            }

            return useEnglish ? plan.EnglishQuestions[index] : plan.Questions[index];
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

    internal sealed record InterviewTheme(
        string Keyword,
        string Label,
        string Question,
        string EnglishQuestion);

    internal sealed class InterviewPlan
    {
        public required string Title { get; init; }
        public required string Company { get; init; }
        public required string PrimaryHook { get; init; }
        public required string ThemeSummary { get; init; }
        public required IReadOnlyList<InterviewTheme> Themes { get; init; }
        public required IReadOnlyList<string> Questions { get; init; }
        public required IReadOnlyList<string> EnglishQuestions { get; init; }
        public bool HasGapQuestions { get; init; }

        public static InterviewPlan FromVacancy(
            MockInterviewVacancyContext vacancy,
            MockInterviewCandidateContext? candidate = null)
        {
            var company = string.IsNullOrWhiteSpace(vacancy.CompanyName) ? "ons bedrijf" : vacancy.CompanyName.Trim();
            var title = string.IsNullOrWhiteSpace(vacancy.Title) ? "deze functie" : vacancy.Title.Trim();
            var plain = HtmlSanitize.ToPlainPreview(vacancy.Description, maxLength: 2_500);
            var workTypes = vacancy.WorkTypes ?? [];
            var transportModes = vacancy.RequiredTransport ?? [];
            var haystack = $"{title} {plain} {string.Join(' ', workTypes)}".ToLowerInvariant();
            var transport = transportModes.FirstOrDefault() ?? "fiets of OV";

            var themes = new List<InterviewTheme>();
            var snippets = ExtractDutySnippets(plain, max: 3);
            var snippet = snippets.FirstOrDefault() ?? title.ToLowerInvariant();

            // Lead with a question that quotes the vacancy text when we have a duty sentence.
            if (snippets.Count > 0)
            {
                var quote = snippets[0];
                AddTheme(
                    themes,
                    "vacaturetekst",
                    "taken uit de vacaturetekst",
                    $"In de vacature staat: „{Capitalize(quote)}”. Hoe pak jij dat aan — graag met een kort voorbeeld?",
                    $"The vacancy says: \"{Capitalize(quote)}\". How would you handle that — with a short example?");
            }

            // Domain cues from typical Westland / youth job ads
            TryAddMatch(haystack, themes, "klant", "klantcontact",
                "In de vacature komt klantcontact terug. Vertel hoe jij met een lastige of drukke klant omgaat — graag met een voorbeeld.",
                "Customer contact comes up in the vacancy. Tell how you handle a difficult or busy customer — with an example.");
            TryAddMatch(haystack, themes, "gast", "gasten/horeca",
                "Er staat werk met gasten in de tekst. Hoe zorg jij dat gasten zich welkom voelen, ook als het druk is?",
                "The text mentions working with guests. How do you make guests feel welcome, even when it is busy?");
            TryAddMatch(haystack, themes, "kassa", "kassa/afrekenen",
                "Afrekenen of kassa komt terug. Hoe blijf jij vriendelijk én nauwkeurig als er een rij staat?",
                "Checkout/till work comes up. How do you stay friendly and accurate when there is a queue?");
            TryAddMatch(haystack, themes, "magazijn", "magazijnwerk",
                "Magazijnwerk hoort bij de rol. Hoe ga jij om met tillen, tempo en netjes werken?",
                "Warehouse work is part of the role. How do you handle lifting, pace, and working neatly?");
            TryAddMatch(haystack, themes, "inpak", "inpakken/orderpick",
                "Inpakken of orders verzamelen staat in de vacature. Hoe zorg jij dat je snel wérkt zonder fouten?",
                "Packing or picking orders is in the vacancy. How do you work quickly without mistakes?");
            TryAddMatch(haystack, themes, "bezorg", "bezorgen",
                "Bezorgen speelt een rol. Hoe plan jij een route en wat doe je als iets niet lukt (weer, adres, vertraging)?",
                "Delivery plays a role. How do you plan a route, and what do you do when something goes wrong?");
            TryAddMatch(haystack, themes, "schoonmaak", "schoonmaak",
                "Schoonmaak staat in de tekst. Hoe controleer jij of iets écht schoon/klaar is?",
                "Cleaning is in the text. How do you check that something is truly clean/ready?");
            TryAddMatch(haystack, themes, "team", "samenwerken",
                "Samenwerken is belangrijk hier. Vertel over een moment waarop je een collega hielp of om hulp vroeg.",
                "Teamwork matters here. Tell about a moment you helped a colleague or asked for help.");
            TryAddMatch(haystack, themes, "collega", "samenwerken",
                "De vacature noemt collega’s. Hoe ga jij om met feedback van een leidinggevende of teamgenoot?",
                "The vacancy mentions colleagues. How do you handle feedback from a manager or teammate?");
            TryAddMatch(haystack, themes, "verantwoord", "verantwoordelijkheid",
                "Er wordt verantwoordelijkheid gevraagd. Geef een voorbeeld waarin jij iets zelf oppakte zonder dat iemand erachteraan hoefde.",
                "Responsibility is asked for. Give an example where you took something on without someone chasing you.");
            TryAddMatch(haystack, themes, "weekend", "beschikbaarheid in weekenden",
                "Weekenden komen terug. Hoe ziet jouw beschikbaarheid eruit, en hoe combineer je dat met school/andere plannen?",
                "Weekends come up. What does your availability look like, and how do you combine that with school/other plans?");
            TryAddMatch(haystack, themes, "avond", "avonddiensten",
                "Avondwerk speelt mee. Past dat bij jou, en hoe zorg je dat je fit en op tijd bent?",
                "Evening work is involved. Does that fit you, and how do you stay fit and on time?");
            TryAddMatch(haystack, themes, "flexibel", "flexibiliteit",
                "Flexibiliteit staat in de vacature. Kun je een voorbeeld geven waarin je snel schakelde toen plannen veranderden?",
                "Flexibility is in the vacancy. Can you give an example where you adapted quickly when plans changed?");
            TryAddMatch(haystack, themes, "tuinbouw", "tuinbouw/productie",
                "Tuinbouw of productie hoort bij de rol. Hoe ga jij om met herhalend werk en tempo op de werkvloer?",
                "Horticulture or production is part of the role. How do you handle repetitive work and pace on the floor?");
            TryAddMatch(haystack, themes, "kas", "werk in de kas",
                "Werk in/rond de kas komt terug. Wat spreekt jou daarin aan, en hoe houd je het vol op een drukke dag?",
                "Greenhouse work comes up. What appeals to you about that, and how do you keep going on a busy day?");

            foreach (var wt in workTypes)
            {
                var key = wt.Trim().ToLowerInvariant();
                if (key.Contains("horeca", StringComparison.Ordinal))
                {
                    AddTheme(themes, "horeca", "horeca",
                        "Dit is een horecarol. Hoe blijf jij vriendelijk als het tegelijk druk én warm is achter de bar/in de zaak?",
                        "This is a hospitality role. How do you stay friendly when it is both busy and hot behind the bar/in the venue?");
                }
                else if (key.Contains("logistiek", StringComparison.Ordinal))
                {
                    AddTheme(themes, "logistiek", "logistiek",
                        "Logistiek staat centraal. Hoe voorkom jij fouten bij tellen, labels of orders?",
                        "Logistics is central. How do you prevent mistakes with counting, labels, or orders?");
                }
                else if (key.Contains("winkel", StringComparison.Ordinal) || key.Contains("retail", StringComparison.Ordinal))
                {
                    AddTheme(themes, "winkel", "winkelwerk",
                        "Winkelwerk hoort erbij. Wat doe je als een klant iets zoekt dat je niet meteen kunt vinden?",
                        "Shop work is part of it. What do you do when a customer looks for something you cannot find right away?");
                }
            }

            // Second quoted duty if available and distinct.
            if (snippets.Count > 1)
            {
                var quote2 = snippets[1];
                AddTheme(
                    themes,
                    "vacaturetekst-2",
                    "tweede taak uit de tekst",
                    $"Nog iets uit de tekst: „{Capitalize(quote2)}”. Wat zou jij als eerste doen als dit op je bord komt?",
                    $"Another line from the text: \"{Capitalize(quote2)}\". What would you do first if this landed on your plate?");
            }

            if (themes.Count == 0)
            {
                themes.Add(new InterviewTheme(
                    "motivatie",
                    "motivatie voor deze rol",
                    $"Waarom past {title} bij jou — noem iets uit de vacaturetekst dat je aanspreekt.",
                    $"Why does {title} fit you — mention something from the vacancy text that appeals to you."));
            }

            while (themes.Count < 3)
            {
                themes.Add(themes.Count switch
                {
                    1 => new InterviewTheme(
                        "ervaring",
                        "ervaring of school",
                        $"Welke ervaring (school, sport, bijbaan, stage) helpt jou bij {snippet}? Geef één concreet voorbeeld.",
                        $"Which experience (school, sports, side job, internship) helps you with {snippet}? Give one concrete example."),
                    2 => new InterviewTheme(
                        "drukte",
                        "werken onder druk",
                        $"Stel: het is druk rond {snippet} en iemand vraagt tegelijk iets anders. Wat doe je eerst, en waarom?",
                        $"Imagine it is busy around {snippet} and someone asks for something else at the same time. What do you do first, and why?"),
                    _ => new InterviewTheme(
                        "vervoer",
                        "reistijd/vervoer",
                        $"In de vacature past vervoer als {transport}. Hoe kom jij meestal naar het werk, en hoe betrouwbaar is dat?",
                        $"The vacancy fits transport like {transport}. How do you usually get to work, and how reliable is that?")
                });
            }

            // Insert profile↔vacancy gap questions before the closer (max 2).
            var gapThemes = new List<InterviewTheme>();
            foreach (var gap in (candidate?.Gaps ?? []).Take(2))
            {
                gapThemes.Add(new InterviewTheme(
                    $"gap-{gap.Key}",
                    gap.Key switch
                    {
                        "license" => "rijbewijs vs. vacature",
                        "education" => "opleiding vs. vacature",
                        "employers" => "ervaring vs. vacature",
                        "hours" or "hours-unknown" => "uren vs. vacature",
                        "schedule" => "beschikbaarheid vs. vacature",
                        "about-me" => "profielverhaal",
                        _ => "profiel vs. vacature"
                    },
                    gap.Question,
                    gap.EnglishQuestion));
            }

            // Build final question list: up to 3 vacancy themes, then gaps, pad if needed, then closer.
            var selected = themes.Take(3).ToList();
            foreach (var gap in gapThemes)
            {
                if (selected.All(t => t.Keyword != gap.Keyword))
                {
                    selected.Add(gap);
                }
            }

            var pad = 0;
            while (selected.Count < 4)
            {
                pad++;
                selected.Add(new InterviewTheme(
                    $"start-{pad}",
                    "beschikbaarheid",
                    $"Wanneer kun je starten (rond {vacancy.StartDate:dd-MM}) en hoeveel uur per week past bij jou voor {title}?",
                    $"When can you start (around {vacancy.StartDate:dd-MM}) and how many hours per week fit you for {title}?"));
            }

            var questions = selected.Select(t => t.Question).ToList();
            var englishQuestions = selected.Select(t => t.EnglishQuestion).ToList();
            questions.Add(
                "Laatste oefenvraag: welke vraag zou jij zelf aan de werkgever stellen over deze vacature — en waarom is die vraag slim?");
            englishQuestions.Add(
                "Last practice question: what would you ask the employer about this vacancy — and why is that a smart question?");

            var summaryParts = selected.Take(4).Select(t => t.Label).ToList();
            return new InterviewPlan
            {
                Title = title,
                Company = company,
                PrimaryHook = snippet,
                ThemeSummary = string.Join(", ", summaryParts),
                Themes = selected,
                Questions = questions,
                EnglishQuestions = englishQuestions,
                HasGapQuestions = gapThemes.Count > 0
            };
        }

        private static void TryAddMatch(
            string haystack,
            List<InterviewTheme> themes,
            string keyword,
            string label,
            string question,
            string englishQuestion)
        {
            if (!haystack.Contains(keyword, StringComparison.Ordinal))
            {
                return;
            }

            AddTheme(themes, keyword, label, question, englishQuestion);
        }

        private static void AddTheme(
            List<InterviewTheme> themes,
            string keyword,
            string label,
            string question,
            string englishQuestion)
        {
            if (themes.Any(t => t.Label.Equals(label, StringComparison.OrdinalIgnoreCase)
                                || t.Keyword.Equals(keyword, StringComparison.OrdinalIgnoreCase)))
            {
                return;
            }

            themes.Add(new InterviewTheme(keyword, label, question, englishQuestion));
        }

        private static IReadOnlyList<string> ExtractDutySnippets(string plain, int max)
        {
            if (string.IsNullOrWhiteSpace(plain) || max <= 0)
            {
                return [];
            }

            var sentences = Regex.Split(plain, @"(?<=[\.\!\?\n])\s+")
                .Select(s => s.Trim().TrimStart('-', '•', '*', ' '))
                .Where(s => s.Length is >= 24 and <= 160)
                .ToList();

            var duty = sentences.Where(ContainsDutyVerb).Select(TrimSnippet).Where(s => s.Length >= 20).ToList();
            if (duty.Count == 0 && sentences.Count > 0)
            {
                duty.Add(TrimSnippet(sentences[0]));
            }

            return duty.Distinct(StringComparer.OrdinalIgnoreCase).Take(max).ToList();
        }

        private static bool ContainsDutyVerb(string s)
        {
            var lower = s.ToLowerInvariant();
            return lower.Contains("je ") || lower.Contains("jij ")
                || lower.Contains("zorgen") || lower.Contains("helpen")
                || lower.Contains("werken") || lower.Contains("verantwoordelijk")
                || lower.Contains("taken") || lower.Contains("opdracht")
                || lower.Contains("you ") || lower.Contains("will ");
        }

        private static string TrimSnippet(string value)
        {
            var cleaned = Regex.Replace(value, @"\s+", " ").Trim();
            if (cleaned.Length > 100)
            {
                cleaned = cleaned[..97].TrimEnd(',', '.', ';', ' ') + "…";
            }

            return cleaned;
        }

        private static string Capitalize(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return value;
            }

            var trimmed = value.Trim();
            return char.ToUpperInvariant(trimmed[0]) + trimmed[1..];
        }
    }
}
