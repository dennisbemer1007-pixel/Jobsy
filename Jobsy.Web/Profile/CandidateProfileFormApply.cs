using System.Globalization;
using Jobsy.Core.Rules;
using Jobsy.Web.Components.Candidate.ProfileSections;
using Jobsy.Web.Models;
namespace Jobsy.Web.Profile;
public static class CandidateProfileFormApply {
  public static object ToPutPayload(MeProfile profile) => new {
    openForWork = profile.OpenForWork, dateOfBirth = profile.DateOfBirth, preferences = profile.Preferences,
    homeLatitude = profile.HomeLatitude, homeLongitude = profile.HomeLongitude, clearHomeLocation = false,
    firstName = profile.FirstName, lastName = profile.LastName, phoneNumber = profile.PhoneNumber ?? "",
    whatsAppContactAllowed = profile.WhatsAppContactAllowed, references = profile.References,
    availableFromDate = profile.AvailableFromDate, clearAvailableFromDate = profile.AvailableFromDate is null };
  public static (bool Ok, string? ErrorKey) ApplyPersonal(IFormCollection form, MeProfile profile) {
    profile.FirstName = form["first_name"].ToString().Trim(); profile.LastName = form["last_name"].ToString().Trim();
    profile.Preferences ??= new CandidatePreferences(); profile.Preferences.HomeAddress = form["home_address"].ToString().Trim();
    return (true,null); }
  public static (bool Ok, string? ErrorKey) ApplyPreferences(IFormCollection form, MeProfile profile) {
    profile.Preferences ??= new CandidatePreferences();
    if (int.TryParse(form["max_travel"].ToString(), out var t)) profile.Preferences.MaxTravelMinutes = t; return (true,null); }
  public static (bool Ok, string? ErrorKey) ApplyAvailability(IFormCollection form, MeProfile profile) {
    profile.Preferences ??= new CandidatePreferences();
    if (decimal.TryParse(form["min_hours"].ToString(), out var min)) profile.Preferences.MinHoursPerWeek = min; return (true,null); }
  public static (bool Ok, string? ErrorKey) ApplyExperience(IFormCollection form, MeProfile profile) {
    profile.Preferences ??= new CandidatePreferences(); profile.Preferences.AboutMe = form["about_me"].ToString().Trim(); return (true,null); }
}
