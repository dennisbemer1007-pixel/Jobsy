namespace Jobsy.Core.Features;

/// <summary>Admin UI grouping for platform feature toggles.</summary>
public enum FeatureFlagAdminGroup
{
    Kandidaat,
    Werkgever,
    Scholen,
    Tests,
    Experimenteel,
    Platform,
    Security,
    General
}

/// <summary>
/// Canonical catalog of bool feature flags. Every <see cref="PlatformFeature"/> and every
/// public <c>api/settings/feature-flags</c> field must appear here (enforced by tests).
/// </summary>
public sealed record FeatureFlagDefinition(
    string Key,
    FeatureFlagAdminGroup Group,
    string TitleKey,
    string DescriptionKey,
    PlatformFeature? PlatformFeature,
    string? ConfigurationKey,
    bool DefaultValue,
    bool ExposedOnPublicEndpoint,
    string? ImpactKey = null);

public static class FeatureFlagRegistry
{
    /// <summary>
    /// Flags defined in open PRs — registry parity test skips catalog until merged.
    /// </summary>
    public static IReadOnlySet<string> PendingAdminCatalogKeys { get; } = new HashSet<string>(StringComparer.Ordinal)
    {
        "DreamJobIllustrationEnabled", // PR #724
        "CandidateCompactHeroEnabled", // compact hero rollout — config Jobsy:CandidateCompactHeroEnabled until admin field lands
    };

    public static IReadOnlyList<FeatureFlagDefinition> All { get; } = Build();

    public static IReadOnlyDictionary<string, FeatureFlagDefinition> ByKey { get; }
        = All.ToDictionary(d => d.Key, StringComparer.Ordinal);

    public static IReadOnlyList<FeatureFlagDefinition> PublicEndpointFlags { get; }
        = All.Where(d => d.ExposedOnPublicEndpoint).ToList();

