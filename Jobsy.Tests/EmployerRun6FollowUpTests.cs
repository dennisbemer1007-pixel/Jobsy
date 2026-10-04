using System.Net;
using System.Reflection;
using System.Text;
using Jobsy.Core.Admin;
using Jobsy.Core.Enums;
using Jobsy.Core.Rules;
using Jobsy.Web.Admin;
using Jobsy.Web.Localization;
using Jobsy.Web.Navigation;
using Jobsy.Web.Services;

namespace Jobsy.Tests;

public class EmployerRun6FollowUpTests
{
    [Fact]
    public void Admin_may_purge_an_active_vacancy_without_applications()
    {
        Assert.True(VacancyDeletionRules.AdminMayPurge(VacancyStatus.Active, 0));
        Assert.True(VacancyDeletionRules.AdminMayPurge(VacancyStatus.Active, 2));
        Assert.True(VacancyDeletionRules.AdminMayPurge(VacancyStatus.PendingApproval, 0));
        Assert.False(VacancyDeletionRules.EmployerMayDelete(VacancyStatus.Active, 0));
        Assert.False(VacancyDeletionRules.AdminMayDeleteWithoutApplications(VacancyStatus.Active, 0));
        Assert.False(VacancyDeletionRules.AdminMayPurgeWithApplications(0));
    }

    [Fact]
    public async Task Delete_not_allowed_keeps_the_offline_sentence()
    {
        var response = new HttpResponseMessage(HttpStatusCode.BadRequest)
        {
            Content = new StringContent(
                """{"code":"vacancy_delete_not_allowed","message":"Haal de vacature eerst offline.","userMessage":true}""",
                Encoding.UTF8,
                "application/json")
        };

        var error = await ApiErrorException.FromResponseAsync(response);
        Assert.Equal("vacancy_delete_not_allowed", error.Code);
        Assert.Equal("Haal de vacature eerst offline.", error.UserMessage);
        Assert.Equal("AdminVacancy.DeleteOffline", UserFacingError.MessageKeyFor(error));
    }

    [Fact]
    public void Admin_vacancy_delete_error_is_an_alert()
    {
        var razor = File.ReadAllText(Path.Combine(
            FindRoot(),
            "Jobsy.Web/Components/Admin/Sections/VacanciesAdminSection.razor"));
        Assert.Contains("role=\"@(_messageIsError ? \"alert\" : \"status\")\"", razor, StringComparison.Ordinal);
        Assert.Contains("data-testid=\"admin-vacancy-message\"", razor, StringComparison.Ordinal);
        Assert.Contains("UserFacing.Describe(ex)", razor, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData(true, false, false, false, false, "/candidate/profile?")]
    [InlineData(false, true, false, false, false, "tab=proof")]
    [InlineData(false, false, true, false, false, "item=education")]
    [InlineData(false, false, false, true, false, "/candidate/profile?")]
    [InlineData(false, false, false, false, true, "item=references")]
    public void Profiel_aanvullen_follows_the_first_missing_item(
        bool missingAvailability,
        bool missingExperience,
        bool missingEducation,
        bool missingAbout,
        bool missingReferences,
        string expected)
    {
        var id = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee");
        var href = ApplyProfileLink.Href(id,
        [
            new ApplyProfileLink.Gap("availability", missingAvailability),
            new ApplyProfileLink.Gap(ApplyProfileLink.Experience, missingExperience),
            new ApplyProfileLink.Gap(ApplyProfileLink.Education, missingEducation),
            new ApplyProfileLink.Gap("about", missingAbout),
            new ApplyProfileLink.Gap(ApplyProfileLink.References, missingReferences)
        ]);

        Assert.Contains(expected, href, StringComparison.Ordinal);
        if (expected.Contains("proof", StringComparison.Ordinal) || expected.StartsWith("item=", StringComparison.Ordinal))
        {
            Assert.Contains("/candidate/paspoort?", href, StringComparison.Ordinal);
            Assert.Contains("returnUrl=", href, StringComparison.Ordinal);
        }
    }

    [Fact]
    public void Every_api_audit_action_has_a_label_in_all_locales()
    {
        var nl = new Dictionary<string, string>(StringComparer.Ordinal);
        var en = new Dictionary<string, string>(StringComparer.Ordinal);
        var pl = new Dictionary<string, string>(StringComparer.Ordinal);
        var ro = new Dictionary<string, string>(StringComparer.Ordinal);
        var ar = new Dictionary<string, string>(StringComparer.Ordinal);
        UiStringsAdmin.MergeAll(nl, en, pl, ro, ar);

        var keys = typeof(AdminAuditKeys)
            .GetFields(BindingFlags.Public | BindingFlags.Static)
            .Where(field => field.IsLiteral && field.FieldType == typeof(string))
            .Select(field => (string)field.GetRawConstantValue()!)
            .ToList();

        Assert.Contains(AdminAuditKeys.VacancyInactive, keys);
        var missing = new List<string>();
        foreach (var key in keys)
        {
            foreach (var catalog in new[] { nl, en, pl, ro, ar })
            {
                var label = AdminActionLabels.Label(key, cultureKey =>
                    catalog.TryGetValue(cultureKey, out var text) ? text : cultureKey);
                if (string.IsNullOrWhiteSpace(label)
                    || string.Equals(label, key, StringComparison.Ordinal)
                    || label.StartsWith("AdminAudit.Action.", StringComparison.Ordinal))
                {
                    missing.Add(key + " → " + label);
                }
            }
        }

        Assert.True(missing.Count == 0, string.Join(", ", missing.Distinct()));
        Assert.Contains(AdminAuditKeys.VacancyPurged, keys);
        Assert.Equal("Vacature offline gehaald", AdminActionLabels.Label(AdminAuditKeys.VacancyInactive, key => nl[key]));
        Assert.Equal("Vacancy taken offline", AdminActionLabels.Label(AdminAuditKeys.VacancyInactive, key => en[key]));
        Assert.Equal("Oferta wyłączona", AdminActionLabels.Label(AdminAuditKeys.VacancyInactive, key => pl[key]));
        Assert.Equal("Job scos offline", AdminActionLabels.Label(AdminAuditKeys.VacancyInactive, key => ro[key]));
        Assert.Equal("أُوقفت الوظيفة", AdminActionLabels.Label(AdminAuditKeys.VacancyInactive, key => ar[key]));
        Assert.Equal("Vacature definitief verwijderd", AdminActionLabels.Label(AdminAuditKeys.VacancyPurged, key => nl[key]));
        Assert.Equal("Vacancy permanently deleted", AdminActionLabels.Label(AdminAuditKeys.VacancyPurged, key => en[key]));
        Assert.Equal("Oferta trwale usunięta", AdminActionLabels.Label(AdminAuditKeys.VacancyPurged, key => pl[key]));
        Assert.Equal("Job șters definitiv", AdminActionLabels.Label(AdminAuditKeys.VacancyPurged, key => ro[key]));
        Assert.Equal("حُذفت الوظيفة نهائياً", AdminActionLabels.Label(AdminAuditKeys.VacancyPurged, key => ar[key]));
    }

    private static string FindRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null)
        {
            if (File.Exists(Path.Combine(dir.FullName, "Jobsy.sln")))
            {
                return dir.FullName;
            }

            dir = dir.Parent;
        }

        throw new InvalidOperationException("Jobsy.sln not found.");
    }
}
