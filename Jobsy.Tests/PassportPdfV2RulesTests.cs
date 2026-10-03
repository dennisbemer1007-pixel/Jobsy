using Jobsy.Core.Contracts;
using Jobsy.Core.Passport;

namespace Jobsy.Tests;

public class PassportPdfV2RulesTests
{
    [Fact]
    public void Greenhouse_title_suggests_groen_and_order_is_not_a_score()
    {
        var sectors = PassportSectorSuggestions.Suggest(
        [
            "Medewerker tuinbouw",
            "Medewerker tuinbouw",
            "Verzorgende IG"
        ]);

        Assert.Equal("groen", sectors[0]);
        Assert.Contains("zorg", sectors);
        Assert.DoesNotContain(sectors, sector => sector.Any(char.IsDigit));
    }

    [Fact]
    public void Reason_codes_are_facts_and_never_holland_or_strength()
    {
        var reasons = PassportSectorSuggestions.ReasonsFor(
            "logistiek",
            [new CandidateEmployerHistoryDto("PostNL", "orderpicker", Years: 2)],
            [new CandidateCertificateDto("VCA basis"), new CandidateCertificateDto("HACCP")],
            new SharedWorkPreferences(PhysicalWork: "lifting-15"));

        Assert.Contains(reasons, reason => reason.Code == "experience-2");
        Assert.Contains(reasons, reason => reason.Code.StartsWith("certificate-vca", StringComparison.Ordinal));
        Assert.Contains(reasons, reason => reason.Code == "pref-physical");
        Assert.DoesNotContain(reasons, reason => reason.Code.Contains("haccp", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(reasons, reason => reason.Code.StartsWith("holland-", StringComparison.Ordinal));
        Assert.DoesNotContain(reasons, reason => reason.Code.StartsWith("strength-", StringComparison.Ordinal));

        var kept = PassportSectorSuggestions.Sanitize(
        [
            new PassportSectorChoice("logistiek", ["holland-R", "experience-2", "strength-x"], "eigen regel", ["Orderpicker"])
        ]);
        Assert.Equal(["experience-2"], kept[0].ReasonCodes);
    }

    [Fact]
    public void Unconfirmed_cv_fields_are_dropped_until_edit_and_reset_on_reupload()
    {
        const string filled = "[\"over mij\",\"werkervaring\"]";
        Assert.Contains("over mij", PassportCvConfirmation.Unconfirmed(filled, null));
        var confirmed = PassportCvConfirmation.Confirm(null, "over mij");
        Assert.False(PassportCvConfirmation.IsDropped(filled, confirmed, "over mij"));
        Assert.True(PassportCvConfirmation.IsDropped(filled, confirmed, "werkervaring"));
        var reset = PassportCvConfirmation.ResetFilled(confirmed, filled);
        Assert.True(PassportCvConfirmation.IsDropped(filled, reset, "over mij"));
    }

    [Theory]
    [InlineData(0, false, "nee")]
    [InlineData(1, false, "in overleg")]
    [InlineData(2, false, "ja")]
    [InlineData(0, true, "in overleg")]
    public void Shifts_follow_the_day_count_table(int days, bool flexible, string expected)
    {
        var availability = new Dictionary<string, string[]>();
        foreach (var day in new[] { "Ma", "Di", "Wo" }.Take(days))
        {
            availability[day] = ["Ochtend"];
        }

        Assert.Equal(expected, PassportShiftRules.Derive("Ochtend", availability, flexible));
    }

    [Fact]
    public void Completeness_needs_every_share_field_and_one_test()
    {
        var ready = new CandidatePreferencesDto(
            [],
            30,
            "fiets",
            SpokenLanguages: [new CandidateLanguageDto("pl")],
            DutchLevel: "goed",
            DrivingLicenses: ["B"],
            Availability: new Dictionary<string, string[]> { ["Ma"] = ["Ochtend"] },
            MinHoursPerWeek: 24,
            WorkRegion: "Utrecht e.o.");
        Assert.True(PassportCompletenessRules.IsShareReady(ready, 1));
        Assert.False(PassportCompletenessRules.IsShareReady(ready, 0));
        Assert.False(PassportCompletenessRules.IsShareReady(ready with { WorkRegion = null }, 1));
    }

    [Fact]
    public void Verification_requires_phone_only_when_the_setting_is_on()
    {
        var dates = new DateTime?[] { DateTime.UtcNow, DateTime.UtcNow, DateTime.UtcNow, DateTime.UtcNow };
        var withoutPhone = PassportVerificationRules.Evaluate(dates, DateTime.UtcNow, null, phoneVerificationEnabled: false);
        Assert.True(withoutPhone.IsVerified);
        Assert.Equal(PassportVerificationRules.BadgeText, "Lobsy-geverifieerd");

        var phoneOn = PassportVerificationRules.Evaluate(dates, DateTime.UtcNow, null, phoneVerificationEnabled: true);
        Assert.False(phoneOn.IsVerified);
        Assert.True(phoneOn.PhoneRequired);
    }

    [Fact]
    public void Translation_is_used_only_when_approved_and_the_hash_still_matches()
    {
        const string source = "Lubię pracować w zespole";
        var hash = PassportTranslationRules.Hash(source);
        Assert.True(PassportTranslationRules.CanUse(source, hash, DateTime.UtcNow, "Ik werk graag in een team"));
        Assert.False(PassportTranslationRules.CanUse(source + "!", hash, DateTime.UtcNow, "Ik werk graag in een team"));
        Assert.False(PassportTranslationRules.CanUse(source, hash, null, "Ik werk graag in een team"));
    }

    [Fact]
    public void Public_id_uses_the_short_code_alphabet_and_groups_of_four()
    {
        var seen = new HashSet<string>();
        for (var i = 0; i < 20; i++)
        {
            var id = PassportPublicId.Create();
            Assert.Equal(12, id.Length);
            Assert.DoesNotContain(id, ch => "ILO01".Contains(ch));
            Assert.True(seen.Add(id));
            var display = PassportPublicId.Display(id);
            Assert.Equal(14, display.Length);
            Assert.Equal(2, display.Count(ch => ch == '-'));
        }
    }
}