    private static IReadOnlyList<FeatureFlagDefinition> Build()
    {
        const bool pub = true;
        var list = new List<FeatureFlagDefinition>
        {
            Def("CandidatePassportEnabled", FeatureFlagAdminGroup.Kandidaat, PlatformFeature.CandidatePassport, pub, false),
            Def("SensitiveExperienceEnabled", FeatureFlagAdminGroup.Kandidaat, PlatformFeature.SensitiveExperience, pub, false, "Jobsy:SensitiveExperienceEnabled"),
            Def("CandidateExternalVacanciesEnabled", FeatureFlagAdminGroup.Kandidaat, PlatformFeature.CandidateExternalVacancies, pub, false),
            Def("ProactiveMatchingEnabled", FeatureFlagAdminGroup.Kandidaat, PlatformFeature.ProactiveMatching, pub, false),
            Def("FutureJobsForYouEnabled", FeatureFlagAdminGroup.Kandidaat, PlatformFeature.FutureJobsForYou, pub, false),
            Def("HonestAdviceEnabled", FeatureFlagAdminGroup.Kandidaat, PlatformFeature.HonestAdvice, pub, false),
            Def("UwvFeaturesEnabled", FeatureFlagAdminGroup.Kandidaat, PlatformFeature.UwvFeatures, pub, true, "Features:UwvFeatures"),
            Def("PhoneVerificationEnabled", FeatureFlagAdminGroup.Kandidaat, null, pub, false),
            Def("WhatsAppRemindersEnabled", FeatureFlagAdminGroup.Kandidaat, PlatformFeature.WhatsAppReminders, pub, false),
            Def("ReferenceVerificationEnabled", FeatureFlagAdminGroup.Kandidaat, PlatformFeature.ReferenceVerification, pub, false),
            Def("AfterMatchEnabled", FeatureFlagAdminGroup.Kandidaat, PlatformFeature.AfterMatch, pub, false),
            Def("TestJourneyIllustrationEnabled", FeatureFlagAdminGroup.Kandidaat, null, pub, false, "TestJourneyIllustrationEnabled"),
            Def("OnboardingFinaleV2Enabled", FeatureFlagAdminGroup.Kandidaat, null, pub, false, "Onboarding:FinaleV2Enabled"),

            Def("EmployersEnabled", FeatureFlagAdminGroup.Werkgever, PlatformFeature.Employers, pub, false),
            Def("EmployerPhase2Enabled", FeatureFlagAdminGroup.Werkgever, PlatformFeature.EmployerPhase2, pub, false),
            Def("KandidatenkaartEnabled", FeatureFlagAdminGroup.Werkgever, PlatformFeature.Kandidatenkaart, pub, false),
            Def("CompanyMapInitiativeEnabled", FeatureFlagAdminGroup.Werkgever, PlatformFeature.CompanyMapInitiative, pub, true),
            Def("EmployerFitQuestionnairesEnabled", FeatureFlagAdminGroup.Werkgever, PlatformFeature.EmployerFitQuestionnaires, pub, false),
            Def("MatchCalibrationV1Enabled", FeatureFlagAdminGroup.Werkgever, null, exposedOnPublicEndpoint: false, defaultValue: false),
            Def("TrainingPartnersEnabled", FeatureFlagAdminGroup.Werkgever, PlatformFeature.TrainingPartners, pub, false, "Jobsy:TrainingPartnersEnabled"),
            Def("VacancyContentModerationEnabled", FeatureFlagAdminGroup.Werkgever, null, exposedOnPublicEndpoint: false, defaultValue: true),

            Def("SchoolsEnabled", FeatureFlagAdminGroup.Scholen, PlatformFeature.Schools, pub, false),
            Def("PupilPassportV2Enabled", FeatureFlagAdminGroup.Scholen, PlatformFeature.PupilPassportV2, pub, false, "Jobsy:PupilPassportV2Enabled"),
            Def("StudentPassportEnabled", FeatureFlagAdminGroup.Scholen, PlatformFeature.StudentPassport, pub, false, "Jobsy:StudentPassportEnabled"),

            Def("TestsV2Enabled", FeatureFlagAdminGroup.Tests, PlatformFeature.TestsV2, pub, false),
            Def("CompactTestPdfEnabled", FeatureFlagAdminGroup.Tests, PlatformFeature.CompactTestPdf, pub, false),
            Def("PassportPdfV2Enabled", FeatureFlagAdminGroup.Tests, PlatformFeature.PassportPdfV2, pub, false),
            Def("PassportPdfDnaBlocksEnabled", FeatureFlagAdminGroup.Tests, null, exposedOnPublicEndpoint: false, defaultValue: true),
            Def("FreeCandidateTestsEnabled", FeatureFlagAdminGroup.Tests, null, exposedOnPublicEndpoint: false, defaultValue: true),
            Def("TestEditModeEnabled", FeatureFlagAdminGroup.Tests, null, pub, false, "JobsyFeatures:TestEditMode"),
            Def("SamplePdfEnabled", FeatureFlagAdminGroup.Tests, null, pub, false, "JobsyFeatures:SamplePdf"),
            Def("TestDepthValueMockupEnabled", FeatureFlagAdminGroup.Tests, PlatformFeature.TestDepthValueMockup, pub, true, "TestDepthValueMockupEnabled"),
            Def("TestItemExamplesEnabled", FeatureFlagAdminGroup.Tests, PlatformFeature.TestItemExamples, pub, false),
            Def("TestProgressInsightEnabled", FeatureFlagAdminGroup.Tests, PlatformFeature.TestProgressInsight, pub, false),

            Def("PassportPartnersEnabled", FeatureFlagAdminGroup.Experimenteel, PlatformFeature.PassportPartners, pub, false),
            Def("AmbassadorsEnabled", FeatureFlagAdminGroup.Experimenteel, PlatformFeature.Ambassadors, pub, false),
            Def("OccupationDayInLifeEnabled", FeatureFlagAdminGroup.Experimenteel, null, pub, true, "OccupationDayInLifeEnabled"),
            Def("ResearchConsentEnabled", FeatureFlagAdminGroup.Experimenteel, null, exposedOnPublicEndpoint: false, defaultValue: false),
            Def("AuthenticatorEnabled", FeatureFlagAdminGroup.Experimenteel, null, exposedOnPublicEndpoint: false, defaultValue: false),
        };

        return list;
    }

    private static FeatureFlagDefinition Def(
        string key,
        FeatureFlagAdminGroup group,
        PlatformFeature? feature,
        bool exposedOnPublicEndpoint,
        bool defaultValue,
        string? configurationKey = null,
        string? impactKey = null)
    {
        var title = $"AdminSettings.{key}.Title";
        var desc = $"AdminSettings.{key}.Desc";
        if (key == "VacancyContentModerationEnabled")
        {
            title = "AdminSettings.VacancyModeration.Title";
            desc = "AdminSettings.VacancyModeration.Desc";
        }

        return new FeatureFlagDefinition(
            key,
            group,
            title,
            desc,
            feature,
            configurationKey,
            defaultValue,
            exposedOnPublicEndpoint,
            impactKey);
    }
}
