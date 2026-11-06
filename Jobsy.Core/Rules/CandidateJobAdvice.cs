using System.Text.RegularExpressions;
using Jobsy.Core.Careers;
using Jobsy.Core.Localization;

namespace Jobsy.Core.Rules;

/// <summary>
/// Deterministic coach answers for job lists, catalogue comparisons and a short motivation.
/// Always the same <see cref="CareerCompassBuilder.Listed"/> list as the report. No stated likes.
/// </summary>
public static class CandidateJobAdvice
{
    public static bool Handles(string? question)
        => !string.IsNullOrWhiteSpace(question)
           && (LooksLikeMotivation(question)
               || LooksLikeComparison(question)
               || LooksLikeJobsList(question)
               || LooksLikeWorkClaim(question)
               || LooksLikeEmployerName(question)
               || CandidateCoachScript.Handles(question));

    public static string? TryReply(
        string? language,
        string question,
        RiasecScores? scores,
        string? education,
        bool hasWorkExperience,
        IReadOnlyList<string>? workLines = null,
        IReadOnlyList<(string Code, int Score)>? competence = null,
        IReadOnlyList<string>? certificates = null,
        bool employersEnabled = true)
    {
        var reply = Reply(
            language, question, scores, education, hasWorkExperience, workLines, competence, certificates, employersEnabled);
        return string.IsNullOrWhiteSpace(reply) ? null : CandidateCoachPolish.Apply(reply);
    }

    private static string? Reply(
        string? language,
        string question,
        RiasecScores? scores,
        string? education,
        bool hasWorkExperience,
        IReadOnlyList<string>? workLines,
        IReadOnlyList<(string Code, int Score)>? competence,
        IReadOnlyList<string>? certificates,
        bool employersEnabled)
    {
        if (string.IsNullOrWhiteSpace(question))
        {
            return null;
        }

        var lang = JobsyLanguages.Normalize(language);
        if (LooksLikeEmployerName(question) && !employersEnabled)
        {
            return EmployerNameHidden(lang);
        }

        if (LooksLikeWorkClaim(question))
        {
            return WorkClaim(lang, question, workLines);
        }

        if (scores is not { IsComplete: true })
        {
            return null;
        }

        var scripted = CandidateCoachScript.TryReply(language, question, scores, education, competence, certificates);
        if (scripted is not null)
        {
            return scripted;
        }

        if (LooksLikeMotivation(question))
        {
            return Motivation(lang, question, scores, education, hasWorkExperience, workLines);
        }

        if (LooksLikeComparison(question))
        {
            return Comparison(lang, question, scores, education);
        }

        if (LooksLikeJobsList(question))
        {
            return JobsList(lang, scores, education);
        }

        return null;
    }

    public static bool LooksLikeMotivation(string text)
    {
        var fold = text.ToLowerInvariant();
        return fold.Contains("motivatie", StringComparison.Ordinal)
               || fold.Contains("motivation", StringComparison.Ordinal)
               || fold.Contains("motywac", StringComparison.Ordinal)
               || fold.Contains("motivat", StringComparison.Ordinal)
               || fold.Contains("cover letter", StringComparison.Ordinal)
               || fold.Contains("sollicitatiebrief", StringComparison.Ordinal)
               || fold.Contains("رسالة", StringComparison.Ordinal);
    }

