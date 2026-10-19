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

/// <summary>When a bool switch must be confirmed before it lands in the draft.</summary>
public enum PlatformSettingConfirmWhen
{
    None,
    On,
    Off,
    Both
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
    string? BadgeKey = null,
    PlatformSettingConfirmWhen ConfirmWhen = PlatformSettingConfirmWhen.None);

public static class PlatformSettingsCatalog
{
    public const string GroupPlatformMode = "platform-mode";
    public const string GroupVacancies = "vacancies";
    public const string GroupSecurity = "security";
    public const string GroupDemo = "demo";
    public const string GroupGeneral = "general";
    public const string GroupScholen = "scholen";
    public const string GroupSales = "sales";

    public static readonly IReadOnlyList<(string Key, string TitleKey, string DescriptionKey)> Groups =
    [
        (GroupPlatformMode, "AdminSettings.Group.PlatformMode", "AdminSettings.Group.PlatformMode.Desc"),
        (GroupVacancies, "AdminSettings.Group.Vacancies", "AdminSettings.Group.Vacancies.Desc"),
        (GroupScholen, "AdminSettings.Group.Scholen", "AdminSettings.Group.Scholen.Desc"),
        (GroupSales, "AdminSettings.Group.Sales", "AdminSettings.Group.Sales.Desc"),
        (GroupSecurity, "AdminSettings.Group.Security", "AdminSettings.Group.Security.Desc"),
        (GroupDemo, "AdminSettings.Group.Demo", "AdminSettings.Group.Demo.Desc"),
        (GroupGeneral, "AdminSettings.Group.General", "AdminSettings.Group.General.Desc"),
    ];

