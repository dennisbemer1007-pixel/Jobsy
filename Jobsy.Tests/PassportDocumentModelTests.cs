using System.Collections;
using System.Reflection;
using System.Text.Json;
using System.Text.RegularExpressions;
using Jobsy.Api.Controllers;
using Jobsy.Core.Contracts;
using Jobsy.Core.Passport;

namespace Jobsy.Tests;

public class PassportDocumentModelTests
{
    private static readonly Regex Forbidden = new(
        "(?i)(dateofbirth|ageyears|^age$|nationalit|photo|^bsn$|health|homeaddress|postcode|postal|latitude|longitude|coordinate|dislike|percent|score|\\bfit\\b|rank|match|whoami|rolefit|culturefit|holland|riasec|\\btip\\b|strength|competenc|personality|answers)",
        RegexOptions.CultureInvariant);

    [Fact]
    public void Whitelist_matches_every_reachable_property_and_blocks_the_never_list()
    {
        var found = new List<string>();
        Walk(typeof(PassportDocumentModel), found, []);
        var allowed = PassportDocumentModel.Whitelist.ToHashSet(StringComparer.Ordinal);

        Assert.Equal(allowed.Count, found.Count);
        foreach (var path in found)
        {
            Assert.Contains(path, allowed);
            var name = path.Split('.')[^1];
            Assert.DoesNotMatch(Forbidden, name);
        }
    }

    [Fact]
    public void Sector_choices_round_trip_and_drop_unknown_or_test_outcome_reasons()
    {
        var json = MeController.SerializePreferences(new CandidatePreferencesDto(
            ["orderpicker"],
            20,
            "Fiets",
            PassportSectors:
            [
                new PassportSectorChoice("logistiek", ["experience-2", "holland-R", "strength-social"], "Ik werk graag in een team dat vroeg begint", ["orderpicker"]),
                new PassportSectorChoice("onbekend", ["experience-1"], null, []),
                new PassportSectorChoice("zorg", ["experience-1"], null, ["verzorgende"])
            ]));

        var parsed = MeController.ParsePreferences(json);
        Assert.Equal(2, parsed.PassportSectors!.Count);
        Assert.Equal("logistiek", parsed.PassportSectors[0].Code);
        Assert.Contains("experience-2", parsed.PassportSectors[0].ReasonCodes);
        Assert.DoesNotContain(parsed.PassportSectors[0].ReasonCodes, code => code.StartsWith("holland-", StringComparison.Ordinal));
        Assert.DoesNotContain(parsed.PassportSectors[0].ReasonCodes, code => code.StartsWith("strength-", StringComparison.Ordinal));
        Assert.DoesNotContain(parsed.PassportSectors, sector => sector.Code == "onbekend");
    }

    [Fact]
    public void Unconfirmed_cv_fields_and_the_ai_marker_stay_off_the_document()
    {
        var prefs = new CandidatePreferencesDto(
            ["orderpicker"],
            30,
            "Fiets",
            AboutMe: "ZZ-AI-MARKER-CV hello",
            DrivingLicenses: ["B"],
            Employers: [new CandidateEmployerHistoryDto("PostNL", "orderpicker", StartMonth: "2022-01")],
            Educations: ["MBO"],
            Certificates: [new CandidateCertificateDto("VCA")],
            MinHoursPerWeek: 24,
            DutchLevel: "goed",
            SpokenLanguages: [new CandidateLanguageDto("nl")],
            WorkRegion: "Utrecht e.o.");

        var dropped = PassportDocumentFactory.Build(Input(prefs, filled: """["over mij","werkervaring","opleiding","certificaten","gewenste rollen","rijbewijs","telefoon"]"""));
        Assert.Null(dropped.AboutMe);
        Assert.Empty(dropped.Experience);
        Assert.Empty(dropped.Education);
        Assert.Empty(dropped.Certificates);
        Assert.Empty(dropped.Roles);
        Assert.Empty(dropped.Licences);
        Assert.Null(dropped.Contact!.Phone);
        Assert.DoesNotContain("ZZ-AI-MARKER-CV", JsonSerializer.Serialize(dropped), StringComparison.Ordinal);

        var confirmed = PassportDocumentFactory.Build(Input(
            prefs,
            filled: """["over mij"]""",
            confirmed: """["over mij"]"""));
        Assert.Contains("ZZ-AI-MARKER-CV", confirmed.AboutMe, StringComparison.Ordinal);
        Assert.Equal("MKF", confirmed.Initials);
    }

    private static PassportBuildInput Input(CandidatePreferencesDto prefs, string? filled = null, string? confirmed = null)
        => new(
            UserId: Guid.Parse("11111111-1111-1111-1111-111111111111"),
            FullName: "Marta Kowalska (fictief)",
            OpenForWork: true,
            AvailableFromDate: null,
            Preferences: prefs,
            FilledFieldsJson: filled,
            ConfirmedFieldsJson: confirmed,
            Tests: [new PassportTestStatus("Carrière", true, new DateTime(2026, 1, 2, 0, 0, 0, DateTimeKind.Utc))],
            EmailVerifiedAtUtc: new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            PhoneVerifiedAtUtc: null,
            PhoneVerificationEnabled: false,
            Email: "marta@example.com",
            Phone: "0612345678",
            IncludeContact: true,
            WhatsAppOk: true,
            PrimaryLanguage: "nl",
            SecondaryLanguage: null,
            IncludesPage2: false,
            PublicId: null,
            QrUrl: null,
            GeneratedAtUtc: new DateTime(2026, 10, 3, 0, 0, 0, DateTimeKind.Utc),
            CoBrand: null);

    private static void Walk(Type type, List<string> found, HashSet<Type> seen)
    {
        if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>))
        {
            Walk(type.GetGenericArguments()[0], found, seen);
            return;
        }

        if (type != typeof(string) && typeof(IEnumerable).IsAssignableFrom(type))
        {
            if (type.IsGenericType)
            {
                Walk(type.GetGenericArguments()[0], found, seen);
            }

            return;
        }

        if (type.IsPrimitive || type.IsEnum || type == typeof(string) || type == typeof(decimal) || type == typeof(DateTime) || type == typeof(DateOnly) || type == typeof(Guid))
        {
            return;
        }

        if (!seen.Add(type))
        {
            return;
        }

        foreach (var prop in type.GetProperties(BindingFlags.Public | BindingFlags.Instance))
        {
            if (prop.GetIndexParameters().Length > 0)
            {
                continue;
            }

            found.Add(type.Name + "." + prop.Name);
            Walk(prop.PropertyType, found, seen);
        }
    }
}
