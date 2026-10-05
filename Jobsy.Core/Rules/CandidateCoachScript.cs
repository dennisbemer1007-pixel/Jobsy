using System.Text.RegularExpressions;
using Jobsy.Core.Localization;
using Jobsy.Core.Reports;
using Jobsy.Core.Reports.Career;

namespace Jobsy.Core.Rules;

/// <summary>
/// Scripted coach answers that must match the report. The model is not asked these questions.
/// </summary>
public static class CandidateCoachScript
{
    public static bool Handles(string? question)
    {
        if (string.IsNullOrWhiteSpace(question))
        {
            return false;
        }

        return LooksLikeLikes(question)
               || LooksLikeCertificate(question)
               || LooksLikeStrengths(question)
               || LooksLikePitfalls(question)
               || LooksLikeActionStep(question)
               || LooksLikeJobFit(question)
               || LooksLikeDirectionFilter(question);
    }

    public static string? TryReply(
        string? language,
        string question,
        RiasecScores scores,
        string? education,
        IReadOnlyList<(string Code, int Score)>? competence,
        IReadOnlyList<string>? certificates)
    {
        var lang = JobsyLanguages.Normalize(language);
        if (LooksLikeLikes(question))
        {
            return Likes(lang, scores);
        }

        if (LooksLikeCertificate(question))
        {
            return Certificates(lang, question, certificates);
        }

        if (LooksLikeStrengths(question))
        {
            return Strengths(lang, competence);
        }

        if (LooksLikePitfalls(question))
        {
            return Pitfalls(lang, scores);
        }

        if (LooksLikeActionStep(question))
        {
            return ActionStep(lang, scores);
        }

        if (LooksLikeJobFit(question))
        {
            var titles = CandidateJobAdvice.TitlesIn(question);
            var title = titles.Count == 0 ? null : titles[0];
            if (title is not null)
            {
                if (CareerCompassBuilder.IsLeadershipTitle(title)
                    && !CareerCompassBuilder.EnterprisingInTop3(scores))
                {
                    return LeadershipLeftOut(lang, title, scores);
                }

                return JobReason(lang, title, scores, education);
            }
        }

        if (LooksLikeDirectionFilter(question))
        {
            var code = DirectionCode(question);
            if (code is not null)
            {
                return DirectionJobs(lang, code, scores, education);
            }
        }

        return null;
    }

    private static bool LooksLikeLikes(string text)
    {
        var fold = text.ToLowerInvariant();
        if (fold.Contains("motivatie", StringComparison.Ordinal)
            || fold.Contains("motivation", StringComparison.Ordinal)
            || fold.Contains("certificaat", StringComparison.Ordinal)
            || fold.Contains("certificate", StringComparison.Ordinal))
        {
            return false;
        }

        return fold.Contains("leuk", StringComparison.Ordinal)
               || fold.Contains("vind ik", StringComparison.Ordinal)
               || fold.Contains("what do i like", StringComparison.Ordinal)
               || fold.Contains("what i like", StringComparison.Ordinal)
               || fold.Contains("lubię", StringComparison.Ordinal)
               || fold.Contains("lubie", StringComparison.Ordinal)
               || fold.Contains("îmi place", StringComparison.Ordinal)
               || fold.Contains("imi place", StringComparison.Ordinal)
               || fold.Contains("أحب", StringComparison.Ordinal);
    }

    private static bool LooksLikeCertificate(string text)
    {
        var fold = text.ToLowerInvariant();
        return fold.Contains("certificaat", StringComparison.Ordinal)
               || fold.Contains("certificate", StringComparison.Ordinal)
               || fold.Contains("certyfikat", StringComparison.Ordinal)
               || fold.Contains("certificat", StringComparison.Ordinal)
               || fold.Contains("شهادة", StringComparison.Ordinal)
               || fold.Contains("vca", StringComparison.Ordinal);
    }

    private static bool LooksLikeStrengths(string text)
    {
        var fold = text.ToLowerInvariant();
        return fold.Contains("sterkste", StringComparison.Ordinal)
               || fold.Contains("sterke punt", StringComparison.Ordinal)
               || fold.Contains("strength", StringComparison.Ordinal)
               || fold.Contains("mocne", StringComparison.Ordinal)
               || fold.Contains("puncte forte", StringComparison.Ordinal)
               || fold.Contains("نقاط القوة", StringComparison.Ordinal);
    }

