using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using Jobsy.Core.Email;
using Jobsy.Core.Email.Localization;
using Jobsy.Core.Features;
using Jobsy.Core.Options;
using Jobsy.Core.Reminders;
using Jobsy.Core.Rules;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Jobsy.Infrastructure.Reminders;

/// <summary>
/// WhatsApp Business Cloud API. Sends nothing when the platform flag is off or
/// the phone-number id / access token are missing.
/// </summary>
public sealed partial class WhatsAppReminderChannel(
    IHttpClientFactory httpClientFactory,
    IOptions<WhatsAppReminderOptions> options,
    IFeatureFlags features,
    ILogger<WhatsAppReminderChannel> logger) : IReminderChannel
{
    public const string HttpClientName = "WhatsAppCloud";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    public string Name => "whatsapp";

    public async Task<ReminderChannelResult> SendAsync(
        ReminderDispatch dispatch,
        CancellationToken cancellationToken = default)
    {
        if (!dispatch.SendWhatsApp)
        {
            return new ReminderChannelResult(false, "whatsapp-off");
        }

        if (!await features.IsEnabledAsync(PlatformFeature.WhatsAppReminders, cancellationToken))
        {
            return new ReminderChannelResult(false, "flag-off");
        }

        var settings = options.Value;
        if (!settings.IsConfigured)
        {
            return new ReminderChannelResult(false, "config-missing");
        }

        var phoneNumberId = settings.PhoneNumberId!.Trim();
        var token = settings.AccessToken!.Trim();
        var version = string.IsNullOrWhiteSpace(settings.GraphVersion) ? "v21.0" : settings.GraphVersion.Trim();
        if (!PhoneNumberIdPattern().IsMatch(phoneNumberId) || !GraphVersionPattern().IsMatch(version))
        {
            logger.LogWarning("WhatsApp reminder skipped: phone number id or graph version is not safe to put in a URL.");
            return new ReminderChannelResult(false, "config-invalid");
        }

        var to = CandidatePhoneRules.ToWhatsAppE164Digits(dispatch.WhatsAppPhone);
        if (to is null)
        {
            return new ReminderChannelResult(false, "phone-invalid");
        }

        var tests = string.Equals(dispatch.Kind, ComebackReminderKinds.BasicTests, StringComparison.Ordinal);
        var prefix = tests ? "Email.ComebackTests" : "Email.ComebackLookAgain";
        var body = EmailStrings.Get(dispatch.Culture, prefix + ".WhatsApp");
        var payload = JsonSerializer.Serialize(new WaRequest(to, new WaText(body)), JsonOptions);

        var client = httpClientFactory.CreateClient(HttpClientName);
        using var request = new HttpRequestMessage(
            HttpMethod.Post,
            $"https://graph.facebook.com/{version}/{phoneNumberId}/messages")
        {
            Content = new StringContent(payload, Encoding.UTF8, "application/json")
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        using var response = await client.SendAsync(request, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            logger.LogWarning(
                "WhatsApp reminder failed for user {UserId} with status {Status}.",
                dispatch.UserId,
                (int)response.StatusCode);
            return new ReminderChannelResult(false, "http-" + (int)response.StatusCode);
        }

        logger.LogInformation("WhatsApp reminder sent for user {UserId}.", dispatch.UserId);
        return new ReminderChannelResult(true);
    }

    [GeneratedRegex(@"^\d{1,32}$")]
    private static partial Regex PhoneNumberIdPattern();

    [GeneratedRegex(@"^v\d{1,3}(\.\d{1,3})?$")]
    private static partial Regex GraphVersionPattern();

    private sealed record WaRequest(
        [property: JsonPropertyName("to")] string To,
        [property: JsonPropertyName("text")] WaText Text)
    {
        [JsonPropertyName("messaging_product")]
        public string MessagingProduct { get; } = "whatsapp";

        [JsonPropertyName("type")]
        public string Type { get; } = "text";
    }

    private sealed record WaText(
        [property: JsonPropertyName("body")] string Body)
    {
        [JsonPropertyName("preview_url")]
        public bool PreviewUrl { get; } = false;
    }
}