    public static bool LooksLikeComparison(string text)
    {
        var fold = text.ToLowerInvariant();
        var compareWord = fold.Contains("beter", StringComparison.Ordinal)
                          || fold.Contains("better", StringComparison.Ordinal)
                          || fold.Contains("lepiej", StringComparison.Ordinal)
                          || fold.Contains("mai bine", StringComparison.Ordinal)
                          || fold.Contains("vergelijk", StringComparison.Ordinal)
                          || fold.Contains("compare", StringComparison.Ordinal)
                          || fold.Contains("porówn", StringComparison.Ordinal)
                          || fold.Contains("compar", StringComparison.Ordinal)
                          || fold.Contains("أفضل", StringComparison.Ordinal)
                          || fold.Contains("مقارنة", StringComparison.Ordinal)
                          || fold.Contains("verschil", StringComparison.Ordinal)
                          || fold.Contains("difference", StringComparison.Ordinal)
                          || fold.Contains("różnic", StringComparison.Ordinal)
                          || fold.Contains("roznc", StringComparison.Ordinal)
                          || fold.Contains("diferen", StringComparison.Ordinal)
                          || fold.Contains("الفرق", StringComparison.Ordinal)
                          || OfOrOfPattern.IsMatch(fold);
        return compareWord && TitlesIn(text).Count >= 1;
    }

    /// <summary>A work-history claim such as "did I work as a forklift driver for five years?".</summary>
    public static bool LooksLikeWorkClaim(string text)
    {
        var fold = text.ToLowerInvariant();
        var work = fold.Contains("gewerkt", StringComparison.Ordinal)
                   || fold.Contains("worked", StringComparison.Ordinal)
                   || fold.Contains("work as", StringComparison.Ordinal)
                   || fold.Contains("pracowa", StringComparison.Ordinal)
                   || fold.Contains("lucrat", StringComparison.Ordinal)
                   || fold.Contains("عملت", StringComparison.Ordinal)
                   || fold.Contains("أعمل", StringComparison.Ordinal);
        if (!work)
        {
            return false;
        }

        return YearWord.IsMatch(fold)
               || TitlesIn(text).Count > 0
               || fold.Contains("heftruck", StringComparison.Ordinal)
               || fold.Contains("forklift", StringComparison.Ordinal)
               || fold.Contains("wózek", StringComparison.Ordinal)
               || fold.Contains("wozek", StringComparison.Ordinal)
               || fold.Contains("stivuitor", StringComparison.Ordinal)
               || fold.Contains("رافعة", StringComparison.Ordinal);
    }

    public static bool LooksLikeEmployerName(string text)
    {
        var fold = text.ToLowerInvariant();
        var employer = fold.Contains("werkgever", StringComparison.Ordinal)
                       || fold.Contains("employer", StringComparison.Ordinal)
                       || fold.Contains("pracodawc", StringComparison.Ordinal)
                       || fold.Contains("angajator", StringComparison.Ordinal)
                       || fold.Contains("صاحب العمل", StringComparison.Ordinal);
        if (!employer)
        {
            return false;
        }

        return fold.Contains("naam", StringComparison.Ordinal)
               || fold.Contains("name", StringComparison.Ordinal)
               || fold.Contains("nazw", StringComparison.Ordinal)
               || fold.Contains("nume", StringComparison.Ordinal)
               || fold.Contains("اسم", StringComparison.Ordinal)
               || fold.Contains("wie is", StringComparison.Ordinal)
               || fold.Contains("who is", StringComparison.Ordinal)
               || fold.Contains("kto jest", StringComparison.Ordinal)
               || fold.Contains("cine este", StringComparison.Ordinal);
    }

    public static bool LooksLikeJobsList(string text)
    {
        if (IsVacancyHunt(text))
        {
            return false;
        }

        var fold = text.ToLowerInvariant();
        return fold.Contains("beroep", StringComparison.Ordinal)
               || fold.Contains("welke baan", StringComparison.Ordinal)
               || fold.Contains("welke banen", StringComparison.Ordinal)
               || fold.Contains("passen bij", StringComparison.Ordinal)
               || fold.Contains("past bij", StringComparison.Ordinal)
               || fold.Contains("which job", StringComparison.Ordinal)
               || fold.Contains("what job", StringComparison.Ordinal)
               || fold.Contains("jobs fit", StringComparison.Ordinal)
               || fold.Contains("suit me", StringComparison.Ordinal)
               || fold.Contains("zawod", StringComparison.Ordinal)
               || fold.Contains("meseri", StringComparison.Ordinal)
               || fold.Contains("وظائف", StringComparison.Ordinal)
               || fold.Contains("مهن", StringComparison.Ordinal)
               || fold.Contains("تناسب", StringComparison.Ordinal);
    }

