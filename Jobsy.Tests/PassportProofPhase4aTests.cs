using Jobsy.Web.Components.Candidate;
using Jobsy.Web.Components.Candidate.ProfileSections;

namespace Jobsy.Tests;

public class ProofStrengthRulesTests
{
    [Theory]
    [InlineData(0, 0, 0, 0, false, 0)]
    [InlineData(1, 0, 0, 0, false, 1)]
    [InlineData(2, 1, 1, 1, true, 6)]
    [InlineData(3, 2, 2, 2, true, 8)] // capped
    [InlineData(10, 10, 10, 10, true, 8)]
    public void Count_sums_and_caps_at_eight(
        int employers, int educations, int certificates, int references, bool cv, int expected)
    {
        var snap = new ProofStrengthRules.ProofSnapshot(employers, educations, certificates, references, cv);
        Assert.Equal(expected, ProofStrengthRules.Count(snap));
        Assert.Equal(expected, ProofStrengthRules.Build(snap).Filled);
    }

    [Fact]
    public void SuggestMissing_prefers_reference_then_certificate()
    {
        var snap = new ProofStrengthRules.ProofSnapshot(
            Employers: 3,
            Educations: 2,
            Certificates: 0,
            References: 0,
            HasOwnCv: true);
        var view = ProofStrengthRules.Build(snap);
        Assert.Equal(6, view.Filled);
        Assert.True(view.Missing.Count >= 2);
        Assert.Equal(ProofStrengthRules.ProofKind.Reference, view.Missing[0].Kind);
        Assert.Equal(ProofStrengthRules.ProofKind.Certificate, view.Missing[1].Kind);
        Assert.Equal("Passport.Proof.HintTwo", ProofStrengthRules.HintKey(view.Missing));
    }

    [Fact]
    public void Hard_shell_has_no_missing()
    {
        var snap = new ProofStrengthRules.ProofSnapshot(3, 1, 2, 1, true);
        var view = ProofStrengthRules.Build(snap);
        Assert.Equal(8, view.Filled);
        Assert.Empty(view.Missing);
        Assert.Equal("Passport.Proof.HintHard", ProofStrengthRules.HintKey(view.Missing));
    }
}

public class ProfileSectionsExtractionTests
{
    [Fact]
    public void Profile_host_composes_shared_sections()
    {
        var root = FindRepoRoot();
        var profile = File.ReadAllText(Path.Combine(root, "Jobsy.Web/Components/Pages/Candidate/Profile.razor"));
        Assert.Contains("<PersonalSection", profile);
        Assert.Contains("<DevicesSection", profile);
        Assert.Contains("<PreferencesSection", profile);
        Assert.Contains("<AvailabilitySection", profile);
        Assert.Contains("<MotivationSection", profile);
        Assert.Contains("<CvSection", profile);
        Assert.Contains("<ExperienceSection", profile);
        Assert.Contains("<EducationSection", profile);
        Assert.Contains("<CertificatesSection", profile);
        Assert.Contains("<ReferencesSection", profile);
        Assert.Contains("<ConsentSection", profile);
        Assert.Contains("<DeleteAccountSection", profile);
        Assert.Contains("<ProfileSaveBar", profile);
        Assert.Contains("CandidateProfileEditor", profile);
        Assert.Contains("profile-accordion", profile);
        Assert.Contains("UnsubscribeDialog", profile);
    }

    [Fact]
    public void Sections_keep_key_markup_classes()
    {
        var root = FindRepoRoot();
        var blob = string.Concat(Directory.EnumerateFiles(
            Path.Combine(root, "Jobsy.Web/Components/Candidate/ProfileSections"),
            "*.razor").Select(File.ReadAllText));
        Assert.Contains("profile-contact__names", blob);
        Assert.Contains("profile-devices", blob);
        Assert.Contains("profile-check-grid", blob);
        Assert.Contains("availability-matrix", blob);
        Assert.Contains("availability-presets", blob);
        Assert.Contains("profile-employer-card", blob);
        Assert.Contains("profile-cert-row", blob);
        Assert.Contains("profile-save-bar", blob);
        Assert.Contains("profile-unsubscribe", blob);
        Assert.Contains("CandidateProfileEditor.MaxReferences", blob);
    }