    /// <summary>Functies page groups (excludes Algemeen).</summary>
    public static readonly IReadOnlyList<string> FeaturesGroupKeys =
    [
        GroupPlatformMode, GroupVacancies, GroupScholen, GroupSales, GroupSecurity, GroupDemo
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
        if (FieldExists("EmployerPhase2Enabled"))
        {
            list.Add(new PlatformSettingDescriptor(
                Key: "EmployerPhase2Enabled",
                Group: GroupPlatformMode,
                TitleKey: "AdminSettings.EmployerPhase2.Enabled.Title",
                DescriptionKey: "AdminSettings.EmployerPhase2.Enabled.Desc",
                Kind: PlatformSettingKind.Bool,
                Read: s => s.EmployerPhase2Enabled,
                Write: v => new PlatformFeatureUpdate(EmployerPhase2Enabled: ToBool(v)),
                ImpactKey: "AdminSettings.EmployerPhase2.Enabled.ImpactOff",
                ImpactLevel: PlatformSettingImpactLevel.Warn,
                ShowOnDashboard: true,
                ConfirmOnChange: true,
                ConfirmWhen: PlatformSettingConfirmWhen.On));
        }

        if (FieldExists("EmployersEnabled"))
        {
            list.Add(new PlatformSettingDescriptor(
                Key: "EmployersEnabled",
                Group: GroupPlatformMode,
                TitleKey: "AdminSettings.Employers.Enabled.Title",
                DescriptionKey: "AdminSettings.Employers.Enabled.Desc",
                Kind: PlatformSettingKind.Bool,
                Read: s => s.EmployersEnabled,
                Write: v => new PlatformFeatureUpdate(EmployersEnabled: ToBool(v)),
                ImpactKey: "AdminSettings.Employers.Enabled.ImpactOff",
                ImpactLevel: PlatformSettingImpactLevel.Warn,
                ShowOnDashboard: true,
                ConfirmOnChange: true,
                ConfirmWhen: PlatformSettingConfirmWhen.Both));
        }

        if (FieldExists("CandidatePassportEnabled"))
        {
            list.Add(new PlatformSettingDescriptor(
                Key: "CandidatePassportEnabled",
                Group: GroupPlatformMode,
                TitleKey: "AdminSettings.Passport.Enabled.Title",
                DescriptionKey: "AdminSettings.Passport.Enabled.Desc",
                Kind: PlatformSettingKind.Bool,
                Read: s => s.CandidatePassportEnabled,
                Write: v => new PlatformFeatureUpdate(CandidatePassportEnabled: ToBool(v)),
                ShowOnDashboard: true));
        }

        if (FieldExists("PassportPartnersEnabled"))
        {
            list.Add(new PlatformSettingDescriptor(
                Key: "PassportPartnersEnabled",
                Group: GroupPlatformMode,
                TitleKey: "AdminSettings.PassportPartners.Enabled.Title",
                DescriptionKey: "AdminSettings.PassportPartners.Enabled.Desc",
                Kind: PlatformSettingKind.Bool,
                Read: s => s.PassportPartnersEnabled,
                Write: v => new PlatformFeatureUpdate(PassportPartnersEnabled: ToBool(v)),
                ShowOnDashboard: true));
        }

        if (FieldExists("PassportPdfV2Enabled"))
        {
            list.Add(new PlatformSettingDescriptor(
                Key: "PassportPdfV2Enabled",
                Group: GroupPlatformMode,
                TitleKey: "AdminSettings.PassportPdfV2.Enabled.Title",
                DescriptionKey: "AdminSettings.PassportPdfV2.Enabled.Desc",
                Kind: PlatformSettingKind.Bool,
                Read: s => s.PassportPdfV2Enabled,
                Write: v => new PlatformFeatureUpdate(PassportPdfV2Enabled: ToBool(v)),
                ShowOnDashboard: true));
        }

        if (FieldExists("SensitiveExperienceEnabled"))
        {
            list.Add(new PlatformSettingDescriptor(
                Key: "SensitiveExperienceEnabled",
                Group: GroupPlatformMode,
                TitleKey: "AdminSettings.SensitiveExperience.Enabled.Title",
                DescriptionKey: "AdminSettings.SensitiveExperience.Enabled.Desc",
                Kind: PlatformSettingKind.Bool,
                Read: s => s.SensitiveExperienceEnabled,
                Write: v => new PlatformFeatureUpdate(SensitiveExperienceEnabled: ToBool(v)),
                ImpactKey: "AdminSettings.SensitiveExperience.Enabled.ImpactOff",
                ImpactLevel: PlatformSettingImpactLevel.None,
                ShowOnDashboard: false));
        }

        if (FieldExists("PassportPdfDnaBlocksEnabled"))
        {
            list.Add(new PlatformSettingDescriptor(
                Key: "PassportPdfDnaBlocksEnabled",
                Group: GroupPlatformMode,
                TitleKey: "AdminSettings.PassportPdfDnaBlocks.Enabled.Title",
                DescriptionKey: "AdminSettings.PassportPdfDnaBlocks.Enabled.Desc",
                Kind: PlatformSettingKind.Bool,
                Read: s => s.PassportPdfDnaBlocksEnabled,
                Write: v => new PlatformFeatureUpdate(PassportPdfDnaBlocksEnabled: ToBool(v)),
                ShowOnDashboard: true));
        }

        if (FieldExists("ReferenceVerificationEnabled"))
        {
            list.Add(new PlatformSettingDescriptor(
                Key: "ReferenceVerificationEnabled",
                Group: GroupPlatformMode,
                TitleKey: "AdminSettings.ReferenceVerification.Enabled.Title",
                DescriptionKey: "AdminSettings.ReferenceVerification.Enabled.Desc",
                Kind: PlatformSettingKind.Bool,
                Read: s => s.ReferenceVerificationEnabled,
                Write: v => new PlatformFeatureUpdate(ReferenceVerificationEnabled: ToBool(v)),
                ShowOnDashboard: true));
        }

        if (FieldExists("TestsV2Enabled"))
        {
            list.Add(new PlatformSettingDescriptor(
                Key: "TestsV2Enabled",
                Group: GroupPlatformMode,
                TitleKey: "AdminSettings.TestsV2.Enabled.Title",
                DescriptionKey: "AdminSettings.TestsV2.Enabled.Desc",
                Kind: PlatformSettingKind.Bool,
                Read: s => s.TestsV2Enabled,
                Write: v => new PlatformFeatureUpdate(TestsV2Enabled: ToBool(v)),
                ShowOnDashboard: true));
        }

        if (FieldExists("PhoneVerificationEnabled"))
        {
            list.Add(new PlatformSettingDescriptor(
                Key: "PhoneVerificationEnabled",
                Group: GroupPlatformMode,
                TitleKey: "AdminSettings.PhoneVerification.Enabled.Title",
                DescriptionKey: "AdminSettings.PhoneVerification.Enabled.Desc",
                Kind: PlatformSettingKind.Bool,
                Read: s => s.PhoneVerificationEnabled,
                Write: v => new PlatformFeatureUpdate(PhoneVerificationEnabled: ToBool(v)),
                ShowOnDashboard: false));
        }

        if (FieldExists("WhatsAppRemindersEnabled"))
        {
            list.Add(new PlatformSettingDescriptor(
                Key: "WhatsAppRemindersEnabled",
                Group: GroupPlatformMode,
                TitleKey: "AdminSettings.WhatsAppReminders.Enabled.Title",
                DescriptionKey: "AdminSettings.WhatsAppReminders.Enabled.Desc",
                Kind: PlatformSettingKind.Bool,
                Read: s => s.WhatsAppRemindersEnabled,
                Write: v => new PlatformFeatureUpdate(WhatsAppRemindersEnabled: ToBool(v)),
                ImpactKey: "AdminSettings.WhatsAppReminders.Enabled.Impact",
                ImpactLevel: PlatformSettingImpactLevel.Warn,
                ConfirmOnChange: true,
                ConfirmWhen: PlatformSettingConfirmWhen.On,
                ShowOnDashboard: true));
        }

        if (FieldExists("CompactTestPdfEnabled"))
        {
            list.Add(new PlatformSettingDescriptor(
                Key: "CompactTestPdfEnabled",
                Group: GroupPlatformMode,
                TitleKey: "AdminSettings.CompactTestPdf.Enabled.Title",
                DescriptionKey: "AdminSettings.CompactTestPdf.Enabled.Desc",
                Kind: PlatformSettingKind.Bool,
                Read: s => s.CompactTestPdfEnabled,
                Write: v => new PlatformFeatureUpdate(CompactTestPdfEnabled: ToBool(v)),
                ShowOnDashboard: false));
        }

        if (FieldExists("HonestAdviceEnabled"))
        {
            list.Add(new PlatformSettingDescriptor(
                Key: "HonestAdviceEnabled",
                Group: GroupPlatformMode,
                TitleKey: "AdminSettings.HonestAdvice.Enabled.Title",
                DescriptionKey: "AdminSettings.HonestAdvice.Enabled.Desc",
                Kind: PlatformSettingKind.Bool,
                Read: s => s.HonestAdviceEnabled,
                Write: v => new PlatformFeatureUpdate(HonestAdviceEnabled: ToBool(v)),
                ShowOnDashboard: false));
        }

        if (FieldExists("FutureJobsForYouEnabled"))
        {
            list.Add(new PlatformSettingDescriptor(
                Key: "FutureJobsForYouEnabled",
                Group: GroupPlatformMode,
                TitleKey: "AdminSettings.FutureJobs.Enabled.Title",
                DescriptionKey: "AdminSettings.FutureJobs.Enabled.Desc",
                Kind: PlatformSettingKind.Bool,
                Read: s => s.FutureJobsForYouEnabled,
                Write: v => new PlatformFeatureUpdate(FutureJobsForYouEnabled: ToBool(v)),
                ShowOnDashboard: false));
        }

        if (FieldExists("FreeCandidateTestsEnabled"))
        {
            list.Add(new PlatformSettingDescriptor(
                Key: "FreeCandidateTestsEnabled",
                Group: GroupPlatformMode,
                TitleKey: "AdminSettings.FreeCandidateTests.Enabled.Title",
                DescriptionKey: "AdminSettings.FreeCandidateTests.Enabled.Desc",
                Kind: PlatformSettingKind.Bool,
                Read: s => s.FreeCandidateTestsEnabled,
                Write: v => new PlatformFeatureUpdate(FreeCandidateTestsEnabled: ToBool(v)),
                ImpactKey: "AdminSettings.FreeCandidateTests.Enabled.ImpactOff",
                ImpactLevel: PlatformSettingImpactLevel.Warn,
                ConfirmOnChange: true,
                ConfirmWhen: PlatformSettingConfirmWhen.Off,
                ShowOnDashboard: true));
        }

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

        list.Add(new PlatformSettingDescriptor(
            Key: "CandidateInsightsEnabled",
            Group: GroupVacancies,
            TitleKey: "AdminSettings.CandidateInsights.Enabled.Title",
            DescriptionKey: "AdminSettings.CandidateInsights.Enabled.Desc",
            Kind: PlatformSettingKind.Bool,
            Read: s => s.CandidateInsightsEnabled,
            Write: v => new PlatformFeatureUpdate(CandidateInsightsEnabled: ToBool(v)),
            ImpactKey: "AdminSettings.CandidateInsights.Enabled.ImpactOff",
            ImpactLevel: PlatformSettingImpactLevel.Warn,
            ShowOnDashboard: true));

        list.Add(new PlatformSettingDescriptor(
            Key: "CandidateInsightsUnlockDays",
            Group: GroupVacancies,
            TitleKey: "AdminSettings.CandidateInsights.UnlockDays.Title",
            DescriptionKey: "AdminSettings.CandidateInsights.UnlockDays.Desc",
            Kind: PlatformSettingKind.Int,
            Read: s => s.CandidateInsightsUnlockDays,
            Write: v => new PlatformFeatureUpdate(CandidateInsightsUnlockDays: ToInt(v)),
            Min: CandidateInsightsAccess.MinUnlockDays,
            Max: CandidateInsightsAccess.MaxUnlockDays,
            UnitKey: "AdminSettings.Unit.Days"));

        if (FieldExists("CandidateExternalVacanciesEnabled"))
        {
            list.Add(new PlatformSettingDescriptor(
                Key: "CandidateExternalVacanciesEnabled",
                Group: GroupVacancies,
                TitleKey: "AdminSettings.ExternalVacancies.Enabled.Title",
                DescriptionKey: "AdminSettings.ExternalVacancies.Enabled.Desc",
                Kind: PlatformSettingKind.Bool,
                Read: s => s.CandidateExternalVacanciesEnabled,
                Write: v => new PlatformFeatureUpdate(CandidateExternalVacanciesEnabled: ToBool(v)),
                ShowOnDashboard: false));
        }

        if (FieldExists("ProactiveMatchingEnabled"))
        {
            list.Add(new PlatformSettingDescriptor(
                Key: "ProactiveMatchingEnabled",
                Group: GroupVacancies,
                TitleKey: "AdminSettings.ProactiveMatching.Enabled.Title",
                DescriptionKey: "AdminSettings.ProactiveMatching.Enabled.Desc",
                Kind: PlatformSettingKind.Bool,
                Read: s => s.ProactiveMatchingEnabled,
                Write: v => new PlatformFeatureUpdate(ProactiveMatchingEnabled: ToBool(v)),
                ShowOnDashboard: false));
        }

        if (FieldExists("MatchCalibrationV1Enabled"))
        {
            list.Add(new PlatformSettingDescriptor(
                Key: "MatchCalibrationV1Enabled",
                Group: GroupVacancies,
                TitleKey: "AdminSettings.MatchCalibrationV1.Enabled.Title",
                DescriptionKey: "AdminSettings.MatchCalibrationV1.Enabled.Desc",
                Kind: PlatformSettingKind.Bool,
                Read: s => s.MatchCalibrationV1Enabled,
                Write: v => new PlatformFeatureUpdate(MatchCalibrationV1Enabled: ToBool(v)),
                ShowOnDashboard: false));
        }

        if (FieldExists("EmployerFitQuestionnairesEnabled"))
        {
            list.Add(new PlatformSettingDescriptor(
                Key: "EmployerFitQuestionnairesEnabled",
                Group: GroupVacancies,
                TitleKey: "AdminSettings.EmployerFitQuestionnaires.Enabled.Title",
                DescriptionKey: "AdminSettings.EmployerFitQuestionnaires.Enabled.Desc",
                Kind: PlatformSettingKind.Bool,
                Read: s => s.EmployerFitQuestionnairesEnabled,
                Write: v => new PlatformFeatureUpdate(EmployerFitQuestionnairesEnabled: ToBool(v)),
                ShowOnDashboard: false));
        }

        if (FieldExists("KandidatenkaartEnabled"))
        {
            list.Add(new PlatformSettingDescriptor(
                Key: "KandidatenkaartEnabled",
                Group: GroupVacancies,
                TitleKey: "AdminSettings.Kandidatenkaart.Enabled.Title",
                DescriptionKey: "AdminSettings.Kandidatenkaart.Enabled.Desc",
                Kind: PlatformSettingKind.Bool,
                Read: s => s.KandidatenkaartEnabled,
                Write: v => new PlatformFeatureUpdate(KandidatenkaartEnabled: ToBool(v)),
                ImpactKey: "AdminSettings.Kandidatenkaart.Enabled.ImpactOff",
                ImpactLevel: PlatformSettingImpactLevel.Warn,
                ShowOnDashboard: false));
        }

        if (FieldExists("AfterMatchEnabled"))
        {
            list.Add(new PlatformSettingDescriptor(
                Key: "AfterMatchEnabled",
                Group: GroupVacancies,
                TitleKey: "AdminSettings.AfterMatch.Enabled.Title",
                DescriptionKey: "AdminSettings.AfterMatch.Enabled.Desc",
                Kind: PlatformSettingKind.Bool,
                Read: s => s.AfterMatchEnabled,
                Write: v => new PlatformFeatureUpdate(AfterMatchEnabled: ToBool(v)),
                ShowOnDashboard: false));
        }

        if (FieldExists("TrainingPartnersEnabled"))
        {
            list.Add(new PlatformSettingDescriptor(
                Key: "TrainingPartnersEnabled",
                Group: GroupVacancies,
                TitleKey: "AdminSettings.TrainingPartners.Enabled.Title",
                DescriptionKey: "AdminSettings.TrainingPartners.Enabled.Desc",
                Kind: PlatformSettingKind.Bool,
                Read: s => s.TrainingPartnersEnabled,
                Write: v => new PlatformFeatureUpdate(TrainingPartnersEnabled: ToBool(v)),
                ImpactKey: "AdminSettings.TrainingPartners.Enabled.ImpactOff",
                ImpactLevel: PlatformSettingImpactLevel.None,
                ShowOnDashboard: false));
        }

        if (FieldExists("CandidateApplyPerHourLimit"))
        {
            list.Add(new PlatformSettingDescriptor(
                Key: "CandidateApplyPerHourLimit",
                Group: GroupVacancies,
                TitleKey: "AdminSettings.ApplyRate.PerHour.Title",
                DescriptionKey: "AdminSettings.ApplyRate.PerHour.Desc",
                Kind: PlatformSettingKind.Int,
                Read: s => s.CandidateApplyPerHourLimit,
                Write: v => new PlatformFeatureUpdate(CandidateApplyPerHourLimit: ToInt(v)),
                Min: CandidateApplyRateLimitRules.MinLimit,
                Max: CandidateApplyRateLimitRules.MaxPerHour,
                UnitKey: "AdminSettings.Unit.PerHour"));
        }

        if (FieldExists("CandidateApplyPerDayLimit"))
        {
            list.Add(new PlatformSettingDescriptor(
                Key: "CandidateApplyPerDayLimit",
                Group: GroupVacancies,
                TitleKey: "AdminSettings.ApplyRate.PerDay.Title",
                DescriptionKey: "AdminSettings.ApplyRate.PerDay.Desc",
                Kind: PlatformSettingKind.Int,
                Read: s => s.CandidateApplyPerDayLimit,
                Write: v => new PlatformFeatureUpdate(CandidateApplyPerDayLimit: ToInt(v)),
                Min: CandidateApplyRateLimitRules.MinLimit,
                Max: CandidateApplyRateLimitRules.MaxPerDay,
                UnitKey: "AdminSettings.Unit.PerDay"));
        }

        if (FieldExists("CandidateApplyPerMinuteLimit"))
        {
            list.Add(new PlatformSettingDescriptor(
                Key: "CandidateApplyPerMinuteLimit",
                Group: GroupVacancies,
                TitleKey: "AdminSettings.ApplyRate.PerMinute.Title",
                DescriptionKey: "AdminSettings.ApplyRate.PerMinute.Desc",
                Kind: PlatformSettingKind.Int,
                Read: s => s.CandidateApplyPerMinuteLimit,
                Write: v => new PlatformFeatureUpdate(CandidateApplyPerMinuteLimit: ToInt(v)),
                Min: CandidateApplyRateLimitRules.MinLimit,
                Max: CandidateApplyRateLimitRules.MaxPerMinute,
                UnitKey: "AdminSettings.Unit.PerMinute"));
        }

        list.Add(new PlatformSettingDescriptor(
            Key: "CandidateInsightsUnlockPerBranch",
            Group: GroupVacancies,
            TitleKey: "AdminSettings.CandidateInsights.PerBranch.Title",
            DescriptionKey: "AdminSettings.CandidateInsights.PerBranch.Desc",
            Kind: PlatformSettingKind.Bool,
            Read: s => s.CandidateInsightsUnlockPerBranch,
            Write: v => new PlatformFeatureUpdate(CandidateInsightsUnlockPerBranch: ToBool(v))));

        // --- Scholen ---
        if (FieldExists("PupilPassportV2Enabled"))
        {
            list.Add(new PlatformSettingDescriptor(
                Key: "PupilPassportV2Enabled",
                Group: GroupScholen,
                TitleKey: "AdminSettings.PupilPassportV2.Enabled.Title",
                DescriptionKey: "AdminSettings.PupilPassportV2.Enabled.Desc",
                Kind: PlatformSettingKind.Bool,
                Read: s => s.PupilPassportV2Enabled,
                Write: v => new PlatformFeatureUpdate(PupilPassportV2Enabled: ToBool(v)),
                ShowOnDashboard: false));
        }

        if (FieldExists("StudentPassportEnabled"))
        {
            list.Add(new PlatformSettingDescriptor(
                Key: "StudentPassportEnabled",
                Group: GroupScholen,
                TitleKey: "AdminSettings.StudentPassport.Enabled.Title",
                DescriptionKey: "AdminSettings.StudentPassport.Enabled.Desc",
                Kind: PlatformSettingKind.Bool,
                Read: s => s.StudentPassportEnabled,
                Write: v => new PlatformFeatureUpdate(StudentPassportEnabled: ToBool(v)),
                ShowOnDashboard: false));
        }

        list.Add(new PlatformSettingDescriptor(
            Key: "SchoolsEnabled",
            Group: GroupScholen,
            TitleKey: "AdminSettings.Schools.Enabled.Title",
            DescriptionKey: "AdminSettings.Schools.Enabled.Desc",
            Kind: PlatformSettingKind.Bool,
            Read: s => s.SchoolsEnabled,
            Write: v => new PlatformFeatureUpdate(SchoolsEnabled: ToBool(v)),
            ImpactKey: "AdminSettings.Schools.Enabled.ImpactOff",
            ImpactLevel: PlatformSettingImpactLevel.Warn,
            ConfirmOnChange: true,
            ShowOnDashboard: true,
            ConfirmWhen: PlatformSettingConfirmWhen.Both));

        list.Add(new PlatformSettingDescriptor(
            Key: "SchoolPerCodeResultsEnabled",
            Group: GroupScholen,
            TitleKey: "AdminSettings.Schools.PerCode.Title",
            DescriptionKey: "AdminSettings.Schools.PerCode.Desc",
            Kind: PlatformSettingKind.Bool,
            Read: s => s.SchoolPerCodeResultsEnabled,
            Write: v => new PlatformFeatureUpdate(SchoolPerCodeResultsEnabled: ToBool(v))));

        list.Add(new PlatformSettingDescriptor(
            Key: "SchoolRetentionCutoffMonth",
            Group: GroupScholen,
            TitleKey: "AdminSettings.Schools.RetentionMonth.Title",
            DescriptionKey: "AdminSettings.Schools.RetentionMonth.Desc",
            Kind: PlatformSettingKind.Int,
            Read: s => s.SchoolRetentionCutoffMonth,
            Write: v => new PlatformFeatureUpdate(SchoolRetentionCutoffMonth: ToInt(v)),
            Min: 1,
            Max: 12));

        list.Add(new PlatformSettingDescriptor(
            Key: "SchoolRetentionCutoffDay",
            Group: GroupScholen,
            TitleKey: "AdminSettings.Schools.RetentionDay.Title",
            DescriptionKey: "AdminSettings.Schools.RetentionDay.Desc",
            Kind: PlatformSettingKind.Int,
            Read: s => s.SchoolRetentionCutoffDay,
            Write: v => new PlatformFeatureUpdate(SchoolRetentionCutoffDay: ToInt(v)),
            Min: 1,
            Max: 31));

        // --- Sales (Ambassadeur parked by default; tip SettingsAdmin toggle → catalog) ---
        list.Add(new PlatformSettingDescriptor(
            Key: "AmbassadorsEnabled",
            Group: GroupSales,
            TitleKey: "AdminSettings.Ambassadors.Enabled.Title",
            DescriptionKey: "AdminSettings.Ambassadors.Enabled.Desc",
            Kind: PlatformSettingKind.Bool,
            Read: s => s.AmbassadorsEnabled,
            Write: v => new PlatformFeatureUpdate(AmbassadorsEnabled: ToBool(v)),
            ImpactKey: "AdminSettings.Ambassadors.Enabled.ImpactOff",
            ImpactLevel: PlatformSettingImpactLevel.Warn,
            ConfirmOnChange: true,
            ShowOnDashboard: true));

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
        if (FieldExists("TestJourneyIllustrationEnabled"))
        {
            list.Add(new PlatformSettingDescriptor(
                Key: "TestJourneyIllustrationEnabled",
                Group: GroupDemo,
                TitleKey: "AdminSettings.TestJourneyIllustration.Enabled.Title",
                DescriptionKey: "AdminSettings.TestJourneyIllustration.Enabled.Desc",
                Kind: PlatformSettingKind.Bool,
                Read: s => s.TestJourneyIllustrationEnabled,
                Write: v => new PlatformFeatureUpdate(TestJourneyIllustrationEnabled: ToBool(v)),
                ShowOnDashboard: false));
        }

        if (FieldExists("OnboardingFinaleV2Enabled"))
        {
            list.Add(new PlatformSettingDescriptor(
                Key: "OnboardingFinaleV2Enabled",
                Group: GroupDemo,
                TitleKey: "AdminSettings.OnboardingFinaleV2.Enabled.Title",
                DescriptionKey: "AdminSettings.OnboardingFinaleV2.Enabled.Desc",
                Kind: PlatformSettingKind.Bool,
                Read: s => s.OnboardingFinaleV2Enabled,
                Write: v => new PlatformFeatureUpdate(OnboardingFinaleV2Enabled: ToBool(v)),
                ShowOnDashboard: false));
        }

        if (FieldExists("OccupationDayInLifeEnabled"))
        {
            list.Add(new PlatformSettingDescriptor(
                Key: "OccupationDayInLifeEnabled",
                Group: GroupDemo,
                TitleKey: "AdminSettings.OccupationDayInLife.Enabled.Title",
                DescriptionKey: "AdminSettings.OccupationDayInLife.Enabled.Desc",
                Kind: PlatformSettingKind.Bool,
                Read: s => s.OccupationDayInLifeEnabled,
                Write: v => new PlatformFeatureUpdate(OccupationDayInLifeEnabled: ToBool(v)),
                ShowOnDashboard: false));
        }

        if (FieldExists("ResearchConsentEnabled"))
        {
            list.Add(new PlatformSettingDescriptor(
                Key: "ResearchConsentEnabled",
                Group: GroupDemo,
                TitleKey: "AdminSettings.ResearchConsentEnabled.Title",
                DescriptionKey: "AdminSettings.ResearchConsentEnabled.Desc",
                Kind: PlatformSettingKind.Bool,
                Read: s => s.ResearchConsentEnabled,
                Write: v => new PlatformFeatureUpdate(ResearchConsentEnabled: ToBool(v)),
                ShowOnDashboard: false));
        }

        if (FieldExists("TestEditModeEnabled"))
        {
            list.Add(new PlatformSettingDescriptor(
                Key: "TestEditModeEnabled",
                Group: GroupDemo,
                TitleKey: "AdminSettings.TestEditModeEnabled.Title",
                DescriptionKey: "AdminSettings.TestEditModeEnabled.Desc",
                Kind: PlatformSettingKind.Bool,
                Read: s => s.TestEditModeEnabled,
                Write: v => new PlatformFeatureUpdate(TestEditModeEnabled: ToBool(v)),
                ShowOnDashboard: false));
        }

        if (FieldExists("SamplePdfEnabled"))
        {
            list.Add(new PlatformSettingDescriptor(
                Key: "SamplePdfEnabled",
                Group: GroupDemo,
                TitleKey: "AdminSettings.SamplePdfEnabled.Title",
                DescriptionKey: "AdminSettings.SamplePdfEnabled.Desc",
                Kind: PlatformSettingKind.Bool,
                Read: s => s.SamplePdfEnabled,
                Write: v => new PlatformFeatureUpdate(SamplePdfEnabled: ToBool(v)),
                ShowOnDashboard: false));
        }

        list.Add(new PlatformSettingDescriptor(
            Key: "AuthenticatorEnabled",
            Group: GroupDemo,
            TitleKey: "AdminSettings.AuthenticatorStub.Title",
            DescriptionKey: "AdminSettings.AuthenticatorStub.Desc",
            Kind: PlatformSettingKind.Bool,
            Read: s => s.AuthenticatorEnabled,
            Write: v => new PlatformFeatureUpdate(AuthenticatorEnabled: ToBool(v)),
            EnvironmentLock: PlatformSettingEnvironmentLock.AcceptatieOnly,
            BadgeKey: "AdminSettings.Badge.AcceptatieOnly"));

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
        string? publicUrl = null;
        int? inactive = null;
        int? timeout = null;
        DateOnly? freeUntil = null;
        var clearFree = false;
        bool? notifyAdmins = null;
        bool? notifySubject = null;
        bool? insightsEnabled = null;
        int? insightsUnlockDays = null;
        bool? insightsPerBranch = null;
        bool? schoolsEnabled = null;
        bool? schoolPerCode = null;
        int? schoolRetentionMonth = null;
        int? schoolRetentionDay = null;
        bool? ambassadorsEnabled = null;
        bool? employersEnabled = null;
        bool? candidatePassportEnabled = null;
        bool? passportPartnersEnabled = null;
        bool? passportPdfV2Enabled = null;
        bool? passportPdfDnaBlocksEnabled = null;
        bool? referenceVerificationEnabled = null;
        bool? testsV2Enabled = null;
        bool? phoneVerificationEnabled = null;
        bool? whatsAppRemindersEnabled = null;
        bool? compactTestPdfEnabled = null;
        bool? honestAdviceEnabled = null;
        bool? futureJobsForYouEnabled = null;
        bool? employerPhase2Enabled = null;
        bool? candidateExternalVacanciesEnabled = null;
        bool? proactiveMatchingEnabled = null;
        bool? matchCalibrationV1Enabled = null;
        bool? employerFitQuestionnairesEnabled = null;
        bool? kandidatenkaartEnabled = null;
        bool? afterMatchEnabled = null;
        bool? sensitiveExperienceEnabled = null;
        bool? trainingPartnersEnabled = null;
        bool? testJourneyIllustrationEnabled = null;
        bool? onboardingFinaleV2Enabled = null;
        bool? occupationDayInLifeEnabled = null;
        bool? researchConsentEnabled = null;
        bool? testEditModeEnabled = null;
        bool? samplePdfEnabled = null;
        IReadOnlyList<string>? clearAdminFlagOverrideKeys = null;
        bool? freeCandidateTestsEnabled = null;
        int? candidateApplyPerMinute = null;
        int? candidateApplyPerHour = null;
        int? candidateApplyPerDay = null;

        foreach (var p in parts)
        {
            if (p.VacancyContentModerationEnabled is not null) moderation = p.VacancyContentModerationEnabled;
            if (p.AuthenticatorEnabled is not null) authenticator = p.AuthenticatorEnabled;
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
            if (p.CandidateInsightsEnabled is not null) insightsEnabled = p.CandidateInsightsEnabled;
            if (p.CandidateInsightsUnlockDays is not null) insightsUnlockDays = p.CandidateInsightsUnlockDays;
            if (p.CandidateInsightsUnlockPerBranch is not null) insightsPerBranch = p.CandidateInsightsUnlockPerBranch;
            if (p.SchoolsEnabled is not null) schoolsEnabled = p.SchoolsEnabled;
            if (p.SchoolPerCodeResultsEnabled is not null) schoolPerCode = p.SchoolPerCodeResultsEnabled;
            if (p.SchoolRetentionCutoffMonth is not null) schoolRetentionMonth = p.SchoolRetentionCutoffMonth;
            if (p.SchoolRetentionCutoffDay is not null) schoolRetentionDay = p.SchoolRetentionCutoffDay;
            if (p.AmbassadorsEnabled is not null) ambassadorsEnabled = p.AmbassadorsEnabled;
            if (p.EmployersEnabled is not null) employersEnabled = p.EmployersEnabled;
            if (p.CandidatePassportEnabled is not null) candidatePassportEnabled = p.CandidatePassportEnabled;
            if (p.PassportPartnersEnabled is not null) passportPartnersEnabled = p.PassportPartnersEnabled;
            if (p.PassportPdfV2Enabled is not null) passportPdfV2Enabled = p.PassportPdfV2Enabled;
            if (p.PassportPdfDnaBlocksEnabled is not null) passportPdfDnaBlocksEnabled = p.PassportPdfDnaBlocksEnabled;
            if (p.ReferenceVerificationEnabled is not null) referenceVerificationEnabled = p.ReferenceVerificationEnabled;
            if (p.TestsV2Enabled is not null) testsV2Enabled = p.TestsV2Enabled;
            if (p.PhoneVerificationEnabled is not null) phoneVerificationEnabled = p.PhoneVerificationEnabled;
            if (p.WhatsAppRemindersEnabled is not null) whatsAppRemindersEnabled = p.WhatsAppRemindersEnabled;
            if (p.CompactTestPdfEnabled is not null) compactTestPdfEnabled = p.CompactTestPdfEnabled;
            if (p.FreeCandidateTestsEnabled is not null) freeCandidateTestsEnabled = p.FreeCandidateTestsEnabled;
            if (p.HonestAdviceEnabled is not null) honestAdviceEnabled = p.HonestAdviceEnabled;
            if (p.FutureJobsForYouEnabled is not null) futureJobsForYouEnabled = p.FutureJobsForYouEnabled;
            if (p.EmployerPhase2Enabled is not null) employerPhase2Enabled = p.EmployerPhase2Enabled;
            if (p.CandidateExternalVacanciesEnabled is not null)
            {
                candidateExternalVacanciesEnabled = p.CandidateExternalVacanciesEnabled;
            }

            if (p.ProactiveMatchingEnabled is not null)
            {
                proactiveMatchingEnabled = p.ProactiveMatchingEnabled;
            }

            if (p.MatchCalibrationV1Enabled is not null)
            {
                matchCalibrationV1Enabled = p.MatchCalibrationV1Enabled;
            }

            if (p.EmployerFitQuestionnairesEnabled is not null)
            {
                employerFitQuestionnairesEnabled = p.EmployerFitQuestionnairesEnabled;
            }

            if (p.KandidatenkaartEnabled is not null)
            {
                kandidatenkaartEnabled = p.KandidatenkaartEnabled;
            }

            if (p.AfterMatchEnabled is not null)
            {
                afterMatchEnabled = p.AfterMatchEnabled;
            }

            if (p.TrainingPartnersEnabled is not null)
            {
                trainingPartnersEnabled = p.TrainingPartnersEnabled;
            }

            if (p.SensitiveExperienceEnabled is not null)
            {
                sensitiveExperienceEnabled = p.SensitiveExperienceEnabled;
            }

            if (p.TestJourneyIllustrationEnabled is not null)
            {
                testJourneyIllustrationEnabled = p.TestJourneyIllustrationEnabled;
            }

            if (p.OnboardingFinaleV2Enabled is not null)
            {
                onboardingFinaleV2Enabled = p.OnboardingFinaleV2Enabled;
            }

            if (p.OccupationDayInLifeEnabled is not null)
            {
                occupationDayInLifeEnabled = p.OccupationDayInLifeEnabled;
            }

            if (p.ResearchConsentEnabled is not null)
            {
                researchConsentEnabled = p.ResearchConsentEnabled;
            }

            if (p.TestEditModeEnabled is not null)
            {
                testEditModeEnabled = p.TestEditModeEnabled;
            }

            if (p.SamplePdfEnabled is not null)
            {
                samplePdfEnabled = p.SamplePdfEnabled;
            }

            if (p.ClearAdminFlagOverrideKeys is not null)
            {
                clearAdminFlagOverrideKeys = p.ClearAdminFlagOverrideKeys;
            }

            if (p.CandidateApplyPerMinuteLimit is not null)
            {
                candidateApplyPerMinute = p.CandidateApplyPerMinuteLimit;
            }

            if (p.CandidateApplyPerHourLimit is not null)
            {
                candidateApplyPerHour = p.CandidateApplyPerHourLimit;
            }

            if (p.CandidateApplyPerDayLimit is not null)
            {
                candidateApplyPerDay = p.CandidateApplyPerDayLimit;
            }
        }

        return new PlatformFeatureUpdate(
            moderation,
            authenticator,
            publicUrl,
            inactive,
            timeout,
            freeUntil,
            clearFree,
            SupportAccessNotifyAdmins: notifyAdmins,
            SupportAccessNotifySubject: notifySubject,
            CandidateInsightsEnabled: insightsEnabled,
            CandidateInsightsUnlockDays: insightsUnlockDays,
            CandidateInsightsUnlockPerBranch: insightsPerBranch,
            SchoolsEnabled: schoolsEnabled,
            SchoolPerCodeResultsEnabled: schoolPerCode,
            SchoolRetentionCutoffMonth: schoolRetentionMonth,
            SchoolRetentionCutoffDay: schoolRetentionDay,
            AmbassadorsEnabled: ambassadorsEnabled,
            EmployersEnabled: employersEnabled,
            CandidatePassportEnabled: candidatePassportEnabled,
            PassportPartnersEnabled: passportPartnersEnabled,
            PassportPdfV2Enabled: passportPdfV2Enabled,
            PassportPdfDnaBlocksEnabled: passportPdfDnaBlocksEnabled,
            ReferenceVerificationEnabled: referenceVerificationEnabled,
            TestsV2Enabled: testsV2Enabled,
            PhoneVerificationEnabled: phoneVerificationEnabled,
            WhatsAppRemindersEnabled: whatsAppRemindersEnabled,
            CompactTestPdfEnabled: compactTestPdfEnabled,
            FreeCandidateTestsEnabled: freeCandidateTestsEnabled,
            HonestAdviceEnabled: honestAdviceEnabled,
            FutureJobsForYouEnabled: futureJobsForYouEnabled,
            EmployerPhase2Enabled: employerPhase2Enabled,
            CandidateExternalVacanciesEnabled: candidateExternalVacanciesEnabled,
            ProactiveMatchingEnabled: proactiveMatchingEnabled,
            MatchCalibrationV1Enabled: matchCalibrationV1Enabled,
            EmployerFitQuestionnairesEnabled: employerFitQuestionnairesEnabled,
            KandidatenkaartEnabled: kandidatenkaartEnabled,
            AfterMatchEnabled: afterMatchEnabled,
            SensitiveExperienceEnabled: sensitiveExperienceEnabled,
            TrainingPartnersEnabled: trainingPartnersEnabled,
            TestJourneyIllustrationEnabled: testJourneyIllustrationEnabled,
            OnboardingFinaleV2Enabled: onboardingFinaleV2Enabled,
            OccupationDayInLifeEnabled: occupationDayInLifeEnabled,
            ResearchConsentEnabled: researchConsentEnabled,
            TestEditModeEnabled: testEditModeEnabled,
            SamplePdfEnabled: samplePdfEnabled,
            ClearAdminFlagOverrideKeys: clearAdminFlagOverrideKeys,
            CandidateApplyPerMinuteLimit: candidateApplyPerMinute,
            CandidateApplyPerHourLimit: candidateApplyPerHour,
            CandidateApplyPerDayLimit: candidateApplyPerDay);
    }
}