    public static IReadOnlyList<string> TitlesIn(string question)
    {
        var folded = CareerOccupationKeys.Fold(question);
        var hits = new List<(string Title, int Len)>();
        foreach (var job in OccupationCatalog.Shared.All)
        {
            if (job.Nl.Length >= 4)
            {
                TryAdd(hits, folded, job.Nl, job.Nl);
            }
        }

        foreach (var entry in CareerDreamCatalog.All)
        {
            TryAdd(hits, folded, entry.Title, entry.Title);
            foreach (var alias in entry.Aliases)
            {
                TryAdd(hits, folded, alias, entry.Title);
            }
        }

        foreach (var (stem, title) in new (string Stem, string Title)[]
                 {
                     ("kierowc", "Chauffeur"),
                     ("sofer", "Chauffeur"),
                     ("driver", "Chauffeur"),
                     ("السائق", "Chauffeur")
                 })
        {
            var stemFold = CareerOccupationKeys.Fold(stem);
            if (folded.Split(' ', StringSplitOptions.RemoveEmptyEntries)
                .Any(word => word.Equals(stemFold, StringComparison.Ordinal)
                             || word.StartsWith(stemFold, StringComparison.Ordinal)))
            {
                hits.Add((title, stemFold.Length + 1));
            }
        }

        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var ordered = new List<string>();
        foreach (var hit in hits.OrderByDescending(h => h.Len).ThenBy(h => h.Title, StringComparer.OrdinalIgnoreCase))
        {
            if (seen.Add(hit.Title))
            {
                ordered.Add(hit.Title);
            }
        }

        return ordered;
    }

    private static void TryAdd(List<(string Title, int Len)> hits, string foldedQuestion, string needle, string title)
    {
        var fold = CareerOccupationKeys.Fold(needle);
        if (fold.Length >= 3 && CareerOccupationKeys.Hits(foldedQuestion, fold))
        {
            hits.Add((title, fold.Length));
        }
    }

    private static bool IsVacancyHunt(string text)
    {
        var fold = text.ToLowerInvariant();
        return fold.Contains("vacature", StringComparison.Ordinal)
               || fold.Contains("vacanc", StringComparison.Ordinal)
               || fold.Contains("sollicit", StringComparison.Ordinal)
               || fold.Contains("banenkaart", StringComparison.Ordinal)
               || fold.Contains("job map", StringComparison.Ordinal)
               || fold.Contains("ofert", StringComparison.Ordinal);
    }

    private static string JobsList(string lang, RiasecScores scores, string? education)
    {
        var listed = CareerCompassBuilder.Listed(scores, education);
        var lines = listed.Select((job, index) =>
            $"{index + 1}. {OccupationTitles.ForChat(job.Title, lang)} ({CareerCompassBuilder.FormatPercent(job.Percent, lang)}%)");
        var list = JoinParts(lang, lines.ToList());
        var (label, score) = TopDirection(scores, lang);
        return lang switch
        {
            "en" => $"Your test shows these jobs fit you best: {list}. You score highest on {label} ({score}%).",
            "pl" => $"Z twojego testu wynika, że te zawody pasują najlepiej: {list}. Najwyższy wynik masz w {label} ({score}%).",
            "ro" => $"Din testul tău reiese că aceste meserii ți se potrivesc cel mai bine: {list}. Scorul tău cel mai mare este la {label} ({score}%).",
            "ar" => $"يظهر من اختبارك أن هذه المهن تناسبك أكثر: {list}. أعلى درجة لك هي في {label} ({score}%).",
            _ => $"Uit je test blijkt dat deze beroepen het best bij je passen: {list}. Je scoort het hoogst op {label} ({score}%)."
        };
    }