    [Fact]
    public void ProfileSections_have_no_hardcoded_dutch_consent_copy()
    {
        var root = FindRepoRoot();
        var dir = Path.Combine(root, "Jobsy.Web/Components/Candidate/ProfileSections");
        foreach (var file in Directory.EnumerateFiles(dir, "*.razor"))
        {
            var text = File.ReadAllText(file);
            Assert.DoesNotContain("Privacy en toestemming", text);
            Assert.DoesNotContain("Toestemming geven", text);
            Assert.DoesNotContain("Talentpooltoestemming intrekken", text);
            Assert.DoesNotContain("Bevestigingslink sturen", text);
            Assert.DoesNotContain("Anoniem zichtbaar worden", text);
        }
    }

    [Fact]
    public void Editor_caps_references_at_three()
        => Assert.Equal(3, CandidateProfileEditor.MaxReferences);

    [Fact]
    public void Passport_proof_tab_reuses_sections_and_rules()
    {
        var root = FindRepoRoot();
        var tab = File.ReadAllText(Path.Combine(root, "Jobsy.Web/Components/Candidate/Passport/PassportProofTab.razor"));
        Assert.Contains("ProofStrengthRules", tab);
        Assert.Contains("<ExperienceSection", tab);
        Assert.Contains("<CertificatesSection", tab);
        Assert.Contains("<ReferencesSection", tab);
        Assert.Contains("<CvSection", tab);
        Assert.Contains("filter-sheet", tab);
        Assert.Contains("CandidateProfileEditor.MaxReferences", tab);

        var passport = File.ReadAllText(Path.Combine(root, "Jobsy.Web/Components/Pages/Candidate/Passport.razor"));
        Assert.Contains("<PassportProofTab", passport);
        Assert.DoesNotContain("Passport.Transitional.OpenData", passport.Split("passport-panel-proof")[1].Split("passport-panel-data")[0]);
    }

    [Fact]
    public void Passport_data_tab_hosts_sections_and_employer_switches()
    {
        var root = FindRepoRoot();
        var tab = File.ReadAllText(Path.Combine(root, "Jobsy.Web/Components/Candidate/Passport/PassportDataTab.razor"));
        Assert.Contains("FeatureVisible", tab);
        Assert.Contains("PlatformFeature.Employers", tab);
        Assert.Contains("Profile.OpenForWork", tab);
        Assert.Contains("Passport.Data.TalentPool", tab);
        Assert.Contains("<PersonalSection", tab);
        Assert.Contains("<DevicesSection", tab);
        Assert.Contains("<PreferencesSection", tab);
        Assert.Contains("<AvailabilitySection", tab);
        Assert.Contains("<MotivationSection", tab);
        Assert.Contains("<ConsentSection", tab);
        Assert.Contains("<DeleteAccountSection", tab);
        Assert.Contains("UnsubscribeDialog", tab);
        Assert.Contains("aria-expanded", tab);
        Assert.Contains("aria-controls", tab);
        Assert.Contains("IsUnder16", File.ReadAllText(Path.Combine(root, "Jobsy.Web/Components/Candidate/ProfileSections/ConsentSection.razor")));
        Assert.Contains("OpenUnsubscribe", File.ReadAllText(Path.Combine(root, "Jobsy.Web/Components/Candidate/ProfileSections/DeleteAccountSection.razor")));

        var passport = File.ReadAllText(Path.Combine(root, "Jobsy.Web/Components/Pages/Candidate/Passport.razor"));
        Assert.Contains("<PassportDataTab", passport);
        Assert.DoesNotContain("Passport.Transitional.OpenData", passport);
        Assert.DoesNotContain("ClassicTabsUntilPhase4", File.ReadAllText(Path.Combine(root, "Jobsy.Web/Navigation/PassportRedirects.cs")));
    }

    private static string FindRepoRoot()
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
