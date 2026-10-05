using System.Globalization;
using System.Text;
using Jobsy.Core.Contracts;
using Jobsy.Core.Localization;
using Jobsy.Core.Rules;
using Jobsy.Core.Time;

namespace Jobsy.Core.Passport;

public static class PassportPdfModelBuilder
{
    public const int MaxExperience = 6;
    public const int MaxCertificates = 6;
    public const int MaxEducations = 4;
    public const int MaxHowIWork = 5;
    public const int MaxStrengths = 6;
    public const int MaxOwnWords = 420;

    public static PassportPdfModel Build(PassportPdfFacts facts)
    {
        ArgumentNullException.ThrowIfNull(facts);
        var lang = JobsyLanguages.Normalize(facts.Language);
        var culture = Culture(lang);
        var shifts = DeriveShifts(facts.Availability, facts.FlexibleTimes);
        var shiftNote = ShiftNote(lang, shifts, facts.FlexibleTimes, facts.Availability);
        var strengths = Strengths(facts.Dna);
        var generated = AmsterdamTime.ToLocal(facts.GeneratedAtUtc);
        var testsDone = facts.Dna.Count(layer => layer.Done);
        var showBadge = testsDone == PassportDnaLayer.Keys.Length
                        && facts.EmailVerified
                        && (!facts.PhoneVerificationRequired || facts.PhoneVerified);
        var lastTest = facts.Dna
            .Where(layer => layer.Done && layer.CompletedAtUtc is not null)
            .Select(layer => layer.CompletedAtUtc!.Value)
            .OrderByDescending(x => x)
            .Cast<DateTime?>()
            .FirstOrDefault();
        var testsStatus = PassportPdfStrings.F(lang, "TestsProgress", testsDone);
        var badgeDetail = showBadge
            ? lastTest is DateTime doneAt
                ? testsStatus + " · " + doneAt.ToLocalAmsterdam(culture)
                : testsStatus
            : null;

        var experience = Experience(facts);
        var certificates = Certificates(facts);
        var hasAvailabilitySignal = facts.OpenForWork
                                    || facts.MinHours is not null
                                    || facts.MaxHours is not null
                                    || shifts.Count > 0
                                    || facts.AvailableFrom is not null;

        return new PassportPdfModel(
            Language: lang,
            FullName: string.IsNullOrWhiteSpace(facts.FullName) ? "—" : facts.FullName.Trim(),
            Initials: Initials(facts.FullName),
            Tagline: strengths.Count == 0 ? null : string.Join(" · ", strengths.Take(2)),
            Region: Blank(facts.WorkRegion),
            OpenForWork: facts.OpenForWork,
            PassportNumber: facts.UserId is Guid id && id != Guid.Empty
                ? PassportMemberNumber.Format(id)
                : null,
            AvailableFrom: hasAvailabilitySignal ? AvailableLabel(lang, facts.AvailableFrom, culture) : null,
            Hours: Hours(lang, facts.MinHours, facts.MaxHours),
            Shifts: shifts.Count == 0 ? [] : shifts.Select(s => new PassportShiftChip(
                PassportPdfStrings.T(lang, "Part." + s.Code),
                PassportPdfStrings.T(lang, s.Kind switch
                {
                    PassportShiftKind.Yes => "ShiftYes",
                    PassportShiftKind.Consult => "ShiftConsult",
                    _ => "ShiftNo"
                }),
                s.Kind)).ToArray(),
            ShiftNote: shiftNote,
            TransportLine: Transport(lang, facts),
            TravelLine: facts.MaxTravelMinutes is > 0 and <= 300
                ? PassportPdfStrings.F(lang, "MaxTravel", facts.MaxTravelMinutes.Value)
                : null,
            OwnCarLine: facts.HasOwnCar is bool car
                ? PassportPdfStrings.F(lang, "OwnCar", PassportPdfStrings.T(lang, car ? "Yes" : "No"))
                : null,
            Email: facts.IncludeContact ? Blank(facts.Email) : null,
            Phone: facts.IncludeContact ? Blank(facts.Phone) : null,
            WhatsApp: facts.IncludeContact && facts.WhatsApp && !string.IsNullOrWhiteSpace(facts.Phone),
            Languages: Languages(lang, facts),
            DutchNote: DutchNote(lang, facts.DutchLevel),
            WorkPreferences: WorkPreferences(lang, facts),
            Strengths: strengths,
            StrengthSource: strengths.Count == 0 || lastTest is null
                ? null
                : PassportPdfStrings.F(lang, "StrengthSource", lastTest.Value.ToLocalAmsterdam(culture)),
            SoughtLine: Sought(lang, facts),
            Experience: experience.Lines,
            ExperienceMore: experience.More,
            Certificates: certificates.Lines,
            CertificatesMore: certificates.More,
            Educations: Educations(facts),
            Dna: Dna(lang, facts.Dna, culture),
            HowIWork: HowIWork(lang, facts),
            OwnWords: Clip(facts.OwnWords, MaxOwnWords),
            Motivation: Same(facts.Motivation, facts.OwnWords) ? null : Clip(facts.Motivation, 240),
            ShowBadge: showBadge,
            BadgeDetail: badgeDetail,
            TestsPlainStatus: testsStatus,
            Checked: Checked(lang, facts, culture),
            NotChecked:
            [
                PassportPdfStrings.T(lang, "NotIdentity"),
                PassportPdfStrings.T(lang, "NotDiplomas"),
                PassportPdfStrings.T(lang, "NotReferences")
            ],
            GeneratedLabel: generated.ToString("d MMM yyyy", culture),
            GeneratedAtUtc: facts.GeneratedAtUtc);
    }