    private static string Comparison(string lang, string question, RiasecScores scores, string? education)
    {
        var titles = TitlesIn(question).ToList();
        string? preface = null;
        var mentionsChauffeur = titles.RemoveAll(title =>
                string.Equals(title, "Chauffeur", StringComparison.OrdinalIgnoreCase)) > 0
            || CareerOccupationKeys.Fold(question).Split(' ', StringSplitOptions.RemoveEmptyEntries)
                .Contains("chauffeur");
        if (mentionsChauffeur
            && !titles.Any(title => CareerOccupationKeys.Fold(title).Contains("chauffeur", StringComparison.Ordinal)))
        {
            titles.Insert(0, "vrachtwagenchauffeur");
            preface = lang switch
            {
                "en" => "Driver is not one job in the list. We compare lorry driver.",
                "pl" => "Kierowca nie jest jednym zawodem na liście. Porównujemy kierowcę ciężarówki.",
                "ro" => "Șofer nu este o singură meserie în listă. Comparăm șofer de camion.",
                "ar" => "سائق ليست مهنة واحدة في القائمة. نقارن سائق الشاحنة.",
                _ => "Chauffeur staat niet als één beroep in de lijst. We vergelijken vrachtwagenchauffeur."
            };
        }

        string Shown(decimal value) => CareerCompassBuilder.FormatPercent(value, lang);
        string WithPreface(string text) => string.IsNullOrWhiteSpace(preface) ? text : preface + " " + text;

        if (titles.Count == 0)
        {
            return WithPreface(JobsList(lang, scores, education));
        }

        var scored = titles
            .Select(title => (Title: title, Percent: CareerCompassBuilder.CatalogueFit(title, scores, education)))
            .OrderByDescending(x => x.Percent)
            .ThenBy(x => x.Title, StringComparer.OrdinalIgnoreCase)
            .Take(2)
            .ToList();
        var best = scored[0];
        var bestName = OccupationTitles.ForChat(best.Title, lang);
        if (best.Percent is not decimal bestPercent)
        {
            var names = string.Join(lang == "en" ? " and " : " en ", scored.Select(item => OccupationTitles.ForChat(item.Title, lang)));
            return WithPreface(lang switch
            {
                "en" => $"We have no reliable score for {names}.",
                "pl" => $"Nie mamy pewnego wyniku dla: {names}.",
                "ro" => $"Nu avem un scor sigur pentru {names}.",
                "ar" => $"ليس لدينا درجة موثوقة لـ {names}.",
                _ => $"Voor {names} hebben we geen betrouwbare score."
            });
        }

        if (scored.Count == 1)
        {
            return WithPreface(lang switch
            {
                "en" => $"Your test shows {bestName} fits you at {Shown(bestPercent)}%.",
                "pl" => $"Z twojego testu wynika, że {bestName} pasuje w {Shown(bestPercent)}%.",
                "ro" => $"Din testul tău reiese că {bestName} ți se potrivește în proporție de {Shown(bestPercent)}%.",
                "ar" => $"يظهر من اختبارك أن {bestName} يناسبك بنسبة {Shown(bestPercent)}%.",
                _ => $"Uit je test blijkt dat {bestName} bij je past, met {Shown(bestPercent)}%."
            });
        }

        var other = scored[1];
        var otherName = OccupationTitles.ForChat(other.Title, lang);
        if (other.Percent is not decimal otherPercent)
        {
            return WithPreface(lang switch
            {
                "en" => $"{bestName} fits you at {Shown(bestPercent)}%. We have no reliable score for {otherName}.",
                "pl" => $"{bestName} pasuje w {Shown(bestPercent)}%. Nie mamy pewnego wyniku dla {otherName}.",
                "ro" => $"{bestName} ți se potrivește în proporție de {Shown(bestPercent)}%. Nu avem un scor sigur pentru {otherName}.",
                "ar" => $"{bestName} يناسبك بنسبة {Shown(bestPercent)}%. ليس لدينا درجة موثوقة لـ {otherName}.",
                _ => $"{bestName} past bij je, met {Shown(bestPercent)}%. Voor {otherName} hebben we geen betrouwbare score."
            });
        }

        var bestLetters = LetterLabels(best.Title, lang);
        var otherLetters = LetterLabels(other.Title, lang);
        var bestShown = Shown(bestPercent);
        var otherShown = Shown(otherPercent);
        if (bestPercent == otherPercent)
        {
            var shared = SharedLetterLabels(best.Title, other.Title, lang);
            if (shared.Count > 0 && SameLetters(best.Title, other.Title))
            {
                var both = JoinLabels(lang, shared);
                return WithPreface(lang switch
                {
                    "en" => $"Both fit you equally ({bestShown}%); both ask for {both}.",
                    "pl" => $"Oba pasują tak samo ({bestShown}%); oba wymagają {both}.",
                    "ro" => $"Ambele ți se potrivesc la fel ({bestShown}%); ambele cer {both}.",
                    "ar" => $"كلاهما يناسبك بنفس الدرجة ({bestShown}%)؛ كلاهما يطلب {both}.",
                    _ => $"Beide passen even goed ({bestShown}%); ze vragen allebei {both}."
                });
            }

            var left = JoinLabels(lang, bestLetters);
            var right = JoinLabels(lang, otherLetters);
            return WithPreface(lang switch
            {
                "en" => $"Both fit you equally ({bestShown}%). {bestName} asks for {left}. {otherName} asks for {right}.",
                "pl" => $"Oba pasują tak samo ({bestShown}%). {bestName} wymaga {left}. {otherName} wymaga {right}.",
                "ro" => $"Ambele ți se potrivesc la fel ({bestShown}%). {bestName} cere {left}. {otherName} cere {right}.",
                "ar" => $"كلاهما يناسبك بنفس الدرجة ({bestShown}%). {bestName} يطلب {left}. {otherName} يطلب {right}.",
                _ => $"Beide passen even goed ({bestShown}%). {bestName} vraagt {left}. {otherName} vraagt {right}."
            });
        }

        var bestAsk = JoinLabels(lang, bestLetters);
        var otherAsk = JoinLabels(lang, otherLetters);
        return WithPreface(lang switch
        {
            "en" => $"{bestName} fits you better ({bestShown}%) than {otherName} ({otherShown}%). {bestName} asks for {bestAsk}. {otherName} asks for {otherAsk}.",
            "pl" => $"{bestName} pasuje lepiej ({bestShown}%) niż {otherName} ({otherShown}%). {bestName} wymaga {bestAsk}. {otherName} wymaga {otherAsk}.",
            "ro" => $"{bestName} ți se potrivește mai bine ({bestShown}%) decât {otherName} ({otherShown}%). {bestName} cere {bestAsk}. {otherName} cere {otherAsk}.",
            "ar" => $"{bestName} يناسبك أكثر ({bestShown}%) من {otherName} ({otherShown}%). {bestName} يطلب {bestAsk}. {otherName} يطلب {otherAsk}.",
            _ => $"{bestName} past beter ({bestShown}%) dan {otherName} ({otherShown}%). {bestName} vraagt {bestAsk}. {otherName} vraagt {otherAsk}."
        });
    }


