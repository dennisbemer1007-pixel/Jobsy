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
               || CandidateCoachScript.Handles(question));

    public static string? TryReply(
        string? language,
        string question,
        RiasecScores? scores,
        string? education,
        bool hasWorkExperience,
        IReadOnlyList<string>? workLines = null,
        IReadOnlyList<(string Code, int Score)>? competence = null,
        IReadOnlyList<string>? certificates = null)
    {
        if (scores is not { IsComplete: true } || string.IsNullOrWhiteSpace(question))
        {
            return null;
        }

        var scripted = CandidateCoachScript.TryReply(language, question, scores, education, competence, certificates);
        if (scripted is not null)
        {
            return scripted;
        }

        var lang = JobsyLanguages.Normalize(language);
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
                          || fold.Contains("مقارنة", StringComparison.Ordinal);
        return compareWord && TitlesIn(text).Count >= 1;
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
            $"{index + 1}. {OccupationTitles.ForChat(job.Title, lang)} ({job.Percent}%)");
        var list = string.Join(", ", lines);
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
        var titles = TitlesIn(question);
        if (titles.Count == 0)
        {
            return JobsList(lang, scores, education);
        }

        var scored = titles
            .Select(title => (Title: title, Percent: CareerCompassBuilder.CatalogueFit(title, scores, education)))
            .OrderByDescending(x => x.Percent)
            .ThenBy(x => x.Title, StringComparer.OrdinalIgnoreCase)
            .Take(2)
            .ToList();
        var best = scored[0];
        var bestName = OccupationTitles.ForChat(best.Title, lang);
        if (best.Percent is not int bestPercent)
        {
            return NoScoreReply(lang);
        }

        if (scored.Count == 1)
        {
            return lang switch
            {
                "en" => $"Your test shows {bestName} fits you at {bestPercent}%.",
                "pl" => $"Z twojego testu wynika, że {bestName} pasuje w {bestPercent}%.",
                "ro" => $"Din testul tău reiese că {bestName} ți se potrivește în proporție de {bestPercent}%.",
                "ar" => $"يظهر من اختبارك أن {bestName} يناسبك بنسبة {bestPercent}%.",
                _ => $"Uit je test blijkt dat {bestName} bij je past, met {bestPercent}%."
            };
        }

        var other = scored[1];
        var otherName = OccupationTitles.ForChat(other.Title, lang);
        if (other.Percent is not int otherPercent)
        {
            return NoScoreReply(lang);
        }

        if (bestPercent == otherPercent)
        {
            return lang switch
            {
                "en" => $"Your test shows {bestName} ({bestPercent}%) and {otherName} ({otherPercent}%) fit you equally.",
                "pl" => $"Z twojego testu wynika, że {bestName} ({bestPercent}%) i {otherName} ({otherPercent}%) pasują tak samo.",
                "ro" => $"Din testul tău reiese că {bestName} ({bestPercent}%) și {otherName} ({otherPercent}%) ți se potrivesc la fel.",
                "ar" => $"يظهر من اختبارك أن {bestName} ({bestPercent}%) و{otherName} ({otherPercent}%) يناسبانك بنفس الدرجة.",
                _ => $"Uit je test blijkt dat {bestName} ({bestPercent}%) en {otherName} ({otherPercent}%) even goed passen."
            };
        }

        return lang switch
        {
            "en" => $"Your test shows {bestName} ({bestPercent}%) fits you better than {otherName} ({otherPercent}%).",
            "pl" => $"Z twojego testu wynika, że {bestName} ({bestPercent}%) pasuje lepiej niż {otherName} ({otherPercent}%).",
            "ro" => $"Din testul tău reiese că {bestName} ({bestPercent}%) ți se potrivește mai bine decât {otherName} ({otherPercent}%).",
            "ar" => $"يظهر من اختبارك أن {bestName} ({bestPercent}%) يناسبك أكثر من {otherName} ({otherPercent}%).",
            _ => $"Uit je test blijkt dat {bestName} ({bestPercent}%) beter past dan {otherName} ({otherPercent}%)."
        };
    }

    private static string NoScoreReply(string lang) => lang switch
    {
        "en" => "We have no reliable source to compare this job with your profile.",
        "pl" => "Nie mamy pewnego źródła, aby porównać ten zawód z Twoim profilem.",
        "ro" => "Nu avem o sursă sigură ca să comparăm această meserie cu profilul tău.",
        "ar" => "ليس لدينا مصدر موثوق لنقارن هذه المهنة بملفك.",
        _ => OccupationCopy.NoScoreSentence
    };

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
        if (percent is not int fit)
        {
            return NoScoreReply(lang);
        }

        var shown = OccupationTitles.ForChat(job, lang);
        var ownLetters = CareerCompassBuilder.WeightsFor(job);
        var directionCode = ownLetters.Count > 0
            ? ownLetters[0].Code
            : CareerCompassBuilder.PrimaryCode(job);
        var directionScore = scores.Get(directionCode);
        var direction = DimensionLabels.For(directionCode, lang);
        var directionFits = directionScore >= 50;
        var work = hasWorkExperience && workLines is { Count: > 0 }
            ? string.Join(", ", workLines.Take(2))
            : null;

        if (string.IsNullOrWhiteSpace(work))
        {
            return lang switch
            {
                "en" => $"You have no work experience in your profile yet. Your test shows {shown} fits you at {fit}%. Short motivation: I want to start as {shown}. {MotivationClaim("en", direction, directionScore, directionFits)}",
                "pl" => $"W twoim profilu nie ma jeszcze doświadczenia w pracy. Z testu wynika, że {shown} pasuje w {fit}%. Krótka motywacja: Chcę zacząć jako {shown}. {MotivationClaim("pl", direction, directionScore, directionFits)}",
                "ro" => $"Nu ai încă experiență de muncă în profil. Din test reiese că {shown} ți se potrivește în proporție de {fit}%. Motivație scurtă: Vreau să încep ca {shown}. {MotivationClaim("ro", direction, directionScore, directionFits)}",
                "ar" => $"لا توجد خبرة عمل في ملفك بعد. يظهر من اختبارك أن {shown} يناسبك بنسبة {fit}%. دافع قصير: أريد أن أبدأ كـ {shown}. {MotivationClaim("ar", direction, directionScore, directionFits)}",
                _ => $"Je hebt nog geen werkervaring in je profiel. Uit je test blijkt dat {shown} bij je past, met {fit}%. Korte motivatie: Ik wil aan de slag als {shown}. {MotivationClaim("nl", direction, directionScore, directionFits)}"
            };
        }

        return lang switch
        {
            "en" => $"Your profile lists this work: {work}. Your test shows {shown} fits you at {fit}%. Short motivation: I want to work as {shown}. {MotivationClaim("en", direction, directionScore, directionFits)}",
            "pl" => $"W twoim profilu jest ta praca: {work}. Z testu wynika, że {shown} pasuje w {fit}%. Krótka motywacja: Chcę pracować jako {shown}. {MotivationClaim("pl", direction, directionScore, directionFits)}",
            "ro" => $"În profilul tău este această muncă: {work}. Din test reiese că {shown} ți se potrivește în proporție de {fit}%. Motivație scurtă: Vreau să lucrez ca {shown}. {MotivationClaim("ro", direction, directionScore, directionFits)}",
            "ar" => $"في ملفك هذا العمل: {work}. يظهر من اختبارك أن {shown} يناسبك بنسبة {fit}%. دافع قصير: أريد أن أعمل كـ {shown}. {MotivationClaim("ar", direction, directionScore, directionFits)}",
            _ => $"In je profiel staat dit werk: {work}. Uit je test blijkt dat {shown} bij je past, met {fit}%. Korte motivatie: Ik wil werken als {shown}. {MotivationClaim("nl", direction, directionScore, directionFits)}"
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
