using System.Net.Http.Headers; using System.Net.Http.Json; using Jobsy.Web.Auth; using Jobsy.Web.Models; using Jobsy.Web.Profile; using Jobsy.Web.Security; using Microsoft.AspNetCore.Antiforgery;
namespace Jobsy.Web.Hosting;
public static class CandidateProfileBffEndpoints {
  public const string HttpClientName = "JobsyCandidateProfileBff";
  public const string PersonalPath = "/candidate/profile/sections/personal";
  public const string PreferencesPath = "/candidate/profile/sections/preferences";
  public const string AvailabilityPath = "/candidate/profile/sections/availability";
  public const string ExperiencePath = "/candidate/profile/sections/experience";
  public static IEndpointRouteBuilder MapCandidateProfileBffEndpoints(this IEndpointRouteBuilder app) {
    Map(app, PersonalPath, CandidateProfileFormApply.ApplyPersonal);
    Map(app, PreferencesPath, CandidateProfileFormApply.ApplyPreferences);
    Map(app, AvailabilityPath, CandidateProfileFormApply.ApplyAvailability);
    Map(app, ExperiencePath, CandidateProfileFormApply.ApplyExperience); return app; }
  static void Map(IEndpointRouteBuilder app, string path, Func<IFormCollection, MeProfile, (bool,string?)> apply) {
    app.MapPost(path, async (HttpContext http, IAntiforgery antiforgery, IHttpClientFactory hf, JobsyAccessTokenIssuer tokens) => {
      var section = CandidateProfileSectionUrls.SectionFromPath(path);
      var form = await http.Request.ReadFormAsync(http.RequestAborted);
      if (!await antiforgery.IsRequestValidAsync(http)) return Results.Redirect(CandidateProfileSectionUrls.Page(section, error:true));
      var jwt = tokens.TryCreate(http.User, TrustedClientIp.Resolve(http));
      if (string.IsNullOrWhiteSpace(jwt)) return Results.Redirect(CandidateProfileSectionUrls.Page(section, error:true));
      var client = hf.CreateClient(HttpClientName); client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", jwt);
      var profile = await client.GetFromJsonAsync<MeProfile>("api/me/profile", http.RequestAborted);
      if (profile is null) return Results.Redirect(CandidateProfileSectionUrls.Page(section, error:true));
      var (ok,_) = apply(form, profile); if (!ok) return Results.Redirect(CandidateProfileSectionUrls.Page(section, error:true));
      using var put = new HttpRequestMessage(HttpMethod.Put, "api/me/profile") { Content = JsonContent.Create(CandidateProfileFormApply.ToPutPayload(profile)) };
      using var resp = await client.SendAsync(put, http.RequestAborted);
      return resp.IsSuccessStatusCode ? Results.Redirect(CandidateProfileSectionUrls.Page(section, saved:true)) : Results.Redirect(CandidateProfileSectionUrls.Page(section, error:true));
    }).RequireAuthorization().WithOrder(-1000); }
}