    private static string NoScoreReply(string lang) => lang switch
    {
        "en" => "We have no reliable source to compare this job with your profile.",
        "pl" => "Nie mamy pewnego źródła, aby porównać ten zawód z Twoim profilem.",
        "ro" => "Nu avem o sursă sigură ca să comparăm această meserie cu profilul tău.",
        "ar" => "ليس لدينا مصدر موثوق لنقارن هذه المهنة بملفك.",
        _ => OccupationCopy.NoScoreSentence
    };

    private static string WorkClaim(string lang, string question, IReadOnlyList<string>? workLines)
    {
        var lines = (workLines ?? [])
            .Where(line => !string.IsNullOrWhiteSpace(line))
            .Select(line => OccupationTitles.LocalizeWorkLine(line, lang))
            .Where(line => line.Length > 0)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
        var asked = AskedWorkNeedles(question);
        var hit = lines.FirstOrDefault(line =>
            asked.Any(needle => line.Contains(needle, StringComparison.OrdinalIgnoreCase)));
        if (hit is not null && !YearsDisagree(question, hit))
        {
            return lang switch
            {
                "en" => $"Yes. Your profile lists {hit}.",
                "pl" => $"Tak. W twoim profilu jest {hit}.",
                "ro" => $"Da. În profilul tău este {hit}.",
                "ar" => $"نعم. في ملفك {hit}.",
                _ => $"Ja. In je profiel staat {hit}."
            };
        }

        if (lines.Count == 0)
        {
            return lang switch
            {
                "en" => "No, that is not in your profile. You have no work in your profile yet.",
                "pl" => "Nie, tego nie ma w twoim profilu. Nie masz jeszcze pracy w profilu.",
                "ro" => "Nu, asta nu este în profilul tău. Nu ai încă muncă în profil.",
                "ar" => "لا، هذا ليس في ملفك. لا يوجد عمل في ملفك بعد.",
                _ => "Nee, dat staat niet in je profiel. Je hebt nog geen werk in je profiel."
            };
        }

        var only = JoinParts(lang, lines);
        return lang switch
        {
            "en" => $"No, that is not in your profile. You only have {only}.",
            "pl" => $"Nie, tego nie ma w twoim profilu. Masz tylko {only}.",
            "ro" => $"Nu, asta nu este în profilul tău. Ai doar {only}.",
            "ar" => $"لا، هذا ليس في ملفك. لديك فقط {only}.",
            _ => $"Nee, dat staat niet in je profiel. Je hebt alleen {only}."
        };
    }

