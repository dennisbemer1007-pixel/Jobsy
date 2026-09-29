using System.Reflection;
using Jobsy.Core.Admin;
using Jobsy.Core.Entities;
using Jobsy.Core.Interfaces;
using Jobsy.Core.Rules;

namespace Jobsy.Web.Admin;

public enum PlatformSettingKind
{
    Bool,
    Int,
    Date,
    Text,
    Policy
}

public enum PlatformSettingImpactLevel
{
    None,
    Warn,
    Danger
}

public enum PlatformSettingEnvironmentLock
{
    None,
    AcceptatieOnly
}

/// <summary>
/// One catalog for platform feature / general settings rows (D7).
/// </summary>
public sealed record PlatformSettingDescriptor(
    string Key,
    string Group,
    string TitleKey,
    string DescriptionKey,
    PlatformSettingKind Kind,
    Func<PlatformFeatureSnapshot, object?> Read,
    Func<object?, PlatformFeatureUpdate> Write,
    string? ImpactKey = null,
    PlatformSettingImpactLevel ImpactLevel = PlatformSettingImpactLevel.None,
    bool ConfirmOnChange = false,
    PlatformSettingEnvironmentLock EnvironmentLock = PlatformSettingEnvironmentLock.None,
    bool ShowOnDashboard = false,
    int? Min = null,
    int? Max = null,
    string? UnitKey = null,
    string? BadgeKey = null);

public static class PlatformSettingsCatalog
{
    public const string GroupPlatformMode = "platform-mode";
    public const string GroupVacancies = "vacancies";
    public const string GroupSecurity = "security";
    public const string GroupDemo = "demo";
    public const string GroupGeneral = "general";

    public static readonly IReadOnlyList<(string Key, string TitleKey, string DescriptionKey)> Groups =
    [
        (GroupPlatformMode, "AdminSettings.Group.PlatformMode", "AdminSettings.Group.PlatformMode.Desc"),
        (GroupVacancies, "AdminSettings.Group.Vacancies", "AdminSettings.Group.Vacancies.Desc"),
        (GroupSecurity, "AdminSettings.Group.Security", "AdminSettings.Group.Security.Desc"),
        (GroupDemo, "AdminSettings.Group.Demo", "AdminSettings.Group.Demo.Desc"),
        (GroupGeneral, "AdminSettings.Group.General", "AdminSettings.Group.General.Desc"),
    ];

    /// <summary>Functies page groups (excludes Algemeen).</summary>
    public static readonly IReadOnlyList<string> FeaturesGroupKeys =
    [
        GroupPlatformMode, GroupVacancies, GroupSecurity, GroupDemo
    ];

    public static readonly IReadOnlyList<string> GeneralGroupKeys = [GroupGeneral];

    private static readonly Lazy<IReadOnlyList<PlatformSettingDescriptor>> EntriesLazy = new(BuildEntries);

    public static IReadOnlyList<PlatformSettingDescriptor> Entries => EntriesLazy.Value;

    public static IReadOnlyList<PlatformSettingDescriptor> ForGroups(IEnumerable<string> groups)
    {
        var set = new HashSet<string>(groups, StringComparer.Ordinal);
        return Entries.Where(e => set.Contains(e.Group)).ToList();
    }

    public static IReadOnlyList<PlatformModeRow> DashboardRows(PlatformFeatureSnapshot snap)
    {
        var rows = new List<PlatformModeRow>();
        foreach (var entry in Entries.Where(e => e.ShowOnDashboard))
        {
            if (entry.Kind == PlatformSettingKind.Policy)
            {
                rows.Add(new PlatformModeRow(
                    entry.Key,
                    entry.TitleKey,
                    "AdminDash.Mode.Required",
                    IsOn: true,
                    IsPolicyReadonly: true));
                continue;
            }

            var value = entry.Read(snap);
            var isOn = value is true;
            rows.Add(new PlatformModeRow(
                entry.Key,
                entry.TitleKey,
                isOn ? "AdminDash.Mode.On" : "AdminDash.Mode.Off",
                isOn));
        }

        return rows;
    }

    public static bool FieldExists(string propertyName)
        => typeof(PlatformFeatureSettings).GetProperty(
               propertyName,
               BindingFlags.Instance | BindingFlags.Public | BindingFlags.IgnoreCase)
           is not null;

