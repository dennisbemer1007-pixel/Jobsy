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
        var traits = Traits(lang, facts.Dna);
        var home = Home(lang, facts);
        var valueLines = ValueLines(lang, facts.Dna);
        var jobFits = JobFits(lang, facts);
        var quotes = Quotes(facts.ReferenceQuotes);
        var direction = Direction(lang, facts);
        var story = Story(lang, facts);

        return new PassportPdfModel(
            Language: lang,
            FullName: string.IsNullOrWhiteSpace(facts.FullName) ? "—" : facts.FullName.Trim(),
            Initials: Initials(facts.FullName),
            Tagline: null,
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
            Checked: Checked(lang, facts, testsDone, quotes.Count > 0),
            NotChecked:
            [
                PassportPdfStrings.T(lang, "NotBirth"),
                PassportPdfStrings.T(lang, "NotHealth"),
                PassportPdfStrings.T(lang, "NotScores"),
                PassportPdfStrings.T(lang, "NotAi"),
                PassportPdfStrings.T(lang, "IdNotChecked")
            ],
            GeneratedLabel: generated.ToString("d MMM yyyy", culture),
            GeneratedAtUtc: facts.GeneratedAtUtc,
            Story: story,
            HeroMeta: HeroMeta(lang, facts, showBadge, testsStatus),
            Chips: Chips(lang, facts, hasAvailabilitySignal),
            Traits: traits,
            HomeLines: home,
            ValueLines: valueLines,
            JobFits: jobFits,
            PracticalLine: PracticalLine(lang, facts, hasAvailabilitySignal, culture),
            PracticalSeek: PracticalSeek(lang, facts),
            Quotes: quotes,
            Direction: direction);
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
        var lang = JobsyLanguages.Normalize(facts.Language);
        var all = (facts.Experience ?? [])
            .Where(item => !string.IsNullOrWhiteSpace(item.Employer))
            .Select(item =>
            {
                var period = Period(lang, item);
                var hasRole = !string.IsNullOrWhiteSpace(item.Role);
                var title = hasRole ? item.Role!.Trim() : item.Employer.Trim();
                var place = hasRole ? item.Employer.Trim() : null;
                var meta = hasRole ? JoinMeta(item.Employer.Trim(), period) : period;
                var duties = Duties(item.Description);
                return new PassportExperienceLine(title, meta, duties.Count == 0 ? null : string.Join(" ", duties), duties, period, place);
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
            var items = LayerItems(layer, lang);
            if (layer is not { Done: true })
            {
                cards.Add(new PassportDnaCard(title, PassportPdfStrings.T(lang, "NotDone"), null, false));
                continue;
            }

            var words = items.Select(item => item.Label).Take(3).ToArray();
            var body = words.Length == 0
                ? PassportPdfStrings.T(lang, "Done")
                : string.Join(" · ", words);
            var when = layer.CompletedAtUtc?.ToLocalAmsterdam(culture);
            cards.Add(new PassportDnaCard(title, body, when, true));
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

    private static IReadOnlyList<string> Checked(string lang, PassportPdfFacts facts, int testsDone, bool quotesShared)
    {
        var lines = new List<string>();
        if (testsDone > 0)
        {
            lines.Add(PassportPdfStrings.F(lang, "DnaChecked", testsDone));
        }

        if (facts.EmailVerified)
        {
            lines.Add(PassportPdfStrings.T(lang, "EmailOk"));
        }

        if (facts.PhoneVerified)
        {
            lines.Add(PassportPdfStrings.T(lang, "PhoneOk"));
        }

        if (quotesShared)
        {
            lines.Add(PassportPdfStrings.T(lang, "RefsShared"));
        }

        return lines;
    }

    private static string? Story(string lang, PassportPdfFacts facts)
    {
        var parts = new List<string>();
        var own = Clip(facts.OwnWords, 320);
        if (own is not null)
        {
            parts.Add(Finish(own));
        }

        foreach (var job in (facts.Experience ?? []).Where(item => !string.IsNullOrWhiteSpace(item.Employer)).Take(2))
        {
            var current = IsCurrent(job);
            var sentence = string.IsNullOrWhiteSpace(job.Role)
                ? PassportPdfStrings.F(lang, current ? "StoryWorksPlace" : "StoryWorkedPlace", job.Employer.Trim())
                : PassportPdfStrings.F(lang, current ? "StoryWorks" : "StoryWorked", job.Employer.Trim(), job.Role.Trim());
            AddFresh(parts, sentence);
            var duties = Duties(job.Description);
            var duty = duties.Count == 0 ? null : duties[0];
            if (duty is not null)
            {
                AddFresh(parts, Finish(duty));
            }
        }

        var educations = (facts.Educations ?? [])
            .Where(item => !string.IsNullOrWhiteSpace(item))
            .Select(item => item.Trim())
            .Take(2)
            .ToArray();
        if (educations.Length > 0)
        {
            AddFresh(parts, PassportPdfStrings.F(lang, "StoryEducation", string.Join(", ", educations)));
        }

        var papers = (facts.Certificates ?? [])
            .Where(item => !string.IsNullOrWhiteSpace(item.Name))
            .Select(item => item.Name.Trim())
            .Take(3)
            .ToArray();
        if (papers.Length > 0)
        {
            AddFresh(parts, PassportPdfStrings.F(lang, "StoryPapers", string.Join(", ", papers)));
        }

        var roles = CleanList(facts.Roles, 3);
        if (roles.Count > 0)
        {
            AddFresh(parts, PassportPdfStrings.F(lang, "StorySeeks", string.Join(", ", roles)));
        }

        var hours = Hours(lang, facts.MinHours, facts.MaxHours);
        if (hours is not null)
        {
            AddFresh(parts, PassportPdfStrings.F(lang, "StoryHours", hours));
        }

        if (!string.IsNullOrWhiteSpace(facts.WorkRegion))
        {
            AddFresh(parts, PassportPdfStrings.F(lang, "StoryRegion", facts.WorkRegion.Trim()));
        }

        return parts.Count == 0 ? null : Clip(string.Join(" ", parts), 720);
    }

    private static void AddFresh(List<string> parts, string sentence)
    {
        var blob = string.Join(" ", parts);
        var probe = sentence.Trim().TrimEnd('.', '!', '?');
        if (probe.Length > 0 && blob.Contains(probe, StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        parts.Add(sentence);
    }

    private static string Finish(string text)
    {
        var trimmed = text.Trim();
        return trimmed.EndsWith('.') || trimmed.EndsWith('!') || trimmed.EndsWith('?')
            ? trimmed
            : trimmed + ".";
    }

    private static bool IsCurrent(PassportExperienceFact item)
        => !string.IsNullOrWhiteSpace(item.StartMonth) && string.IsNullOrWhiteSpace(item.EndMonth);

    private static IReadOnlyList<PassportGlossLine> Traits(string lang, IReadOnlyList<PassportDnaLayerFact> dna)
        => GlossLines(lang, PassportDnaLayer.Competence, LayerItems(dna.FirstOrDefault(x => x.Key == PassportDnaLayer.Competence), lang), 4);

    private static IReadOnlyList<PassportGlossLine> ValueLines(string lang, IReadOnlyList<PassportDnaLayerFact> dna)
        => GlossLines(lang, PassportDnaLayer.Values, LayerItems(dna.FirstOrDefault(x => x.Key == PassportDnaLayer.Values), lang), 4);

    private static IReadOnlyList<PassportGlossLine> GlossLines(
        string lang,
        string layer,
        IReadOnlyList<(string? Code, string Label)> items,
        int take)
    {
        var lines = new List<PassportGlossLine>();
        foreach (var item in items.Take(take))
        {
            var gloss = PassportTraitCopy.Gloss(layer, item.Code, lang)
                        ?? PassportPdfStrings.T(lang, "TraitFallback");
            lines.Add(new PassportGlossLine(item.Label, gloss));
        }

        return lines;
    }

    private static IReadOnlyList<string> Home(string lang, PassportPdfFacts facts)
    {
        var cultureLines = new List<string>();
        var culture = facts.Dna.FirstOrDefault(x => x.Key == PassportDnaLayer.Culture);
        foreach (var item in LayerItems(culture, lang).Take(3))
        {
            var gloss = PassportTraitCopy.Gloss(PassportDnaLayer.Culture, item.Code, lang);
            if (!string.IsNullOrWhiteSpace(gloss))
            {
                cultureLines.Add(gloss);
            }
        }

        var employerLines = new List<string>();
        if (facts.ShareEmployerPreferences)
        {
            foreach (var code in (facts.EmployerPreferences ?? [])
                         .Select(DiscoveryCatalogs.CanonicalEmployer)
                         .Where(code => code is not null)
                         .Take(2))
            {
                var key = "Employer." + code;
                if (PassportPdfStrings.HasKey(key))
                {
                    employerLines.Add(Capitalize(PassportPdfStrings.T(lang, key)));
                }
            }
        }

        var workLines = new List<string>();
        var work = facts.WorkPreferences;
        if (work is not null)
        {
            AddHome(workLines, lang, "Home.Indoor.", WorkPreferenceCatalogs.CanonicalIndoor(work.Indoor));
            AddHome(workLines, lang, "Home.Outdoor.", WorkPreferenceCatalogs.CanonicalOutdoor(work.Outdoor));
            AddHome(workLines, lang, "Home.Physical.", WorkPreferenceCatalogs.CanonicalPhysicalWork(work.PhysicalWork));
            AddHome(workLines, lang, "Home.Pace.", WorkPreferenceCatalogs.CanonicalPace(work.Pace));
        }

        return cultureLines.Take(2)
            .Concat(employerLines.Take(2))
            .Concat(workLines.Take(2))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Take(5)
            .ToArray();
    }

    private static void AddHome(List<string> lines, string lang, string prefix, string? code)
    {
        if (code is null)
        {
            return;
        }

        var key = prefix + code;
        if (PassportPdfStrings.HasKey(key))
        {
            lines.Add(PassportPdfStrings.T(lang, key));
        }
    }

    private static IReadOnlyList<PassportJobFit> JobFits(string lang, PassportPdfFacts facts)
    {
        var roles = CleanList(facts.Roles, 4);
        if (roles.Count == 0)
        {
            return [];
        }

        var whyBase = JobWhy(lang, facts);
        var didLine = PassportPdfStrings.T(lang, "DidThis");
        var self = PassportPdfStrings.T(lang, "JobWhySelf");
        var experience = facts.Experience ?? [];
        return roles.Select(role =>
        {
            var did = experience.Any(job =>
                (!string.IsNullOrWhiteSpace(job.Role)
                 && job.Role.Contains(role, StringComparison.OrdinalIgnoreCase))
                || (!string.IsNullOrWhiteSpace(job.Employer)
                    && role.Contains(job.Employer.Trim(), StringComparison.OrdinalIgnoreCase)));
            var why = did
                ? string.Equals(whyBase, self, StringComparison.Ordinal) ? didLine : whyBase + " · " + didLine
                : whyBase;
            return new PassportJobFit(Clip(role, 42)!, why);
        }).ToArray();
    }

    private static string JobWhy(string lang, PassportPdfFacts facts)
    {
        var bits = new List<string>();
        var careerItems = LayerItems(facts.Dna.FirstOrDefault(x => x.Key == PassportDnaLayer.Career), lang);
        var career = careerItems.Count == 0 ? default : careerItems[0];
        var gloss = PassportTraitCopy.Gloss(PassportDnaLayer.Career, career.Code, lang);
        if (!string.IsNullOrWhiteSpace(gloss))
        {
            bits.Add(gloss);
        }

        var pace = facts.WorkPreferences is null
            ? null
            : WorkPreferenceCatalogs.CanonicalPace(facts.WorkPreferences.Pace);
        if (pace is not null && PassportPdfStrings.HasKey("Home.Pace." + pace))
        {
            bits.Add(LowerFirst(PassportPdfStrings.T(lang, "Home.Pace." + pace)));
        }

        return bits.Count == 0
            ? PassportPdfStrings.T(lang, "JobWhySelf")
            : string.Join(" · ", bits);
    }

    private static IReadOnlyList<PassportReferenceQuote> Quotes(IReadOnlyList<PassportReferenceQuote>? quotes)
        => (quotes ?? [])
            .Where(quote => !string.IsNullOrWhiteSpace(quote.Quote) && !string.IsNullOrWhiteSpace(quote.Attribution))
            .Select(quote => new PassportReferenceQuote(quote.Attribution.Trim(), Clip(quote.Quote.Trim(), 240)!))
            .Where(quote => !quote.Quote.Contains('%') && !quote.Attribution.Contains('%'))
            .Take(2)
            .ToArray();

    private static IReadOnlyList<PassportDirectionStep> Direction(string lang, PassportPdfFacts facts)
    {
        var jobs = (facts.Experience ?? [])
            .Where(item => !string.IsNullOrWhiteSpace(item.Employer))
            .Select(item => (
                Title: Short(string.IsNullOrWhiteSpace(item.Role) ? item.Employer : item.Role!, 28),
                Current: IsCurrent(item),
                Start: item.StartMonth ?? ""))
            .OrderBy(item => item.Start, StringComparer.Ordinal)
            .ToList();
        var steps = new List<PassportDirectionStep>();
        var shown = jobs.TakeLast(3).ToList();
        for (var i = 0; i < shown.Count; i++)
        {
            var last = i == shown.Count - 1;
            var state = last && shown[i].Current
                ? PassportPdfStrings.T(lang, "StepNow")
                : PassportPdfStrings.T(lang, "StepDone");
            steps.Add(new PassportDirectionStep(shown[i].Title, state));
        }

        var dream = Dream(facts.DreamTitle);
        if (dream is not null && steps.All(step => !string.Equals(step.Title, dream, StringComparison.OrdinalIgnoreCase)))
        {
            steps.Add(new PassportDirectionStep(dream, PassportPdfStrings.T(lang, "StepLater")));
        }

        if (steps.Count < 2 && dream is null)
        {
            return [];
        }

        return steps.Take(4).ToArray();
    }

    private static string? Dream(string? title)
    {
        var text = Blank(title);
        if (text is null
            || text.Equals("Weet ik nog niet", StringComparison.OrdinalIgnoreCase)
            || text.Equals("I don't know yet", StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        return Clip(text, 32);
    }

    private static string? HeroMeta(string lang, PassportPdfFacts facts, bool showBadge, string testsStatus)
    {
        var bits = new List<string>();
        if (!string.IsNullOrWhiteSpace(facts.WorkRegion))
        {
            bits.Add(facts.WorkRegion.Trim());
        }

        if (facts.UserId is Guid id && id != Guid.Empty)
        {
            bits.Add(PassportMemberNumber.Format(id));
        }

        bits.Add(showBadge ? PassportPdfStrings.T(lang, "Badge") + " · " + testsStatus : testsStatus);
        return string.Join(" · ", bits);
    }

    private static IReadOnlyList<PassportChip> Chips(string lang, PassportPdfFacts facts, bool available)
    {
        var chips = new List<PassportChip>();
        if (available)
        {
            chips.Add(new PassportChip(PassportPdfStrings.T(lang, "AvailableChip"), "available"));
        }

        var role = CleanList(facts.Roles, 1).FirstOrDefault();
        if (role is not null)
        {
            chips.Add(new PassportChip(Clip(role, 28)!, "role"));
        }

        if (facts.OpenForWork)
        {
            chips.Add(new PassportChip(PassportPdfStrings.T(lang, "OpenForWork"), "open"));
        }

        return chips;
    }

    private static string? PracticalLine(string lang, PassportPdfFacts facts, bool available, CultureInfo culture)
    {
        var bits = new List<string>();
        if (!string.IsNullOrWhiteSpace(facts.WorkRegion))
        {
            bits.Add(facts.WorkRegion.Trim());
        }

        var hours = Hours(lang, facts.MinHours, facts.MaxHours);
        if (hours is not null)
        {
            bits.Add(hours);
        }

        if (available)
        {
            var from = AvailableLabel(lang, facts.AvailableFrom, culture);
            if (from is not null)
            {
                bits.Add(from);
            }
        }

        var transport = Transport(lang, facts);
        if (transport is not null)
        {
            bits.Add(transport);
        }

        if (facts.MaxTravelMinutes is > 0 and <= 300)
        {
            bits.Add(PassportPdfStrings.F(lang, "MaxTravel", facts.MaxTravelMinutes.Value));
        }

        if (facts.HasOwnCar is bool car)
        {
            bits.Add(PassportPdfStrings.F(lang, "OwnCar", PassportPdfStrings.T(lang, car ? "Yes" : "No")));
        }

        var languages = Languages(lang, facts);
        if (languages.Count > 0)
        {
            bits.Add(PassportPdfStrings.T(lang, "Languages") + ": " + string.Join(", ", languages.Select(line =>
                string.IsNullOrWhiteSpace(line.Value) || line.Value == "—" ? line.Label : line.Label + " " + line.Value)));
        }

        if (facts.IncludeContact)
        {
            if (!string.IsNullOrWhiteSpace(facts.Phone))
            {
                bits.Add(facts.Phone.Trim());
            }

            if (facts.WhatsApp && !string.IsNullOrWhiteSpace(facts.Phone))
            {
                bits.Add(PassportPdfStrings.T(lang, "WhatsApp"));
            }

            if (!string.IsNullOrWhiteSpace(facts.Email))
            {
                bits.Add(facts.Email.Trim());
            }
        }

        return bits.Count == 0 ? null : string.Join(" · ", bits);
    }

    private static string PracticalSeek(string lang, PassportPdfFacts facts)
    {
        var roles = CleanList(facts.Roles, 3);
        var contracts = WorkPreferenceCatalogs.NormalizeContracts(facts.ContractPreferences)
            .Where(code => PassportPdfStrings.HasKey("Contract." + code))
            .Select(code => PassportPdfStrings.T(lang, "Contract." + code))
            .ToArray();
        var seek = new List<string>();
        if (roles.Count > 0)
        {
            seek.Add(string.Join(" / ", roles));
        }

        if (contracts.Length > 0)
        {
            seek.Add(string.Join(", ", contracts));
        }

        var hint = PassportPdfStrings.T(lang, "Page2Hint");
        return seek.Count == 0
            ? hint
            : PassportPdfStrings.F(lang, "SeeksLine", string.Join(" · ", seek)) + " · " + hint;
    }

    private static IReadOnlyList<(string? Code, string Label)> LayerItems(PassportDnaLayerFact? layer, string lang)
    {
        if (layer is not { Done: true })
        {
            return [];
        }

        var words = layer.Words
            .Where(word => !string.IsNullOrWhiteSpace(word) && !word.Contains('%'))
            .Select(word => word.Trim())
            .ToList();
        var codes = layer.Codes ?? [];
        var items = new List<(string? Code, string Label)>();
        for (var i = 0; i < words.Count; i++)
        {
            var code = i < codes.Count && !string.IsNullOrWhiteSpace(codes[i])
                ? codes[i]
                : CodeFor(layer.Key, words[i]);
            var label = string.IsNullOrWhiteSpace(code) ? words[i] : DimensionLabels.For(code, lang);
            if (string.IsNullOrWhiteSpace(label))
            {
                label = words[i];
            }

            items.Add((code, label));
        }

        return items;
    }

    private static string? CodeFor(string layer, string label)
    {
        foreach (var code in Codes(layer))
        {
            if (SameLabel(DimensionLabels.For(code), label)
                || SameLabel(DimensionLabels.For(code, "en"), label)
                || SameLabel(DimensionLabels.For(code, "pl"), label)
                || SameLabel(DimensionLabels.For(code, "ro"), label)
                || SameLabel(DimensionLabels.For(code, "ar"), label))
            {
                return code;
            }
        }

        return null;
    }

    private static bool SameLabel(string? left, string right)
        => string.Equals(left?.Trim(), right.Trim(), StringComparison.OrdinalIgnoreCase);

    private static IEnumerable<string> Codes(string layer) => layer switch
    {
        PassportDnaLayer.Competence => CompetencyTestCatalog.QuickScanCategories,
        PassportDnaLayer.Career => CareerTestCatalog.RiasecCodes,
        PassportDnaLayer.Culture => CulturePersonalityCatalog.CultureDimensionCodes
            .Concat(CulturePersonalityCatalog.PersonalityFacetCodes)
            .Distinct(StringComparer.Ordinal),
        PassportDnaLayer.Values => SchwartzValuesCatalog.CategoryCodes,
        _ => []
    };

    private static IReadOnlyList<string> Duties(string? description)
    {
        var text = Blank(description);
        if (text is null)
        {
            return [];
        }

        return text.Split(['\n', '\r', ';', '•'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(part => part.Trim().TrimStart('-', '–', '—').Trim())
            .Where(part => part.Length > 0)
            .Select(part => Clip(part, 110)!)
            .Take(3)
            .ToArray();
    }

    private static string? Period(string lang, PassportExperienceFact item)
    {
        var formatted = LobsyCvModelFactory.FormatEmployerPeriod(item.StartMonth, item.EndMonth, item.Years);
        if (formatted is null)
        {
            return null;
        }

        var start = LobsyCvModelFactory.NormalizeMonth(item.StartMonth);
        var end = LobsyCvModelFactory.NormalizeMonth(item.EndMonth);
        return start is not null && end is null
            ? formatted.Replace("heden", PassportPdfStrings.T(lang, "NowWord"), StringComparison.Ordinal)
            : formatted;
    }

    private static List<string> CleanList(IReadOnlyList<string>? values, int take)
    {
        var list = new List<string>();
        foreach (var value in values ?? [])
        {
            if (string.IsNullOrWhiteSpace(value) || value.Contains('%'))
            {
                continue;
            }

            var text = value.Trim();
            list.Add(text.Length > 48 ? text[..48].TrimEnd() + "…" : text);
            if (list.Count == take)
            {
                break;
            }
        }

        return list;
    }

    private static string Capitalize(string text)
        => string.IsNullOrEmpty(text) ? text : char.ToUpperInvariant(text[0]) + text[1..];

    private static string LowerFirst(string text)
        => string.IsNullOrEmpty(text) ? text : char.ToLowerInvariant(text[0]) + text[1..];

    private static string Short(string text, int max)
        => Clip(text.Trim(), max) ?? text.Trim();

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
