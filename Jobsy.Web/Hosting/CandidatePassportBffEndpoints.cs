using System.Net.Http.Headers;
using System.Net.Http.Json;
using Jobsy.Web.Auth;
using Jobsy.Web.Models;
using Jobsy.Web.Navigation;
using Jobsy.Web.Security;
using Microsoft.AspNetCore.Antiforgery;

namespace Jobsy.Web.Hosting;

public static class CandidatePassportBffEndpoints
{
    public const string MotivationPath = "/candidate/paspoort/sections/motivation";
    public const string HttpClientName = "JobsyCandidatePassportBff";

    public static IEndpointRouteBuilder MapCandidatePassportBffEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapPost(MotivationPath, async (
            HttpContext http,
            IAntiforgery antiforgery,
            IHttpClientFactory httpClientFactory,
            JobsyAccessTokenIssuer accessTokens) =>
        {
            if (http.User.Identity?.IsAuthenticated != true)
            {
                return Results.Redirect("/login?returnUrl=" + Uri.EscapeDataString("/candidate/paspoort?tab=data&section=motivation"));
            }

            var form = await http.Request.ReadFormAsync(http.RequestAborted);
            if (!await antiforgery.IsRequestValidAsync(http))
            {
                return Results.Redirect(PassportRedirects.BuildPassportDataSectionUrl("motivation", error: true));
            }

            var jwt = accessTokens.TryCreate(http.User, TrustedClientIp.Resolve(http));
            if (string.IsNullOrWhiteSpace(jwt))
            {
                return Results.Redirect(PassportRedirects.BuildPassportDataSectionUrl("motivation", error: true));
            }

            var client = httpClientFactory.CreateClient(HttpClientName);
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", jwt);

            MeProfile? profile;
            try
            {
                profile = await client.GetFromJsonAsync<MeProfile>("api/me/profile", http.RequestAborted);
            }
            catch
            {
                profile = null;
            }

            if (profile is null)
            {
                return Results.Redirect(PassportRedirects.BuildPassportDataSectionUrl("motivation", error: true));
            }

            var aboutMe = form["about_me"].ToString();
            var defaultMotivation = form["default_motivation"].ToString();
            profile.Preferences ??= new CandidatePreferences();
            profile.Preferences.AboutMe = string.IsNullOrWhiteSpace(aboutMe) ? null : aboutMe.Trim();
            profile.Preferences.DefaultMotivation = string.IsNullOrWhiteSpace(defaultMotivation)
                ? null
                : defaultMotivation.Trim();

            var payload = new
            {
                openForWork = profile.OpenForWork,
                dateOfBirth = profile.DateOfBirth,
                preferences = profile.Preferences,
                homeLatitude = profile.HomeLatitude,
                homeLongitude = profile.HomeLongitude,
                clearHomeLocation = false,
                firstName = profile.FirstName,
                lastName = profile.LastName,
                phoneNumber = profile.PhoneNumber ?? "",
                whatsAppContactAllowed = profile.WhatsAppContactAllowed,
                references = profile.References,
                availableFromDate = profile.AvailableFromDate,
                clearAvailableFromDate = false
            };

            using var put = new HttpRequestMessage(HttpMethod.Put, "api/me/profile")
            {
                Content = JsonContent.Create(payload)
            };
            using var response = await client.SendAsync(put, http.RequestAborted);
            if (!response.IsSuccessStatusCode)
            {
                return Results.Redirect(PassportRedirects.BuildPassportDataSectionUrl("motivation", error: true));
            }

            return Results.Redirect(PassportRedirects.BuildPassportDataSectionUrl("motivation", saved: true));
        }).RequireAuthorization().WithOrder(-1000);

        return app;
    }
}
