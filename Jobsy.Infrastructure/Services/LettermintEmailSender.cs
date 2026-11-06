using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using Jobsy.Core.Interfaces;
using Jobsy.Core.Options;
using Jobsy.Core.Security;

namespace Jobsy.Infrastructure.Services;

/// <summary>
/// Sends one message through the Lettermint HTTP API
/// (<c>POST https://api.lettermint.co/v1/send</c>, header <c>x-lettermint-token</c>).
/// Open and click tracking stay off on the request. There is no Resend webhook handler in this
/// codebase, so there is no Lettermint bounce webhook either.
/// </summary>
public static class LettermintEmailSender
{
    public const string HttpClientName = "LettermintMail";
    public const string DefaultApiBase = LettermintOptions.DefaultBaseUrl;
    public const string TokenHeaderName = "x-lettermint-token";

    private static readonly Regex EmailInText = new(
        @"[A-Z0-9._%+\-]+@[A-Z0-9.\-]+\.[A-Z]{2,}",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant,
        TimeSpan.FromMilliseconds(100));

    public static async Task SendAsync(
        HttpClient client,
        EmailMessage message,
        string apiKey,
        string fromAddress,
        CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, "send");
        request.Headers.TryAddWithoutValidation(TokenHeaderName, apiKey.Trim());
        if (!string.IsNullOrWhiteSpace(message.IdempotencyKey))
        {
            request.Headers.TryAddWithoutValidation("Idempotency-Key", message.IdempotencyKey.Trim());
        }

        request.Content = JsonContent.Create(CreateRequest(message, fromAddress));
        using var response = await client.SendAsync(request, cancellationToken);
        var body = await response.Content.ReadAsStringAsync(cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException(
                FormatError((int)response.StatusCode, body, apiKey));
        }
    }

    public static LettermintSendRequest CreateRequest(EmailMessage message, string fromAddress)
    {
        Dictionary<string, string>? headers = null;
        if (message.Headers is { Count: > 0 })
        {
            headers = new Dictionary<string, string>(message.Headers, StringComparer.OrdinalIgnoreCase);
        }

        List<LettermintTag>? tags = null;
        Dictionary<string, string>? metadata = null;
        if (message.Tags is { Count: > 0 })
        {
            tags = [];
            metadata = new Dictionary<string, string>(StringComparer.Ordinal);
            foreach (var (name, value) in message.Tags)
            {
                if (string.IsNullOrWhiteSpace(name) || value is null)
                {
                    continue;
                }

                var tagName = name.Trim();
                tags.RemoveAll(tag => string.Equals(tag.Name, tagName, StringComparison.Ordinal));
                tags.Add(new LettermintTag { Name = tagName, Value = value });
                metadata[tagName] = value;
            }

            if (tags.Count > 20)
            {
                tags = tags.Take(20).ToList();
            }

            if (tags.Count == 0)
            {
                tags = null;
                metadata = null;
            }
        }

        return new LettermintSendRequest
        {
            From = fromAddress,
            To = [message.To],
            Subject = message.Subject,
            Html = string.IsNullOrEmpty(message.BodyHtml) ? null : message.BodyHtml,
            Text = string.IsNullOrEmpty(message.BodyText) ? null : message.BodyText,
            ReplyTo = string.IsNullOrWhiteSpace(message.ReplyTo) ? null : [message.ReplyTo.Trim()],
            Headers = headers,
            Metadata = metadata,
            Tags = tags,
            Attachments = message.Attachments is { Count: > 0 }
                ? message.Attachments
                    .Where(attachment => attachment.Content.Length > 0 && !string.IsNullOrWhiteSpace(attachment.FileName))
                    .Select(attachment => new LettermintAttachment
                    {
                        Filename = attachment.FileName,
                        Content = Convert.ToBase64String(attachment.Content),
                        ContentType = string.IsNullOrWhiteSpace(attachment.ContentType)
                            ? "application/octet-stream"
                            : attachment.ContentType
                    })
                    .ToList()
                : null,
            Settings = new LettermintSettings()
        };
    }

    public static string FormatError(int statusCode, string body, string? apiKey = null)
    {
        var detail = Redact(Truncate(body.Replace('\n', ' ').Trim(), 180), apiKey);
        if (statusCode is 401 or 403)
        {
            return
                $"Lettermint weigert de aanvraag ({statusCode}). Controleer de API-sleutel en of het afzenderdomein " +
                $"geverifieerd is. {detail}";
        }

        if (statusCode == 422)
        {
            return $"Lettermint wijst het bericht af (422). Vaak: afzender niet geverifieerd of een veld klopt niet. {detail}";
        }

        if (statusCode == 429)
        {
            return $"Lettermint vraagt om even te wachten (429). {detail}";
        }

        return $"Lettermint gaf {statusCode}. {detail}";
    }

    private static string Redact(string value, string? apiKey)
    {
        var redacted = value;
        if (!string.IsNullOrWhiteSpace(apiKey))
        {
            redacted = redacted.Replace(apiKey, "***", StringComparison.Ordinal);
        }

        try
        {
            return EmailInText.Replace(redacted, match => EmailMask.Mask(match.Value));
        }
        catch (RegexMatchTimeoutException)
        {
            return redacted;
        }
    }

    private static string Truncate(string value, int max)
        => value.Length <= max ? value : value[..max] + "…";

    public sealed class LettermintSendRequest
    {
        [JsonPropertyName("from")]
        public required string From { get; init; }

        [JsonPropertyName("to")]
        public required string[] To { get; init; }

        [JsonPropertyName("subject")]
        public required string Subject { get; init; }

        [JsonPropertyName("html")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? Html { get; init; }

        [JsonPropertyName("text")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? Text { get; init; }

        [JsonPropertyName("reply_to")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string[]? ReplyTo { get; init; }

        [JsonPropertyName("headers")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public Dictionary<string, string>? Headers { get; init; }

        [JsonPropertyName("metadata")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public Dictionary<string, string>? Metadata { get; init; }

        [JsonPropertyName("tags")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public List<LettermintTag>? Tags { get; init; }

        [JsonPropertyName("attachments")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public List<LettermintAttachment>? Attachments { get; init; }

        [JsonPropertyName("settings")]
        public required LettermintSettings Settings { get; init; }
    }

    public sealed class LettermintAttachment
    {
        [JsonPropertyName("filename")]
        public required string Filename { get; init; }

        [JsonPropertyName("content")]
        public required string Content { get; init; }

        [JsonPropertyName("content_type")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? ContentType { get; init; }
    }

    public sealed class LettermintTag
    {
        [JsonPropertyName("name")]
        public required string Name { get; init; }

        [JsonPropertyName("value")]
        public required string Value { get; init; }
    }

    public sealed class LettermintSettings
    {
        [JsonPropertyName("track_opens")]
        public bool TrackOpens { get; init; }

        [JsonPropertyName("track_clicks")]
        public bool TrackClicks { get; init; }
    }
}