    private static bool LooksLikePitfalls(string text)
    {
        var fold = text.ToLowerInvariant();
        return fold.Contains("valkuil", StringComparison.Ordinal)
               || fold.Contains("pitfall", StringComparison.Ordinal)
               || fold.Contains("pułap", StringComparison.Ordinal)
               || fold.Contains("pulap", StringComparison.Ordinal)
               || fold.Contains("capcan", StringComparison.Ordinal)
               || fold.Contains("نقاط الضعف", StringComparison.Ordinal);
    }

    private static bool LooksLikeActionStep(string text)
    {
        var fold = text.ToLowerInvariant();
        return fold.Contains("actieplan", StringComparison.Ordinal)
               || fold.Contains("eerste stap", StringComparison.Ordinal)
               || fold.Contains("action plan", StringComparison.Ordinal)
               || fold.Contains("first step", StringComparison.Ordinal)
               || fold.Contains("plan działania", StringComparison.Ordinal)
               || fold.Contains("pierwszy krok", StringComparison.Ordinal)
               || fold.Contains("plan de acțiune", StringComparison.Ordinal)
               || fold.Contains("plan de actiune", StringComparison.Ordinal)
               || fold.Contains("primul pas", StringComparison.Ordinal)
               || fold.Contains("خطة العمل", StringComparison.Ordinal)
               || fold.Contains("أول خطوة", StringComparison.Ordinal);
    }

    private static bool LooksLikeJobFit(string text)
    {
        if (CandidateJobAdvice.LooksLikeComparison(text) || CandidateJobAdvice.TitlesIn(text).Count == 0)
        {
            return false;
        }

        var fold = text.ToLowerInvariant();
        return fold.Contains("waarom", StringComparison.Ordinal)
               || fold.Contains("why", StringComparison.Ordinal)
               || fold.Contains("dlaczego", StringComparison.Ordinal)
               || fold.Contains("de ce", StringComparison.Ordinal)
               || fold.Contains("لماذا", StringComparison.Ordinal)
               || fold.Contains("past", StringComparison.Ordinal)
               || fold.Contains("passen", StringComparison.Ordinal)
               || fold.Contains("pasuje", StringComparison.Ordinal)
               || fold.Contains("potriv", StringComparison.Ordinal)
               || fold.Contains("fit", StringComparison.Ordinal)
               || fold.Contains("تناسب", StringComparison.Ordinal);
    }

    private static bool LooksLikeDirectionFilter(string text)
    {
        if (CandidateJobAdvice.TitlesIn(text).Count > 0 || DirectionCode(text) is null)
        {
            return false;
        }

        var fold = text.ToLowerInvariant();
        return fold.Contains("beroep", StringComparison.Ordinal)
               || fold.Contains("baan", StringComparison.Ordinal)
               || fold.Contains("job", StringComparison.Ordinal)
               || fold.Contains("zawod", StringComparison.Ordinal)
               || fold.Contains("zawód", StringComparison.Ordinal)
               || fold.Contains("meseri", StringComparison.Ordinal)
               || fold.Contains("مهن", StringComparison.Ordinal)
               || fold.Contains("وظائف", StringComparison.Ordinal);
    }

    private static string Likes(string lang, RiasecScores scores)
    {
        var (label, score) = Top(scores, lang);
        return lang switch
        {
            "en" => $"I do not know that. You score highest on {label} ({score}%).",
            "pl" => $"Tego nie wiem. Najwyższy wynik masz w {label} ({score}%).",
            "ro" => $"Nu știu asta. Scorul tău cel mai mare este la {label} ({score}%).",
            "ar" => $"لا أعرف ذلك. أعلى درجة لك هي في {label} ({score}%).",
            _ => $"Dat weet ik niet. Je scoort het hoogst op {label} ({score}%)."
        };
    }

