using System.Reflection;
using System.Text.Json;
using Jobsy.Api.Controllers;
using Jobsy.Core.Interfaces;
using Jobsy.Core.Privacy;
using Jobsy.Web.Admin;

namespace Jobsy.Tests;

public class AdminRun6Tests
{
    [Fact]
    public void Every_platform_feature_field_is_audited_except_maintenance_and_session_version()
    {
        var method = typeof(SettingsController).GetMethod(
            "CollectPlatformFeatureChanges",
            BindingFlags.NonPublic | BindingFlags.Static);
        Assert.NotNull(method);

        var before = new PlatformFeatureSnapshot(true, false, "https://lobsy.nl", DateTime.UtcNow, FreePublishUntil: new DateOnly(2026, 1, 1));
        var included = typeof(PlatformFeatureSnapshot).GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(p => IsAuditedType(p.PropertyType))
            .Where(p => p.Name is not ("UpdatedAtUtc" or "MinimumSessionVersion"))
            .Where(p => !p.Name.StartsWith("Maintenance", StringComparison.Ordinal))
            .ToList();
        Assert.Contains(included, p => p.Name == "EmployersEnabled");
        Assert.Contains(included, p => p.Name == "CandidatePassportEnabled");

        foreach (var prop in included)
        {
            var after = Clone(before);
            prop.SetValue(after, Flip(prop, prop.GetValue(before)));
            var changes = (System.Collections.IEnumerable)method!.Invoke(null, [before, after])!;
            var fields = changes.Cast<object>()
                .Select(row => row.GetType().GetField("Item1")!.GetValue(row)?.ToString())
                .ToList();
            Assert.Contains(prop.Name, fields);
        }
    }

    [Fact]
    public void Company_form_never_sends_a_dash_and_shows_nederland()
    {
        Assert.Equal("Nederland", CompanyFormFields.DisplayCountry("NL"));
        Assert.Equal("Nederland", CompanyFormFields.DisplayCountry(null));
        Assert.Equal("NL", CompanyFormFields.CountryToStore("Nederland"));
        Assert.Equal(CompanyFormFields.IbanExample, CompanyFormFields.IbanPlaceholder("—"));
        Assert.Equal(CompanyFormFields.IbanExample, CompanyFormFields.IbanPlaceholder(null));
        Assert.Null(CompanyFormFields.IbanToSave("—"));
        Assert.Null(CompanyFormFields.IbanToSave(""));
        Assert.Null(CompanyFormFields.IbanToSave("NL00 **** 6789"));
        Assert.Equal("NL00KNAB0123456789", CompanyFormFields.IbanToSave("NL00KNAB0123456789"));
        Assert.False(IbanMasking.IsFullIbanInput("—"));
    }