    private static IReadOnlyList<PlatformSettingDescriptor> BuildEntries()
    {
        var list = new List<PlatformSettingDescriptor>();

        // --- Platform-modus ---
        // Slot: EmployersEnabled ("Werkgevers actief") — only when the field exists on PlatformFeatureSettings (D7).
        // if (FieldExists("EmployersEnabled")) { list.Add(... ShowOnDashboard, ConfirmOnChange, Impact warn ...); }
        // Slot: CandidatePassportEnabled ("Mijn Paspoort") — only when the field exists (D7).
        // if (FieldExists("CandidatePassportEnabled")) { list.Add(... ShowOnDashboard ...); }

        // --- Vacatures ---
        list.Add(new PlatformSettingDescriptor(
            Key: "VacancyContentModerationEnabled",
            Group: GroupVacancies,
            TitleKey: "AdminSettings.AiModeration.Title",
            DescriptionKey: "AdminSettings.AiModeration.Desc",
            Kind: PlatformSettingKind.Bool,
            Read: s => s.VacancyContentModerationEnabled,
            Write: v => new PlatformFeatureUpdate(VacancyContentModerationEnabled: ToBool(v)),
            ImpactKey: "AdminSettings.AiModeration.ImpactOff",
            ImpactLevel: PlatformSettingImpactLevel.Warn,
            ShowOnDashboard: true));

        list.Add(new PlatformSettingDescriptor(
            Key: "FreePublishUntil",
            Group: GroupVacancies,
            TitleKey: "AdminSettings.FreePublish.Title",
            DescriptionKey: "AdminSettings.FreePublish.Desc",
            Kind: PlatformSettingKind.Date,
            Read: s => s.FreePublishUntil,
            Write: v => v is DateOnly d
                ? new PlatformFeatureUpdate(FreePublishUntil: d)
                : new PlatformFeatureUpdate(ClearFreePublishUntil: true)));

        // --- Beveiliging ---
        list.Add(new PlatformSettingDescriptor(
            Key: "MfaPolicy",
            Group: GroupSecurity,
            TitleKey: "AdminSettings.Mfa.Title",
            DescriptionKey: "AdminSettings.Mfa.Desc",
            Kind: PlatformSettingKind.Policy,
            Read: _ => "Verplicht",
            Write: _ => new PlatformFeatureUpdate(),
            ImpactKey: "AdminSettings.Mfa.Meta",
            ShowOnDashboard: true));

        list.Add(new PlatformSettingDescriptor(
            Key: "SessionInactivityTimeoutMinutes",
            Group: GroupSecurity,
            TitleKey: "AdminSettings.SessionTimeout.Title",
            DescriptionKey: "AdminSettings.SessionTimeout.Desc",
            Kind: PlatformSettingKind.Int,
            Read: s => s.SessionInactivityTimeoutMinutes,
            Write: v => new PlatformFeatureUpdate(SessionInactivityTimeoutMinutes: ToInt(v)),
            Min: SessionSecurityRules.MinInactivityTimeoutMinutes,
            Max: SessionSecurityRules.MaxInactivityTimeoutMinutes,
            UnitKey: "AdminSettings.Unit.Min"));

        list.Add(new PlatformSettingDescriptor(
            Key: "SupportAccessNotifyAdmins",
            Group: GroupSecurity,
            TitleKey: "AdminSettings.SupportNotifyAdmins.Title",
            DescriptionKey: "AdminSettings.SupportNotifyAdmins.Desc",
            Kind: PlatformSettingKind.Bool,
            Read: s => s.SupportAccessNotifyAdmins,
            Write: v => new PlatformFeatureUpdate(SupportAccessNotifyAdmins: ToBool(v))));

        list.Add(new PlatformSettingDescriptor(
            Key: "SupportAccessNotifySubject",
            Group: GroupSecurity,
            TitleKey: "AdminSettings.SupportNotifySubject.Title",
            DescriptionKey: "AdminSettings.SupportNotifySubject.Desc",
            Kind: PlatformSettingKind.Bool,
            Read: s => s.SupportAccessNotifySubject,
            Write: v => new PlatformFeatureUpdate(SupportAccessNotifySubject: ToBool(v))));

        // --- Demo & test ---
        list.Add(new PlatformSettingDescriptor(
            Key: "ExposeRegistrationActivationLinks",
            Group: GroupDemo,
            TitleKey: "AdminSettings.ActivationLinks.Title",
            DescriptionKey: "AdminSettings.ActivationLinks.Desc",
            Kind: PlatformSettingKind.Bool,
            Read: s => s.ExposeRegistrationActivationLinks,
            Write: v => new PlatformFeatureUpdate(ExposeRegistrationActivationLinks: ToBool(v)),
            EnvironmentLock: PlatformSettingEnvironmentLock.AcceptatieOnly,
            BadgeKey: "AdminSettings.Badge.AcceptatieOnly"));

        list.Add(new PlatformSettingDescriptor(
            Key: "AuthenticatorEnabled",
            Group: GroupDemo,
            TitleKey: "AdminSettings.AuthenticatorStub.Title",
            DescriptionKey: "AdminSettings.AuthenticatorStub.Desc",
            Kind: PlatformSettingKind.Bool,
            Read: s => s.AuthenticatorEnabled,
            Write: v => new PlatformFeatureUpdate(AuthenticatorEnabled: ToBool(v))));

        // --- Algemeen ---
        list.Add(new PlatformSettingDescriptor(
            Key: "PublicWebBaseUrl",
            Group: GroupGeneral,
            TitleKey: "AdminSettings.PublicUrl.Title",
            DescriptionKey: "AdminSettings.PublicUrl.Desc",
            Kind: PlatformSettingKind.Text,
            Read: s => s.PublicWebBaseUrl,
            Write: v => new PlatformFeatureUpdate(PublicWebBaseUrl: v?.ToString())));

        list.Add(new PlatformSettingDescriptor(
            Key: "InactiveCompanyDays",
            Group: GroupGeneral,
            TitleKey: "AdminSettings.InactiveDays.Title",
            DescriptionKey: "AdminSettings.InactiveDays.Desc",
            Kind: PlatformSettingKind.Int,
            Read: s => s.InactiveCompanyDays,
            Write: v => new PlatformFeatureUpdate(InactiveCompanyDays: ToInt(v)),
            Min: 30,
            Max: 730,
            UnitKey: "AdminSettings.Unit.Days"));

        // MinimumSessionVersion stays out of the UI (deferred).

        return list;
    }

