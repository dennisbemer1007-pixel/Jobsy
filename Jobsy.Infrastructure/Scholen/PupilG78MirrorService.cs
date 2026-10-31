using System.Net.Http.Json;
using Jobsy.Core.Entities.Scholen;
using Jobsy.Core.Enums;
using Jobsy.Core.Interfaces;
using Jobsy.Core.Rules;
using Jobsy.Core.Scholen;
using Jobsy.Core.Scholen.PupilPassport;
using Microsoft.Extensions.Logging;

namespace Jobsy.Infrastructure.Scholen;

public sealed class PupilG78MirrorService(
    IHttpClientFactory httpClientFactory,
    IOpenAiEndpointResolver openAi,
    ILogger<PupilG78MirrorService> logger) : IPupilG78MirrorRenderer
{
    public const string HttpClientName = "OpenAIPupilG78Mirror";

    public async Task<string> RenderMirrorBodyAsync(
        PupilResult result,
        PupilProgress? progress,
        string language,
        CancellationToken cancellationToken = default)
    {
        var vak = PupilVakScores.Parse(progress?.VakScoresJson);
        var template = PupilG78MirrorTemplate.Render(result, progress, vak, language);
        var endpoint = await openAi.ResolveAsync(OpenAiFeature.TestReflection, cancellationToken);
        if (string.IsNullOrWhiteSpace(endpoint.ApiKey))
        {
            return template;
        }

        try
        {
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            cts.CancelAfter(TimeSpan.FromSeconds(12));
            var sheet = PupilG78MirrorTemplate.FactSheet(result, progress, vak, language);
            var rewritten = await TryRewriteAsync(template, sheet, endpoint, cts.Token);
            if (rewritten is null)
            {
                return template;
            }

            var reason = CandidateFactGuard.RejectionReason(rewritten, sheet);
            if (reason is not null)
            {
                logger.LogInformation("G78 mirror rejected ({Reason}); template fallback.", reason);
                return template;
            }

            return rewritten;
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogWarning(ex, "G78 mirror OpenAI failed; template fallback.");
            return template;
        }
    }

    private async Task<string?> TryRewriteAsync(
        string template,
        CandidateFactSheet sheet,
        OpenAiEndpointResolution endpoint,
        CancellationToken ct)
    {
        var client = httpClientFactory.CreateClient(HttpClientName);
        using var request = new HttpRequestMessage(
            HttpMethod.Post,
            new Uri(new Uri(endpoint.BaseUrl, UriKind.Absolute), "chat/completions"));
        request.Headers.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", endpoint.ApiKey);
        var system = """
            Gebruik alleen feiten uit de feitenlijst. Verzin geen namen, werk, diploma's of nieuwe hobby's.
            Geen markdown, geen opsommingstekens. Noem geen woonplaats, adres of postcode.
            """ + "\n" + CandidateFactGuard.StrictAddendum;
        request.Content = System.Net.Http.Json.JsonContent.Create(new
        {
            model = endpoint.Model,
            temperature = 0.35,
            messages = new object[]
            {
                new { role = "system", content = system },
                new { role = "user", content = sheet.ToPrompt() + "\nTekst:\n" + template }
            }
        });

        using var response = await client.SendAsync(request, ct);
        response.EnsureSuccessStatusCode();
        var json = await response.Content.ReadFromJsonAsync<ChatCompletion>(cancellationToken: ct);
        var text = json?.Choices?.FirstOrDefault()?.Message?.Content?.Trim();
        return string.IsNullOrWhiteSpace(text) ? null : text;
    }

    private sealed class ChatCompletion
    {
        public List<Choice>? Choices { get; set; }
    }

    private sealed class Choice
    {
        public Message? Message { get; set; }
    }

    private sealed class Message
    {
        public string? Content { get; set; }
    }
}