    [Fact]
    public void Raw_admin_values_map_to_dutch_labels()
    {
        string Text(string key) => key switch
        {
            "AdminAction.Resource.PupilCode" => "Leerlingcode",
            "AdminAction.Resource.ApplicationCv" => "CV van sollicitatie",
            "AdminDataAccess.Action.View" => "Bekeken",
            "AdminDataAccess.Action.Pdf" => "PDF gemaakt",
            "AdminDataAccess.Action.Download" => "Gedownload",
            "TokenSpendCost.Extend" => "Verlengen",
            "TokenSpendCost.ContactUnlock" => "Contact ontgrendelen",
            "TokenSpendCost.InsightsUnlock" => "Kandidaatinzichten ontgrendelen",
            "AdminLogs.Message.SourceFailedHttp" => "Bron niet bereikbaar: {0} ({1})",
            "AdminLogs.Message.SourceFailedDns" => "Bron niet bereikbaar: {0} (DNS-fout)",
            _ => key
        };

        Assert.Equal("Leerlingcode", AdminActionLabels.Resource("school.pupil-code", Text));
        Assert.Equal("CV van sollicitatie", AdminActionLabels.Resource("application.cv.download", Text));
        Assert.Equal("Bekeken", AdminActionLabels.AccessAction("user", "view", Text));
        Assert.Equal("PDF gemaakt", AdminActionLabels.AccessAction("user", "pdf", Text));
        Assert.Equal("Gedownload", AdminActionLabels.AccessAction("user", "download", Text));
        Assert.Contains("Add(\"TokenSpendCost.Extend\", \"Verlengen\"", File.ReadAllText(Path.Combine(FindRoot(), "Jobsy.Web", "Localization", "UiStringsAdmin.cs")), StringComparison.Ordinal);
        Assert.Contains("Add(\"TokenSpendCost.ContactUnlock\", \"Contact ontgrendelen\"", File.ReadAllText(Path.Combine(FindRoot(), "Jobsy.Web", "Localization", "UiStringsAdmin.cs")), StringComparison.Ordinal);
        Assert.Contains("Add(\"TokenSpendCost.InsightsUnlock\", \"Kandidaatinzichten ontgrendelen\"", File.ReadAllText(Path.Combine(FindRoot(), "Jobsy.Web", "Localization", "UiStringsAdmin.cs")), StringComparison.Ordinal);
        Assert.Equal("Bron niet bereikbaar: jobs.example (404)", AdminActionLabels.LogMessage("Source failed jobs.example (HTTP 404)", Text));
        Assert.Equal("Bron niet bereikbaar: jobs.example (DNS-fout)", AdminActionLabels.LogMessage("Source failed jobs.example (DNS)", Text));

        var admin = File.ReadAllText(Path.Combine(FindRoot(), "Jobsy.Web", "Localization", "UiStringsAdmin.cs"));
        Assert.Contains("De vaste PushBom-kost geldt alleen als er geen passende tier is.", admin, StringComparison.Ordinal);
        Assert.DoesNotContain("de flat PushBom-kost", admin, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Gegevensinzage_pickers_are_anchored_inside_the_filter_bar()
    {
        var razor = File.ReadAllText(Path.Combine(FindRoot(), "Jobsy.Web", "Components", "Pages", "Admin", "PersonalDataAccessLogAdmin.razor"));
        Assert.Equal(2, Count(razor, "class=\"admin-user-picker\""));
        Assert.DoesNotContain("admin-user-picker__list--inline", razor, StringComparison.Ordinal);
        Assert.DoesNotContain("QueryChanged", razor, StringComparison.Ordinal);
        var css = File.ReadAllText(Path.Combine(FindRoot(), "Jobsy.Web", "wwwroot", "css", "features", "admin.css"));
        Assert.Contains(".admin-user-picker > .admin-filter-input", css, StringComparison.Ordinal);
        Assert.Contains("width: 100%", css, StringComparison.Ordinal);
    }

    [Fact]
    public void WhatsApp_confirms_on_and_employers_confirm_both_ways()
    {
        var whatsApp = PlatformSettingsCatalog.Entries.Single(e => e.Key == "WhatsAppRemindersEnabled");
        var employers = PlatformSettingsCatalog.Entries.Single(e => e.Key == "EmployersEnabled");
        Assert.Equal(PlatformSettingConfirmWhen.On, whatsApp.ConfirmWhen);
        Assert.True(whatsApp.ConfirmOnChange);
        Assert.Equal(PlatformSettingConfirmWhen.Both, employers.ConfirmWhen);
    }

    [Fact]
    public void Referee_mail_names_the_workplace_and_has_its_own_group()
    {
        var mail = File.ReadAllText(Path.Combine(FindRoot(), "Jobsy.Core", "Email", "Localization", "EmailStringsReferee.cs"));
        Assert.Contains("{0} zegt bij {1} als {2} te hebben gewerkt", mail, StringComparison.Ordinal);
        var groups = File.ReadAllText(Path.Combine(FindRoot(), "Jobsy.Web", "Components", "Pages", "Admin", "MailTestAdmin.razor"));
        var referee = groups.IndexOf("MailTestGroupReferee", StringComparison.Ordinal);
        var intern = groups.IndexOf("MailTestGroupInternal", StringComparison.Ordinal);
        Assert.True(referee >= 0 && referee < intern);
        var registry = File.ReadAllText(Path.Combine(FindRoot(), "Jobsy.Core", "Email", "EmailTemplateRegistry.cs"));
        Assert.Contains("Kom terug — 4 weken", registry, StringComparison.Ordinal);
        Assert.Contains("Kom terug — 4 korte tests", registry, StringComparison.Ordinal);
    }

    private static int Count(string text, string needle)
    {
        var n = 0;
        var i = 0;
        while ((i = text.IndexOf(needle, i, StringComparison.Ordinal)) >= 0)
        {
            n++;
            i += needle.Length;
        }

        return n;
    }

    private static bool IsAuditedType(Type type)
    {
        var t = Nullable.GetUnderlyingType(type) ?? type;
        return t == typeof(bool) || t == typeof(int) || t == typeof(string)
            || t == typeof(DateOnly) || t == typeof(DateTime);
    }

    private static PlatformFeatureSnapshot Clone(PlatformFeatureSnapshot source)
        => JsonSerializer.Deserialize<PlatformFeatureSnapshot>(JsonSerializer.Serialize(source))!;

    private static object? Flip(PropertyInfo prop, object? current)
    {
        var type = Nullable.GetUnderlyingType(prop.PropertyType) ?? prop.PropertyType;
        if (type == typeof(bool))
        {
            return current is not true;
        }

        if (type == typeof(int))
        {
            return current is int i ? i + 1 : 1;
        }

        if (type == typeof(string))
        {
            return (current as string ?? "") + "x";
        }

        if (type == typeof(DateOnly))
        {
            return current is DateOnly d ? d.AddDays(1) : new DateOnly(2026, 2, 2);
        }

        if (type == typeof(DateTime))
        {
            return current is DateTime dt ? dt.AddMinutes(1) : DateTime.UtcNow;
        }

        throw new InvalidOperationException(prop.Name);
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
