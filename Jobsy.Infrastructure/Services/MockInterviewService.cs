using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
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
        Je bent een vriendelijke, realistische recruiter of lokale werkgever in Nederland.
        Je voert een OEFEN-sollicitatiegesprek (mock interview) via chat. Dit is géén echt gesprek
        en géén toezegging van werk. Doel: de kandidaat laten oefenen en zenuwen wegnemen.

        Vacaturecontext:
        - Functie: {title}
        - Werkgever: {company}
        - Adres: {address}
        - Startdatum: {startDate}
        - Vervoer: {transport}
        - Uurloon (alleen als zichtbaar/bekend): {wage}
        - Vacaturetekst:
        {description}

        Gedragsregels:
        1. Spreek Nederlands, kort en natuurlijk (alsof je echt belt of spreekt op locatie).
        2. Stel één vraag per beurt, gebaseerd op de vacature (ervaring, motivatie, beschikbaarheid,
           vervoer, omgang met klanten/collega's, praktische situaties).
        3. Na elk antwoord van de kandidaat: geef eerst 1–3 zinnen constructieve feedback
           (wat goed ging + tip), daarna stel je de volgende vraag.
        4. Wees bemoedigend; corrigeer vriendelijk zonder neerbuigend te zijn.
        5. Verzin geen harde toezeggingen over salaris, contract of aanname.
        6. Vraag nooit om BSN, bankgegevens, wachtwoorden of andere zeer gevoelige data.
        7. Als de kandidaat vraagt of dit echt is: leg uit dat dit een oefengesprek via Jobsy is.
        8. Houd antwoorden compact (max ~120 woorden per beurt).
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
                    "Start het oefengesprek. Stel je kort voor als recruiter/werkgever van dit bedrijf, " +
                    "benoem dat dit een oefengesprek is, en stel je eerste vraag."
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
            temperature = 0.7,
            max_tokens = 450,
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
        var transport = vacancy.RequiredTransport.Count > 0
            ? string.Join(", ", vacancy.RequiredTransport)
            : "niet gespecificeerd";
        var wage = vacancy.HourlyWage is null
            ? "niet getoond"
            : $"€ {vacancy.HourlyWage.Value:0.00} per uur";

        return SystemPromptTemplate
            .Replace("{title}", vacancy.Title.Trim(), StringComparison.Ordinal)
            .Replace("{company}", vacancy.CompanyName.Trim(), StringComparison.Ordinal)
            .Replace("{address}", string.IsNullOrWhiteSpace(vacancy.CompanyAddress) ? "onbekend" : vacancy.CompanyAddress.Trim(), StringComparison.Ordinal)
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

    /// <summary>Deterministic Dutch practice flow when OpenAI is unavailable.</summary>
    public static class ScriptedFallback
    {
        public static string NextReply(
            MockInterviewVacancyContext vacancy,
            IReadOnlyList<MockInterviewMessage> history)
        {
            var company = string.IsNullOrWhiteSpace(vacancy.CompanyName) ? "ons bedrijf" : vacancy.CompanyName.Trim();
            var title = string.IsNullOrWhiteSpace(vacancy.Title) ? "deze functie" : vacancy.Title.Trim();
            var userTurns = history.Count(m => m.Role == "user");

            if (history.Count == 0)
            {
                return
                    $"Hoi! Leuk dat je oefent voor {title} bij {company}. Dit is een oefengesprek via Jobsy — " +
                    "geen echt gesprek, maar wel met realistische vragen.\n\n" +
                    "Laten we beginnen: waarom spreekt deze vacature je aan?";
            }

            var lastUser = history.LastOrDefault(m => m.Role == "user")?.Content?.Trim() ?? "";
            var tip = BuildFeedback(lastUser);

            return userTurns switch
            {
                1 => $"{tip}\n\nGoed. Wat voor ervaring of school/werk heb je die hierbij past?",
                2 => $"{tip}\n\nHelder. Hoe zou je meestal naar het werk komen, en past dat bij wat we vragen?",
                3 => $"{tip}\n\nStel: het is druk en een collega vraagt hulp terwijl jij iets anders moet afmaken. Wat doe je?",
                4 => $"{tip}\n\nWanneer zou je kunnen starten, en hoeveel uur per week past bij jou?",
                5 => $"{tip}\n\nLaatste oefenvraag: heb je zelf nog iets dat je aan een echte recruiter zou willen vragen?",
                _ =>
                    $"{tip}\n\nMooi geoefend! Dit was een oefengesprek — geen echte beoordeling. " +
                    "Tip voor later: bereid 2 concrete voorbeelden voor (situatie → wat jij deed → resultaat) " +
                    $"en lees de vacature van {company} nog eens rustig door. Succes als je gaat solliciteren!"
            };
        }

        private static string BuildFeedback(string answer)
        {
            if (answer.Length < 12)
            {
                return "Dank je. Probeer iets uitgebreider te antwoorden — noem graag een concreet voorbeeld. Dat maakt je antwoord sterker.";
            }

            if (answer.Length < 40)
            {
                return "Duidelijk antwoord. Tip: voeg één kort voorbeeld toe (wat gebeurde er, wat deed jij?). Zo klinkt het overtuigender.";
            }

            return "Sterk dat je dit zo toelicht. Tip: houd je antwoord bij een echt gesprek iets korter en eindig met waarom dit bij de vacature past.";
        }
    }
}
