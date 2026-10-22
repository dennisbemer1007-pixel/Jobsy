using System.Text.Json;
using Jobsy.Core.Features;
using Jobsy.Web.Localization;
using Jobsy.Web.Navigation;
using Jobsy.Web.Onboarding;
using Jobsy.Web.Services;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;

namespace Jobsy.Web.Hosting;

/// <summary>Full-page POST for onboarding steps 1–10 (303 redirect, no Blazor circuit).</summary>
public static class OnboardingStartFormEndpoints
{
    private static readonly JsonSerializerOptions Json = new() { PropertyNameCaseInsensitive = true };

    public static IEndpointRouteBuilder MapOnboardingStartFormEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapPost("/candidate/start", PostAsync)
            .RequireAuthorization(new AuthorizeAttribute { Roles = "Candidate" });

        return app;
    }

    private static async Task<IResult> PostAsync(
        HttpContext http,
        [FromForm] string? obNav,
        [FromForm] string? obPayload,
        [FromForm] string? dream,
        [FromForm] string? obDreamPick,
        [FromForm] string? obMiniPick,
        [FromForm] string? consentRequested,
        [FromQuery] int? step,
        IAntiforgery antiforgery,
        JobsyApiClient api,
        IGeocodingClient geocoder,
        IFeatureFlags featureFlags,
        CultureState culture,
        CancellationToken cancellationToken)
    {
        if (!await antiforgery.IsRequestValidAsync(http))
        {
            var fallback = step is >= 1 and <= 10 ? step.Value : 1;
            if (string.Equals(obNav, "begin", StringComparison.OrdinalIgnoreCase))
            {
                return SeeOther(WithError(OnboardingRoutes.ClassicStartPath, culture["Common.Error.TryAgain"]));
            }

            return SeeOther(WithError(OnboardingRoutes.StepUrl(fallback), culture["Common.Error.TryAgain"]));
        }

        if (string.Equals(obNav, "begin", StringComparison.OrdinalIgnoreCase))
        {
            var (beginStatus, beginPayload) = await OnboardingClientStepBffSaver.BeginWelcomeAsync(api, culture, cancellationToken);
            return ToRedirect(beginStatus, beginPayload, 1, advance: true, goBack: false, culture);
        }

        var currentStep = step is >= 1 and <= 10 ? step.Value : 1;

        var advance = string.Equals(obNav, "next", StringComparison.OrdinalIgnoreCase);
        var goBack = string.Equals(obNav, "back", StringComparison.OrdinalIgnoreCase);
        var skipOptional = string.Equals(obNav, "skip", StringComparison.OrdinalIgnoreCase);
        var dreamUnknown = string.Equals(obNav, "dreamUnknown", StringComparison.OrdinalIgnoreCase);
        var persist = string.Equals(obNav, "save", StringComparison.OrdinalIgnoreCase);

        if (!string.IsNullOrWhiteSpace(obDreamPick) && currentStep == 6)
        {
            dream = obDreamPick.Trim();
            persist = true;
            advance = false;
            goBack = false;
        }

        if (!string.IsNullOrWhiteSpace(obMiniPick) && currentStep is >= 7 and <= 10)
        {
            persist = true;
            advance = false;
            goBack = false;
        }

        if (skipOptional || dreamUnknown)
        {
            advance = true;
            skipOptional = true;
        }

        if (!advance && !goBack && !persist)
        {
            return SeeOther(WithError(OnboardingRoutes.StepUrl(currentStep), culture["Onboarding.NotSaved"]));
        }

        JsonElement body;
        if (currentStep == 6)
        {
            body = BuildStep6Body(dream, obPayload);
        }
        else if (currentStep is >= 7 and <= 10)
        {
            body = BuildMiniBlockBody(http, currentStep, consentRequested, obMiniPick, obPayload);
        }
        else if (string.IsNullOrWhiteSpace(obPayload))
        {
            return SeeOther(WithError(OnboardingRoutes.StepUrl(currentStep), culture["Onboarding.NotSaved"]));
        }
        else
        {
            try
            {
                using var doc = JsonDocument.Parse(obPayload);
                body = doc.RootElement.Clone();
            }
            catch (JsonException)
            {
                return SeeOther(WithError(OnboardingRoutes.StepUrl(currentStep), culture["Onboarding.NotSaved"]));
            }
        }

        if (dreamUnknown && currentStep == 6)
        {
            body = ForceEmptyDreamPayload(body);
        }

        var merged = MergeNavFlags(body, advance, goBack, skipOptional);

        if (currentStep == 1)
        {
            var req = JsonSerializer.Deserialize<OnboardingClientStep1Request>(merged.GetRawText(), Json)
                ?? new OnboardingClientStep1Request();
            req.Advance = advance;
            var (status, payload) = await OnboardingClientStep1Saver.SaveAsync(req, api, geocoder, culture, cancellationToken);
            return ToRedirect(status, payload, currentStep, advance, goBack, culture);
        }

        var (stepStatus, stepPayload) = await OnboardingClientStepBffSaver.SaveStepAsync(
            currentStep,
            merged,
            api,
            geocoder,
            featureFlags,
            culture,
            cancellationToken);
        return ToRedirect(stepStatus, stepPayload, currentStep, advance, goBack, culture);
    }

    private static JsonElement BuildMiniBlockBody(
        HttpContext http,
        int step,
        string? consentRequested,
        string? obMiniPick,
        string? obPayload)
    {
        var answers = ReadMiniAnswersFromForm(http.Request.Form);
        if (!string.IsNullOrWhiteSpace(obMiniPick))
        {
            var parts = obMiniPick.Split(':', 2);
            if (parts.Length == 2
                && int.TryParse(parts[0], out var qid)
                && int.TryParse(parts[1], out var score)
                && score is >= 1 and <= 5)
            {
                answers[qid] = score;
            }
        }

        if (answers.Count == 0 && !string.IsNullOrWhiteSpace(obPayload))
        {
            try
            {
                using var doc = JsonDocument.Parse(obPayload);
                foreach (var pair in OnboardingClientStepBridge.ParseMiniAnswers(doc.RootElement.GetRawText()))
                {
                    answers[pair.Key] = pair.Value;
                }

                if (doc.RootElement.TryGetProperty("consentRequested", out var consentProp)
                    && (consentProp.ValueKind == JsonValueKind.True || consentProp.ValueKind == JsonValueKind.False))
                {
                    consentRequested = consentProp.GetBoolean() ? "true" : "false";
                }
            }
            catch (JsonException)
            {
                // fall through
            }
        }

        var consent = step == 7
                      && (string.Equals(consentRequested, "true", StringComparison.OrdinalIgnoreCase)
                          || string.Equals(consentRequested, "on", StringComparison.OrdinalIgnoreCase));

        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream))
        {
            writer.WriteStartObject();
            if (step == 7)
            {
                writer.WriteBoolean("consentRequested", consent);
            }

            writer.WriteStartObject("answers");
            foreach (var (id, value) in answers.OrderBy(static p => p.Key))
            {
                writer.WriteNumber(id.ToString(), value);
            }

            writer.WriteEndObject();
            writer.WriteEndObject();
        }

        stream.Position = 0;
        using var outDoc = JsonDocument.Parse(stream);
        return outDoc.RootElement.Clone();
    }

    private static Dictionary<int, int> ReadMiniAnswersFromForm(IFormCollection form)
    {
        var answers = new Dictionary<int, int>();
        foreach (var key in form.Keys)
        {
            if (string.IsNullOrEmpty(key)
                || !key.StartsWith("miniAnswers[", StringComparison.Ordinal)
                || !key.EndsWith(']'))
            {
                continue;
            }

            var idText = key["miniAnswers[".Length..^1];
            if (!int.TryParse(idText, out var qid))
            {
                continue;
            }

            var raw = form[key].ToString();
            if (int.TryParse(raw, out var score) && score is >= 1 and <= 5)
            {
                answers[qid] = score;
            }
        }

        return answers;
    }

    private static JsonElement BuildStep6Body(string? dream, string? obPayload)
    {
        var trimmed = dream?.Trim() ?? "";
        if (string.IsNullOrEmpty(trimmed) && !string.IsNullOrWhiteSpace(obPayload))
        {
            try
            {
                using var doc = JsonDocument.Parse(obPayload);
                var root = doc.RootElement;
                if (root.TryGetProperty("dream", out var d) && d.ValueKind == JsonValueKind.String)
                {
                    trimmed = d.GetString()?.Trim() ?? "";
                }

                if (string.IsNullOrEmpty(trimmed)
                    && root.TryGetProperty("dreamSearch", out var ds)
                    && ds.ValueKind == JsonValueKind.String)
                {
                    trimmed = ds.GetString()?.Trim() ?? "";
                }
            }
            catch (JsonException)
            {
                // fall through with empty dream
            }
        }

        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream))
        {
            writer.WriteStartObject();
            writer.WriteString("dream", trimmed);
            writer.WriteString("dreamSearch", trimmed);
            writer.WriteEndObject();
        }

        stream.Position = 0;
        using var outDoc = JsonDocument.Parse(stream);
        return outDoc.RootElement.Clone();
    }

    private static JsonElement ForceEmptyDreamPayload(JsonElement body)
    {
        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream))
        {
            writer.WriteStartObject();
            var wroteDream = false;
            var wroteSearch = false;
            foreach (var prop in body.EnumerateObject())
            {
                if (prop.NameEquals("dream"))
                {
                    writer.WriteString("dream", "");
                    wroteDream = true;
                    continue;
                }

                if (prop.NameEquals("dreamSearch"))
                {
                    writer.WriteString("dreamSearch", "");
                    wroteSearch = true;
                    continue;
                }

                writer.WritePropertyName(prop.Name);
                prop.Value.WriteTo(writer);
            }

            if (!wroteDream)
            {
                writer.WriteString("dream", "");
            }

            if (!wroteSearch)
            {
                writer.WriteString("dreamSearch", "");
            }

            writer.WriteEndObject();
        }

        stream.Position = 0;
        using var doc = JsonDocument.Parse(stream);
        return doc.RootElement.Clone();
    }

    private static JsonElement MergeNavFlags(JsonElement body, bool advance, bool goBack, bool skipOptional)
    {
        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream))
        {
            writer.WriteStartObject();
            foreach (var prop in body.EnumerateObject())
            {
                writer.WritePropertyName(prop.Name);
                prop.Value.WriteTo(writer);
            }

            writer.WriteBoolean("advance", advance);
            writer.WriteBoolean("back", goBack);
            writer.WriteBoolean("skipOptional", skipOptional);
            writer.WriteEndObject();
        }

        stream.Position = 0;
        using var doc = JsonDocument.Parse(stream);
        return doc.RootElement.Clone();
    }

    private static RedirectHttpResult ToRedirect(
        int status,
        object payload,
        int currentStep,
        bool advance,
        bool goBack,
        CultureState culture)
    {
        if (status is >= 400)
        {
            var message = ReadJsonString(payload, "message") ?? culture["Onboarding.NotSaved"];
            return SeeOther(WithError(OnboardingRoutes.StepUrl(currentStep), message));
        }

        var redirectUrl = ReadJsonString(payload, "redirectUrl");
        if (!string.IsNullOrWhiteSpace(redirectUrl))
        {
            return SeeOther(redirectUrl);
        }

        if (goBack && currentStep > 1)
        {
            return SeeOther(OnboardingRoutes.StepUrl(currentStep - 1));
        }

        if (advance)
        {
            return SeeOther(OnboardingRoutes.StepUrl(currentStep + 1));
        }

        return SeeOther(OnboardingRoutes.StepUrl(currentStep));
    }

    private static RedirectHttpResult SeeOther(string url)
        => TypedResults.Redirect(url, permanent: false, preserveMethod: false);

    private static string? ReadJsonString(object payload, string property)
    {
        var json = JsonSerializer.Serialize(payload, Json);
        using var doc = JsonDocument.Parse(json);
        return doc.RootElement.TryGetProperty(property, out var prop) ? prop.GetString() : null;
    }

    private static string WithError(string url, string message)
    {
        var sep = url.Contains('?', StringComparison.Ordinal) ? "&" : "?";
        return url + sep + "obError=" + Uri.EscapeDataString(message);
    }
}