    public static IReadOnlyList<(string Code, PassportShiftKind Kind)> DeriveShifts(
        IReadOnlyDictionary<string, string[]>? availability,
        bool flexibleTimes)
    {
        var hasSlots = availability is { Count: > 0 };
        if (!hasSlots && !flexibleTimes)
        {
            return [];
        }

        var result = new List<(string Code, PassportShiftKind Kind)>(DayPartMatrix.DayPartCodes.Length);
        foreach (var part in DayPartMatrix.DayPartCodes)
        {
            var days = hasSlots
                ? DayPartMatrix.DaysWithDayPart(availability!, part).Count
                : 0;
            var kind = days >= 2
                ? PassportShiftKind.Yes
                : days == 1 || flexibleTimes
                    ? PassportShiftKind.Consult
                    : PassportShiftKind.No;
            result.Add((part, kind));
        }

        return result;
    }

    private static string? ShiftNote(
        string lang,
        IReadOnlyList<(string Code, PassportShiftKind Kind)> shifts,
        bool flexibleTimes,
        IReadOnlyDictionary<string, string[]>? availability)
    {
        if (shifts.Count == 0)
        {
            return null;
        }

        if (flexibleTimes && (availability is null || availability.Count == 0))
        {
            return PassportPdfStrings.T(lang, "TimesConsult");
        }

        var bits = new List<string>();
        foreach (var shift in shifts)
        {
            if (shift.Kind == PassportShiftKind.Consult)
            {
                bits.Add(PassportPdfStrings.T(lang, "Consult." + shift.Code));
            }
            else if (shift.Kind == PassportShiftKind.No)
            {
                bits.Add(PassportPdfStrings.T(lang, "No." + shift.Code));
            }
        }

        return bits.Count == 0 ? null : string.Join(" · ", bits);
    }

    private static string? AvailableLabel(string lang, DateOnly? from, CultureInfo culture)
    {
        if (from is null)
        {
            return PassportPdfStrings.T(lang, "Direct");
        }

        return from.Value.ToString("d MMM yyyy", culture);
    }

    private static string? Hours(string lang, decimal? min, decimal? max)
    {
        if (min is null && max is null)
        {
            return null;
        }

        var low = min ?? max;
        var high = max ?? min;
        var text = low == high
            ? Trim(low!.Value)
            : Trim(Math.Min(low!.Value, high!.Value)) + "–" + Trim(Math.Max(low.Value, high.Value));
        return PassportPdfStrings.F(lang, "Hours", text);
    }

