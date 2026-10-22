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

/// <summary>Full-page POST for onboarding steps 1–2 (303 redirect, no Blazor circuit).</summary>
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
            return SeeOther(WithError(OnboardingRoutes.StepUrl(step ?? 1), culture["Common.Error.TryAgain"]));
        }

        var currentStep = step is >= 1 and <= 2 ? step.Value : 1;
        if (string.IsNullOrWhiteSpace(obPayload))
        {
            return SeeOther(WithError(OnboardingRoutes.StepUrl(currentStep), culture["Onboarding.NotSaved"]));
        }

        JsonElement body;
        try
        {
            using var doc = JsonDocument.Parse(obPayload);
            body = doc.RootElement.Clone();
        }
        catch (JsonException)
        {
            return SeeOther(WithError(OnboardingRoutes.StepUrl(currentStep), culture["Onboarding.NotSaved"]));
        }

        var advance = string.Equals(obNav, "next", StringComparison.OrdinalIgnoreCase);
        var goBack = string.Equals(obNav, "back", StringComparison.OrdinalIgnoreCase);
        if (!advance && !goBack)
        {
            return SeeOther(WithError(OnboardingRoutes.StepUrl(currentStep), culture["Onboarding.NotSaved"]));
        }

        var merged = MergeNavFlags(body, advance, goBack);

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

    private static JsonElement MergeNavFlags(JsonElement body, bool advance, bool goBack)
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
            writer.WriteBoolean("skipOptional", false);
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
