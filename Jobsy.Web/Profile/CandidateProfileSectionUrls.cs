using Jobsy.Web.Hosting;
namespace Jobsy.Web.Profile;
public static class CandidateProfileSectionUrls {
  public const string SectionQueryKey = "profileSection";
  public static string SectionFromPath(string path) => path switch {
    CandidateProfileBffEndpoints.PersonalPath => "personal",
    CandidateProfileBffEndpoints.PreferencesPath => "preferences",
    CandidateProfileBffEndpoints.AvailabilityPath => "availability",
    CandidateProfileBffEndpoints.ExperiencePath => "experience", _ => "personal" };
  public static string Page(string section, bool saved=false, bool error=false) {
    var q = $"/candidate/profile?tab=profile&{SectionQueryKey}={Uri.EscapeDataString(section)}";
    if (saved) q += "&profileSaved=1"; if (error) q += "&profileError=1"; return q; }
  public static string NavHref(string section, string? returnUrl) {
    var href = Page(section); if (!string.IsNullOrWhiteSpace(returnUrl)) href += "&returnUrl="+Uri.EscapeDataString(returnUrl); return href; }
}