    private static string Certificates(string lang, string question, IReadOnlyList<string>? certificates)
    {
        var known = (certificates ?? [])
            .Where(item => !string.IsNullOrWhiteSpace(item))
            .Select(item => item.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
        if (known.Count == 0)
        {
            return lang switch
            {
                "en" => "No. There are no certificates in your profile.",
                "pl" => "Nie. W twoim profilu nie ma certyfikatów.",
                "ro" => "Nu. În profilul tău nu sunt certificate.",
                "ar" => "لا. لا توجد شهادات في ملفك.",
                _ => "Nee, er staan geen certificaten in je profiel."
            };
        }

        var hit = known.FirstOrDefault(item =>
            question.Contains(item, StringComparison.OrdinalIgnoreCase)
            || question.Contains(item.Split(' ')[0], StringComparison.OrdinalIgnoreCase));
        if (hit is not null)
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

        var list = string.Join(", ", known);
        return lang switch
        {
            "en" => $"No, that certificate is not in your profile. Your profile lists {list}.",
            "pl" => $"Nie, tego certyfikatu nie ma w profilu. W profilu jest {list}.",
            "ro" => $"Nu, certificatul acela nu este în profil. În profil este {list}.",
            "ar" => $"لا، تلك الشهادة ليست في ملفك. في ملفك {list}.",
            _ => $"Nee, dat certificaat staat niet in je profiel. In je profiel staat {list}."
        };
    }

    private static string Strengths(string lang, IReadOnlyList<(string Code, int Score)>? competence)
    {
        var ranked = (competence ?? [])
            .Where(item => !string.IsNullOrWhiteSpace(item.Code))
            .OrderByDescending(item => item.Score)
            .ThenBy(item => item.Code, StringComparer.Ordinal)
            .ToList();
        var strong = ranked.Where(item => item.Score >= 50).Take(3).ToList();
        if (strong.Count == 0)
        {
            if (ranked.Count == 0)
            {
                return lang switch
                {
                    "en" => "I do not know that. There is no competence score in your profile yet.",
                    "pl" => "Tego nie wiem. W twoim profilu nie ma jeszcze wyniku kompetencji.",
                    "ro" => "Nu știu asta. În profilul tău nu este încă un scor de competență.",
                    "ar" => "لا أعرف ذلك. لا توجد درجة كفاءة في ملفك بعد.",
                    _ => "Dat weet ik niet. Er staat nog geen competentiescore in je profiel."
                };
            }

            var top = ranked[0];
            var weak = $"{DimensionLabels.For(top.Code, lang)} ({top.Score}%)";
            return lang switch
            {
                "en" => $"Your highest score is {weak}. That is not a strength yet.",
                "pl" => $"Twój najwyższy wynik to {weak}. To jeszcze nie jest mocna strona.",
                "ro" => $"Scorul tău cel mai mare este {weak}. Asta nu este încă un punct forte.",
                "ar" => $"أعلى درجة لك هي {weak}. هذه ليست نقطة قوة بعد.",
                _ => $"Je hoogste score is {weak}. Dat is nog geen sterke kant."
            };
        }

        var parts = string.Join(", ", strong.Select(item => $"{DimensionLabels.For(item.Code, lang)} ({item.Score}%)"));
        return lang switch
        {
            "en" => $"Your strongest points are {parts}.",
            "pl" => $"Twoje najmocniejsze strony to {parts}.",
            "ro" => $"Punctele tale cele mai puternice sunt {parts}.",
            "ar" => $"أقوى نقاطك هي {parts}.",
            _ => $"Je sterkste punten zijn {parts}."
        };
    }

    private static string Pitfalls(string lang, RiasecScores scores)
    {
        var top = CareerTestCatalog.RiasecCodes
            .Select(code => (Code: code, Score: scores.Get(code)))
            .OrderByDescending(item => item.Score)
            .ThenBy(item => item.Code, StringComparer.Ordinal)
            .Take(3)
            .ToList();
        var lines = top.Select(item => $"{DimensionLabels.For(item.Code, lang)}: {PitfallText(item.Code, lang)}");
        var body = string.Join(" ", lines);
        return lang switch
        {
            "en" => $"Your pitfalls are the downside of your top directions. {body}",
            "pl" => $"Twoje pułapki to słabsza strona twoich najwyższych kierunków. {body}",
            "ro" => $"Capcanele tale sunt partea slabă a direcțiilor tale de sus. {body}",
            "ar" => $"نقاط ضعفك هي الجانب الأضعف من أعلى اتجاهاتك. {body}",
            _ => $"Je valkuilen zijn de keerzijde van je hoogste richtingen. {body}"
        };
    }

    private static string PitfallText(string code, string lang)
    {
        if (lang is "nl" or "en")
        {
            return DeepReportCatalog.Get($"pitfall.{code}", lang);
        }

        return (code, lang) switch
        {
            (CareerTestCatalog.Realistic, "pl") => "Możesz działać za szybko i za mało rozmawiać.",
            (CareerTestCatalog.Realistic, "ro") => "Poți acționa prea repede și vorbi prea puțin.",
            (CareerTestCatalog.Realistic, "ar") => "قد تتصرف بسرعة كبيرة وتتشاور قليلاً.",
            (CareerTestCatalog.Investigative, "pl") => "Możesz za długo szukać i za późno zacząć.",
            (CareerTestCatalog.Investigative, "ro") => "Poți căuta prea mult și începe prea târziu.",
            (CareerTestCatalog.Investigative, "ar") => "قد تبحث طويلاً وتبدأ متأخراً.",
            (CareerTestCatalog.Artistic, "pl") => "Możesz chcieć zmieniać coś, co już działa.",
            (CareerTestCatalog.Artistic, "ro") => "Poți vrea să schimbi ceva ce deja merge.",
            (CareerTestCatalog.Artistic, "ar") => "قد تريد تغيير شيء يعمل بالفعل.",
            (CareerTestCatalog.Social, "pl") => "Możesz za często mówić tak, żeby pomóc innym.",
            (CareerTestCatalog.Social, "ro") => "Poți spune da prea des ca să ajuți pe alții.",
            (CareerTestCatalog.Social, "ar") => "قد تقول نعم كثيراً لمساعدة الآخرين.",
            (CareerTestCatalog.Enterprising, "pl") => "Możesz pchać za mocno i zostawiać innych w tyle.",
            (CareerTestCatalog.Enterprising, "ro") => "Poți împinge prea tare și îi lași pe alții în urmă.",
            (CareerTestCatalog.Enterprising, "ar") => "قد تدفع بقوة وتتجاوز الآخرين.",
            (CareerTestCatalog.Conventional, "pl") => "Możesz trzymać się starego sposobu, gdy nowy jest lepszy.",
            (CareerTestCatalog.Conventional, "ro") => "Poți rămâne la modul vechi când unul nou este mai bun.",
            (CareerTestCatalog.Conventional, "ar") => "قد تتمسك بالطريقة القديمة عندما تكون الجديدة أفضل.",
            _ => DeepReportCatalog.Get($"pitfall.{code}", "nl")
        };
    }

    private static string ActionStep(string lang, RiasecScores scores)
    {
        var domains = CareerTestCatalog.RiasecCodes
            .Select(code => new DeepAnalysisDomainScore(code, scores.Get(code), 1))
            .ToList();
        var report = CareerDeepReportBuilder.Build(domains, CareerCompassBuilder.Build(scores), null, DateTime.UtcNow);
        var step = report.ActionPlan.FirstOrDefault();
        var dutch = step?.Body.Nl ?? "";
        var english = step?.Body.En ?? "";
        if (lang is "en")
        {
            return english;
        }

        if (lang is "nl")
        {
            return dutch;
        }

        var job = Regex.Match(dutch, @"^Kies (.+) uit de beroepen").Groups[1].Value;
        var shown = string.IsNullOrWhiteSpace(job) ? dutch : OccupationTitles.ForChat(job.Trim(), lang);
        return lang switch
        {
            "pl" => $"Wybierz {shown} z zawodów, które do ciebie pasują. Porozmawiaj z kimś, kto wykonuje tę pracę.",
            "ro" => $"Alege {shown} din meseriile care ți se potrivesc. Vorbește cu cineva care face această muncă.",
            "ar" => $"اختر {shown} من المهن التي تناسبك. تحدث مع شخص يقوم بهذا العمل.",
            _ => dutch
        };
    }

    private static string LeadershipLeftOut(string lang, string title, RiasecScores scores)
    {
        var shown = OccupationTitles.ForChat(title, lang);
        var label = DimensionLabels.For(CareerTestCatalog.Enterprising, lang);
        var score = scores.Enterprising;
        return lang switch
        {
            "en" => $"{shown} is left out. {label} ({score}%) is not one of your top 3 directions.",
            "pl" => $"{shown} zostaje pominięty. {label} ({score}%) nie jest w twoich 3 najwyższych kierunkach.",
            "ro" => $"{shown} este lăsat deoparte. {label} ({score}%) nu este în primele 3 direcții ale tale.",
            "ar" => $"{shown} غير مدرج. {label} ({score}%) ليس ضمن أعلى 3 اتجاهات لك.",
            _ => $"{shown} laten we weg. {label} ({score}%) zit niet bij je drie hoogste richtingen."
        };
    }

    private static string JobReason(string lang, string title, RiasecScores scores, string? education)
    {
        var shown = OccupationTitles.ForChat(title, lang);
        var letters = CareerCompassBuilder.WeightsFor(title)
            .Select(weight => (weight.Code, Score: scores.Get(weight.Code)))
            .ToList();
        if (letters.Count == 0)
        {
            var fallback = CareerCompassBuilder.PrimaryCode(title);
            letters = [(fallback, scores.Get(fallback))];
        }
        var parts = string.Join(
            lang is "ar" ? " و" : lang is "nl" ? " en " : ", ",
            letters.Select(item => $"{DimensionLabels.For(item.Code, lang)} ({item.Score}%)"));
        var percent = CareerCompassBuilder.CatalogueFit(title, scores, education);
        var strong = letters.Count > 0 && letters.All(item => item.Score >= 50);
        if (strong)
        {
            return lang switch
            {
                "en" => $"Your test shows {shown} fits you at {percent}%, because of {parts}.",
                "pl" => $"Z twojego testu wynika, że {shown} pasuje w {percent}%, przez {parts}.",
                "ro" => $"Din testul tău reiese că {shown} ți se potrivește în proporție de {percent}%, prin {parts}.",
                "ar" => $"يظهر من اختبارك أن {shown} يناسبك بنسبة {percent}%، بسبب {parts}.",
                _ => $"Uit je test blijkt dat {shown} past bij {parts}."
            };
        }

        return lang switch
        {
            "en" => $"Your test shows {shown} uses {parts}. A score under 50 is not a strength.",
            "pl" => $"Z twojego testu wynika, że {shown} używa {parts}. Wynik poniżej 50 nie jest mocną stroną.",
            "ro" => $"Din testul tău reiese că {shown} folosește {parts}. Un scor sub 50 nu este un punct forte.",
            "ar" => $"يظهر من اختبارك أن {shown} يستخدم {parts}. الدرجة تحت 50 ليست نقطة قوة.",
            _ => $"Uit je test blijkt dat {shown} scoort op {parts}. Een score onder 50 is geen sterke kant."
        };
    }

    private static string DirectionJobs(string lang, string code, RiasecScores scores, string? education)
    {
        var label = DimensionLabels.For(code, lang);
        var jobs = CareerCompassBuilder.Listed(scores, education)
            .Where(job => string.Equals(CareerCompassBuilder.PrimaryCode(job.Title), code, StringComparison.OrdinalIgnoreCase))
            .Take(8)
            .Select(job => OccupationTitles.ForChat(job.Title, lang))
            .ToList();
        var list = jobs.Count == 0 ? "" : string.Join(", ", jobs);
        if (jobs.Count == 0)
        {
            return lang switch
            {
                "en" => $"None of the jobs on your list have {label} as the main direction.",
                "pl" => $"Żaden zawód na twojej liście nie ma {label} jako głównego kierunku.",
                "ro" => $"Nicio meserie de pe lista ta nu are {label} ca direcție principală.",
                "ar" => $"لا توجد مهنة في قائمتك اتجاهها الرئيسي {label}.",
                _ => $"Geen beroep op je lijst heeft {label} als belangrijkste richting."
            };
        }

        return lang switch
        {
            "en" => $"Jobs on your list whose main direction is {label}: {list}.",
            "pl" => $"Zawody na twojej liście, których główny kierunek to {label}: {list}.",
            "ro" => $"Meserii de pe lista ta a căror direcție principală este {label}: {list}.",
            "ar" => $"المهن في قائمتك التي اتجاهها الرئيسي {label}: {list}.",
            _ => $"Beroepen op je lijst met als belangrijkste richting {label}: {list}."
        };
    }

    private static string? DirectionCode(string question)
    {
        var fold = question.ToLowerInvariant();
        foreach (var code in CareerTestCatalog.RiasecCodes)
        {
            foreach (var lang in new[] { "nl", "en", "pl", "ro", "ar" })
            {
                var label = DimensionLabels.For(code, lang);
                if (!string.IsNullOrWhiteSpace(label) && fold.Contains(label, StringComparison.OrdinalIgnoreCase))
                {
                    return code;
                }
            }

            var dutch = CareerCompassBuilder.TypeLabel(code);
            if (fold.Contains(dutch, StringComparison.OrdinalIgnoreCase))
            {
                return code;
            }
        }

        return null;
    }

    private static (string Label, int Score) Top(RiasecScores scores, string lang)
    {
        var top = CareerTestCatalog.RiasecCodes
            .Select(code => (Code: code, Score: scores.Get(code)))
            .OrderByDescending(item => item.Score)
            .ThenBy(item => item.Code, StringComparer.Ordinal)
            .First();
        return (DimensionLabels.For(top.Code, lang), top.Score);
    }
}