    private static bool ToBool(object? value) => value switch
    {
        bool b => b,
        string s when bool.TryParse(s, out var b) => b,
        _ => false
    };

    private static int ToInt(object? value) => value switch
    {
        int i => i,
        long l => (int)l,
        string s when int.TryParse(s, out var i) => i,
        _ => 0
    };

    /// <summary>Merges several single-field updates into one partial update.</summary>
    public static PlatformFeatureUpdate Merge(IEnumerable<PlatformFeatureUpdate> parts)
    {
        bool? moderation = null;
        bool? authenticator = null;
        bool? expose = null;
        string? publicUrl = null;
        int? inactive = null;
        int? timeout = null;
        DateOnly? freeUntil = null;
        var clearFree = false;
        bool? notifyAdmins = null;
        bool? notifySubject = null;

        foreach (var p in parts)
        {
            if (p.VacancyContentModerationEnabled is not null) moderation = p.VacancyContentModerationEnabled;
            if (p.AuthenticatorEnabled is not null) authenticator = p.AuthenticatorEnabled;
            if (p.ExposeRegistrationActivationLinks is not null) expose = p.ExposeRegistrationActivationLinks;
            if (p.PublicWebBaseUrl is not null) publicUrl = p.PublicWebBaseUrl;
            if (p.InactiveCompanyDays is not null) inactive = p.InactiveCompanyDays;
            if (p.SessionInactivityTimeoutMinutes is not null) timeout = p.SessionInactivityTimeoutMinutes;
            if (p.ClearFreePublishUntil)
            {
                clearFree = true;
                freeUntil = null;
            }
            else if (p.FreePublishUntil is not null)
            {
                freeUntil = p.FreePublishUntil;
                clearFree = false;
            }

            if (p.SupportAccessNotifyAdmins is not null) notifyAdmins = p.SupportAccessNotifyAdmins;
            if (p.SupportAccessNotifySubject is not null) notifySubject = p.SupportAccessNotifySubject;
        }

        return new PlatformFeatureUpdate(
            moderation,
            authenticator,
            expose,
            publicUrl,
            inactive,
            timeout,
            freeUntil,
            clearFree,
            SupportAccessNotifyAdmins: notifyAdmins,
            SupportAccessNotifySubject: notifySubject);
    }
}