    private static string? Transport(string lang, PassportPdfFacts facts)
    {
        var bits = new List<string>();
        var licenses = (facts.Licenses ?? [])
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Select(x => x.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Take(4)
            .ToArray();
        if (licenses.Length > 0)
        {
            bits.Add(PassportPdfStrings.F(lang, "Licence", string.Join(", ", licenses)));
        }

        foreach (var part in TransportLabels.SplitMany(facts.PreferredTransport))
        {
            var key = "Transport." + part;
            bits.Add(PassportPdfStrings.HasKey(key) ? PassportPdfStrings.T(lang, key) : part);
        }

        return bits.Count == 0 ? null : string.Join(" · ", bits);
    }

    private static IReadOnlyList<PassportTextLine> Languages(string lang, PassportPdfFacts facts)
    {
        var lines = new List<PassportTextLine>();
        var dutch = DiscoveryCatalogs.CanonicalDutch(facts.DutchLevel);
        if (dutch is not null && PassportPdfStrings.HasKey("Dutch." + dutch))
        {
            lines.Add(new PassportTextLine(
                PassportPdfStrings.T(lang, "Dutch"),
                PassportPdfStrings.T(lang, "Dutch." + dutch)));
        }

        foreach (var spoken in facts.SpokenLanguages ?? [])
        {
            if (string.IsNullOrWhiteSpace(spoken.Code))
            {
                continue;
            }

            var code = spoken.Code.Trim().ToLowerInvariant();
            if (code == "nl" && dutch is not null)
            {
                continue;
            }

            var nameKey = "Lang." + code;
            var name = PassportPdfStrings.HasKey(nameKey)
                ? PassportPdfStrings.T(lang, nameKey)
                : code.ToUpperInvariant();
            var level = string.IsNullOrWhiteSpace(spoken.Level) ? null : spoken.Level.Trim();
            lines.Add(new PassportTextLine(name, level ?? "—"));
            if (lines.Count == 6)
            {
                break;
            }
        }

        return lines;
    }

    private static string? DutchNote(string lang, string? dutchLevel)
    {
        var dutch = DiscoveryCatalogs.CanonicalDutch(dutchLevel);
        if (dutch is null)
        {
            return null;
        }

        var key = "DutchNote." + dutch;
        return PassportPdfStrings.HasKey(key) ? PassportPdfStrings.T(lang, key) : null;
    }

    private static IReadOnlyList<PassportTextLine> WorkPreferences(string lang, PassportPdfFacts facts)
    {
        var lines = new List<PassportTextLine>();
        var work = facts.WorkPreferences;
        if (work is not null)
        {
            AddWork(lines, lang, "Indoor", "Work.Indoor.", WorkPreferenceCatalogs.CanonicalIndoor(work.Indoor));
            AddWork(lines, lang, "Outdoor", "Work.Outdoor.", WorkPreferenceCatalogs.CanonicalOutdoor(work.Outdoor));
            AddWork(lines, lang, "Physical", "Work.Physical.", WorkPreferenceCatalogs.CanonicalPhysicalWork(work.PhysicalWork));
            AddWork(lines, lang, "Pace", "Work.Pace.", WorkPreferenceCatalogs.CanonicalPace(work.Pace));
        }

        if (facts.ShareEmployerPreferences)
        {
            var labels = (facts.EmployerPreferences ?? [])
                .Select(DiscoveryCatalogs.CanonicalEmployer)
                .Where(code => code is not null && PassportPdfStrings.HasKey("Employer." + code))
                .Select(code => PassportPdfStrings.T(lang, "Employer." + code))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .Take(3)
                .ToArray();
            if (labels.Length > 0)
            {
                lines.Add(new PassportTextLine(
                    PassportPdfStrings.T(lang, "Environment"),
                    string.Join(" · ", labels)));
            }
        }

        return lines;
    }

    private static void AddWork(List<PassportTextLine> lines, string lang, string labelKey, string prefix, string? code)
    {
        if (code is null || !PassportPdfStrings.HasKey(prefix + code))
        {
            return;
        }

        lines.Add(new PassportTextLine(
            PassportPdfStrings.T(lang, labelKey),
            PassportPdfStrings.T(lang, prefix + code)));
    }

    private static IReadOnlyList<string> Strengths(IReadOnlyList<PassportDnaLayerFact> dna)
    {
        var words = new List<string>();
        foreach (var key in PassportDnaLayer.Keys)
        {
            var layer = dna.FirstOrDefault(x => x.Key == key);
            if (layer is not { Done: true })
            {
                continue;
            }

            foreach (var word in layer.Words)
            {
                if (string.IsNullOrWhiteSpace(word) || word.Contains('%'))
                {
                    continue;
                }

                if (words.Contains(word, StringComparer.OrdinalIgnoreCase))
                {
                    continue;
                }

                words.Add(word.Trim());
                if (words.Count == MaxStrengths)
                {
                    return words;
                }
            }
        }

        return words;
    }

    private static string? Sought(string lang, PassportPdfFacts facts)
    {
        var bits = new List<string>();
        foreach (var role in facts.Roles ?? [])
        {
            var text = role.Trim();
            if (text.Length == 0 || text.Contains('%'))
            {
                continue;
            }

            bits.Add(text.Length > 40 ? text[..40].TrimEnd() + "…" : text);
            if (bits.Count == 3)
            {
                break;
            }
        }

        foreach (var code in WorkPreferenceCatalogs.NormalizeContracts(facts.ContractPreferences))
        {
            if (PassportPdfStrings.HasKey("Contract." + code))
            {
                bits.Add(PassportPdfStrings.T(lang, "Contract." + code));
            }
        }

        return bits.Count == 0 ? null : string.Join(" · ", bits);
    }

    private static (IReadOnlyList<PassportExperienceLine> Lines, int More) Experience(PassportPdfFacts facts)
    {
        var all = (facts.Experience ?? [])
            .Where(item => !string.IsNullOrWhiteSpace(item.Employer))
            .Select(item =>
            {
                var period = LobsyCvModelFactory.FormatEmployerPeriod(item.StartMonth, item.EndMonth, item.Years);
                var title = string.IsNullOrWhiteSpace(item.Role)
                    ? item.Employer.Trim()
                    : item.Role.Trim();
                var meta = string.IsNullOrWhiteSpace(item.Role)
                    ? period
                    : JoinMeta(item.Employer.Trim(), period);
                return new PassportExperienceLine(title, meta, Clip(item.Description, 90));
            })
            .ToList();

        if (all.Count == 0 && facts.ExperienceCountWithoutNames > 0)
        {
            return (
            [
                new PassportExperienceLine(
                    PassportPdfStrings.F(facts.Language, "ExperienceCount", facts.ExperienceCountWithoutNames),
                    null,
                    null)
            ],
            0);
        }

        var more = Math.Max(0, all.Count - MaxExperience);
        return (all.Take(MaxExperience).ToArray(), more);
    }

    private static (IReadOnlyList<string> Lines, int More) Certificates(PassportPdfFacts facts)
    {
        var all = (facts.Certificates ?? [])
            .Where(c => !string.IsNullOrWhiteSpace(c.Name))
            .Select(c => c.Year is int year ? $"{c.Name.Trim()} · {year}" : c.Name.Trim())
            .ToList();
        var more = Math.Max(0, all.Count - MaxCertificates);
        return (all.Take(MaxCertificates).ToArray(), more);
    }

    private static IReadOnlyList<string> Educations(PassportPdfFacts facts)
    {
        var lines = (facts.Educations ?? [])
            .Where(e => !string.IsNullOrWhiteSpace(e))
            .Select(e => e.Trim())
            .Take(MaxEducations)
            .ToList();
        if (!string.IsNullOrWhiteSpace(facts.EducationDirection)
            && lines.All(line => !line.Contains(facts.EducationDirection, StringComparison.OrdinalIgnoreCase)))
        {
            if (lines.Count == 0)
            {
                lines.Add(facts.EducationDirection.Trim());
            }
            else
            {
                lines[0] = lines[0] + " · " + facts.EducationDirection.Trim();
            }
        }

        return lines;
    }

    private static IReadOnlyList<PassportDnaCard> Dna(
        string lang,
        IReadOnlyList<PassportDnaLayerFact> layers,
        CultureInfo culture)
    {
        var cards = new List<PassportDnaCard>(4);
        foreach (var key in PassportDnaLayer.Keys)
        {
            var layer = layers.FirstOrDefault(x => x.Key == key);
            var title = PassportPdfStrings.T(lang, "Layer." + key);
            if (layer is not { Done: true })
            {
                cards.Add(new PassportDnaCard(title, PassportPdfStrings.T(lang, "NotDone"), null));
                continue;
            }

            var words = layer.Words.Where(w => !string.IsNullOrWhiteSpace(w) && !w.Contains('%')).Take(3).ToArray();
            var body = words.Length == 0
                ? PassportPdfStrings.T(lang, "Done")
                : string.Join(" · ", words);
            var when = layer.CompletedAtUtc?.ToLocalAmsterdam(culture);
            cards.Add(new PassportDnaCard(title, body, when));
        }

        return cards;
    }

    private static IReadOnlyList<string> HowIWork(string lang, PassportPdfFacts facts)
    {
        var lines = new List<string>();
        foreach (var pref in WorkPreferences(lang, facts))
        {
            lines.Add(pref.Label + ": " + pref.Value);
            if (lines.Count == MaxHowIWork)
            {
                return lines;
            }
        }

        var culture = facts.Dna.FirstOrDefault(x => x.Key == PassportDnaLayer.Culture);
        var word = culture is { Done: true }
            ? culture.Words.FirstOrDefault(w => !string.IsNullOrWhiteSpace(w) && !w.Contains('%'))
            : null;
        if (!string.IsNullOrWhiteSpace(word) && lines.Count < MaxHowIWork)
        {
            lines.Add(PassportPdfStrings.T(lang, "Layer.culture") + ": " + word.Trim());
        }

        return lines;
    }

    private static IReadOnlyList<string> Checked(string lang, PassportPdfFacts facts, CultureInfo culture)
    {
        var lines = new List<string>();
        foreach (var key in PassportDnaLayer.Keys)
        {
            var layer = facts.Dna.FirstOrDefault(x => x.Key == key);
            if (layer is not { Done: true })
            {
                continue;
            }

            var name = PassportPdfStrings.T(lang, "Layer." + key);
            lines.Add(layer.CompletedAtUtc is DateTime at
                ? name + " · " + at.ToLocalAmsterdam(culture)
                : name);
        }

        if (facts.EmailVerified)
        {
            lines.Add(PassportPdfStrings.T(lang, "EmailOk"));
        }

        if (facts.PhoneVerified)
        {
            lines.Add(PassportPdfStrings.T(lang, "PhoneOk"));
        }

        return lines;
    }

    private static string Initials(string? fullName)
    {
        if (string.IsNullOrWhiteSpace(fullName))
        {
            return "XX";
        }

        var sb = new StringBuilder();
        foreach (var part in fullName.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            var letter = part.FirstOrDefault(char.IsLetter);
            if (letter == default)
            {
                continue;
            }

            sb.Append(char.ToUpperInvariant(letter));
            if (sb.Length == 3)
            {
                break;
            }
        }

        return sb.Length == 0 ? "XX" : sb.ToString();
    }

    private static string? JoinMeta(string employer, string? period)
        => string.IsNullOrWhiteSpace(period) ? employer : employer + " · " + period;

    private static string? Clip(string? value, int max)
    {
        var text = Blank(value);
        if (text is null || text.Length <= max)
        {
            return text;
        }

        return text[..(max - 1)].TrimEnd() + "…";
    }

    private static string? Blank(string? value)
        => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static bool Same(string? left, string? right)
        => string.Equals(Blank(left), Blank(right), StringComparison.Ordinal);

    private static string Trim(decimal hours)
        => hours.ToString("0.##", CultureInfo.InvariantCulture);

    private static string ToLocalAmsterdam(this DateTime utc, CultureInfo culture)
        => AmsterdamTime.ToLocal(utc).ToString("d MMM yyyy", culture);

    private static CultureInfo Culture(string lang) => lang switch
    {
        "en" => CultureInfo.GetCultureInfo("en-GB"),
        "pl" => CultureInfo.GetCultureInfo("pl-PL"),
        "ro" => CultureInfo.GetCultureInfo("ro-RO"),
        "ar" => CultureInfo.GetCultureInfo("ar-SA"),
        _ => CultureInfo.GetCultureInfo("nl-NL")
    };
}