    private static string EmployerNameHidden(string lang) => lang switch
    {
        "en" => "The name is not shown.",
        "pl" => "Nazwa nie jest pokazywana.",
        "ro" => "Numele nu este arătat.",
        "ar" => "الاسم غير معروض.",
        _ => "De naam wordt niet getoond."
    };

    private static List<string> AskedWorkNeedles(string question)
    {
        var needles = new List<string>();
        foreach (var title in TitlesIn(question))
        {
            needles.Add(title);
            var head = title.Split('/')[0].Trim();
            if (head.Length >= 4)
            {
                needles.Add(head);
            }
        }

        var fold = question.ToLowerInvariant();
        if (fold.Contains("heftruck", StringComparison.Ordinal)
            || fold.Contains("forklift", StringComparison.Ordinal)
            || fold.Contains("wózek", StringComparison.Ordinal)
            || fold.Contains("wozek", StringComparison.Ordinal)
            || fold.Contains("stivuitor", StringComparison.Ordinal)
            || fold.Contains("رافعة", StringComparison.Ordinal))
        {
            needles.Add("heftruck");
            needles.Add("Heftruck");
            needles.Add("forklift");
        }

        return needles;
    }

    private static bool YearsDisagree(string question, string line)
    {
        var asked = Regex.Match(question, @"\b(\d{1,2})\s*(jaar|years|lat|ani)\b", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
        if (!asked.Success)
        {
            return false;
        }

        var inLine = Regex.Match(line, @"\((\d{1,2})\s*jaar\)", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
        return inLine.Success && inLine.Groups[1].Value != asked.Groups[1].Value;
    }

    private static List<string> LetterLabels(string title, string lang)
    {
        var weights = CareerCompassBuilder.WeightsFor(title);
        var labels = weights
            .Select(weight => DimensionLabels.For(weight.Code, lang))
            .Where(label => !string.IsNullOrWhiteSpace(label))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
        if (labels.Count == 0)
        {
            labels.Add(DimensionLabels.For(CareerCompassBuilder.PrimaryCode(title), lang));
        }

        return labels;
    }

    private static List<string> SharedLetterLabels(string left, string right, string lang)
    {
        var rightCodes = CareerCompassBuilder.WeightsFor(right).Select(weight => weight.Code).ToHashSet(StringComparer.OrdinalIgnoreCase);
        return CareerCompassBuilder.WeightsFor(left)
            .Where(weight => rightCodes.Contains(weight.Code))
            .Select(weight => DimensionLabels.For(weight.Code, lang))
            .Where(label => !string.IsNullOrWhiteSpace(label))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private static bool SameLetters(string left, string right)
    {
        var a = CareerCompassBuilder.WeightsFor(left).Select(weight => weight.Code).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var b = CareerCompassBuilder.WeightsFor(right).Select(weight => weight.Code).ToHashSet(StringComparer.OrdinalIgnoreCase);
        return a.SetEquals(b);
    }

    private static string JoinLabels(string lang, IReadOnlyList<string> labels) => JoinParts(lang, labels);

    private static string JoinParts(string lang, IReadOnlyList<string> items)
    {
        if (items.Count == 0)
        {
            return "";
        }

        if (items.Count == 1)
        {
            return items[0];
        }

        var and = lang switch
        {
            "pl" => "i",
            "ro" => "și",
            "ar" => "و",
            "en" => "and",
            _ => "en"
        };
        var comma = lang is "ar" ? "، " : ", ";
        return items.Count == 2
            ? $"{items[0]} {and} {items[1]}"
            : string.Join(comma, items.Take(items.Count - 1)) + " " + and + " " + items[^1];
    }

    private static readonly Regex OfOrOfPattern = new(
        @"\bof\b.+\bof\b",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Compiled);

    private static readonly Regex YearWord = new(
        @"\b(jaar|years|year|lat|ani)\b|سنوات|سنة",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Compiled);

    private static string Motivation(
        string lang,
        string question,
        RiasecScores scores,
        string? education,
        bool hasWorkExperience,
        IReadOnlyList<string>? workLines)
    {
        var titles = TitlesIn(question);
        var named = titles.Count > 0 ? titles[0] : null;
        var listed = CareerCompassBuilder.Listed(scores, education);
        var top = listed.Count > 0 ? listed[0] : null;
        var job = named ?? top?.Title ?? "werk";
        var percent = named is null
            ? top?.Percent ?? CareerCompassBuilder.CatalogueFit(job, scores, education)
            : CareerCompassBuilder.CatalogueFit(named, scores, education);
        if (percent is not decimal fit)
        {
            return NoScoreReply(lang);
        }

        var shownFit = CareerCompassBuilder.FormatPercent(fit);

        var shown = OccupationTitles.ForChat(job, lang);
        var ownLetters = CareerCompassBuilder.WeightsFor(job);
        var directionCode = ownLetters.Count > 0
            ? ownLetters[0].Code
            : CareerCompassBuilder.PrimaryCode(job);
        var directionScore = scores.Get(directionCode);
        var direction = DimensionLabels.For(directionCode, lang);
        var directionFits = directionScore >= 50;
        var work = hasWorkExperience && workLines is { Count: > 0 }
            ? JoinParts(lang, workLines.Take(2).ToList())
            : null;

        if (string.IsNullOrWhiteSpace(work))
        {
            return lang switch
            {
                "en" => $"You have no work experience in your profile yet. Your test shows {shown} fits you at {shownFit}%. Short motivation: I want to start as {shown}. {MotivationClaim("en", direction, directionScore, directionFits)}",
                "pl" => $"W twoim profilu nie ma jeszcze doświadczenia w pracy. Z testu wynika, że {shown} pasuje w {shownFit}%. Krótka motywacja: Chcę zacząć jako {shown}. {MotivationClaim("pl", direction, directionScore, directionFits)}",
                "ro" => $"Nu ai încă experiență de muncă în profil. Din test reiese că {shown} ți se potrivește în proporție de {shownFit}%. Motivație scurtă: Vreau să încep ca {shown}. {MotivationClaim("ro", direction, directionScore, directionFits)}",
                "ar" => $"لا توجد خبرة عمل في ملفك بعد. يظهر من اختبارك أن {shown} يناسبك بنسبة {shownFit}%. دافع قصير: أريد أن أبدأ كـ {shown}. {MotivationClaim("ar", direction, directionScore, directionFits)}",
                _ => $"Je hebt nog geen werkervaring in je profiel. Uit je test blijkt dat {shown} bij je past, met {shownFit}%. Korte motivatie: Ik wil aan de slag als {shown}. {MotivationClaim("nl", direction, directionScore, directionFits)}"
            };
        }

        return lang switch
        {
            "en" => $"Your profile lists this work: {work}. Your test shows {shown} fits you at {shownFit}%. Short motivation: I want to work as {shown}. {MotivationClaim("en", direction, directionScore, directionFits)}",
            "pl" => $"W twoim profilu jest ta praca: {work}. Z testu wynika, że {shown} pasuje w {shownFit}%. Krótka motywacja: Chcę pracować jako {shown}. {MotivationClaim("pl", direction, directionScore, directionFits)}",
            "ro" => $"În profilul tău este această muncă: {work}. Din test reiese că {shown} ți se potrivește în proporție de {shownFit}%. Motivație scurtă: Vreau să lucrez ca {shown}. {MotivationClaim("ro", direction, directionScore, directionFits)}",
            "ar" => $"في ملفك هذا العمل: {work}. يظهر من اختبارك أن {shown} يناسبك بنسبة {shownFit}%. دافع قصير: أريد أن أعمل كـ {shown}. {MotivationClaim("ar", direction, directionScore, directionFits)}",
            _ => $"In je profiel staat dit werk: {work}. Uit je test blijkt dat {shown} bij je past, met {shownFit}%. Korte motivatie: Ik wil werken als {shown}. {MotivationClaim("nl", direction, directionScore, directionFits)}"
        };
    }

    private static string MotivationClaim(string lang, string direction, int score, bool fits)
    {
        if (!fits)
        {
            return lang switch
            {
                "en" => $"My test shows that I score {score}% on {direction}.",
                "pl" => $"Z mojego testu wynika, że mam {score}% w {direction}.",
                "ro" => $"Din testul meu reiese că am {score}% la {direction}.",
                "ar" => $"يظهر من اختباري أن درجتي {score}% في {direction}.",
                _ => $"Uit mijn test blijkt dat ik {score}% scoor op {direction}."
            };
        }

        return lang switch
        {
            "en" => $"My test shows that {direction} ({score}%) fits me.",
            "pl" => $"Z mojego testu wynika, że {direction} ({score}%) do mnie pasuje.",
            "ro" => $"Din testul meu reiese că {direction} ({score}%) mi se potrivește.",
            "ar" => $"يظهر من اختباري أن {direction} ({score}%) يناسبني.",
            _ => $"Uit mijn test blijkt dat {direction} ({score}%) bij mij past."
        };
    }

    private static (string Label, int Score) TopDirection(RiasecScores scores, string lang)
    {
        var top = CareerTestCatalog.RiasecCodes
            .Select(code => (Code: code, Score: scores.Get(code)))
            .OrderByDescending(x => x.Score)
            .ThenBy(x => x.Code, StringComparer.Ordinal)
            .First();
        return (DimensionLabels.For(top.Code, lang), top.Score);
    }
}
