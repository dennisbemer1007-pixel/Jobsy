using Jobsy.Core.Localization;

namespace Jobsy.Core.Rules;

/// <summary>
/// Scripted coach answers for hours, availability, values, culture and a short shadowing mail.
/// These come from the profile and the tests, before the model is asked.
/// </summary>
public static class CandidateProfileFacts
{
    public static string? TryReply(
        string? language,
        string question,
        int? minHours,
        int? maxHours,
        DateOnly? availableFrom,
        DateOnly today,
        string? valuesLine,
        string? cultureLine)
    {
        if (string.IsNullOrWhiteSpace(question))
        {
            return null;
        }

        var lang = JobsyLanguages.Normalize(language);
        var fold = question.ToLowerInvariant();
        if (LooksLikeShadowMail(fold))
        {
            return ShadowMail(lang);
        }

        if (LooksLikeHours(fold))
        {
            return Hours(lang, minHours, maxHours) ?? Missing(lang);
        }

        if (LooksLikeAvailability(fold))
        {
            return Availability(lang, availableFrom, today);
        }

        if (LooksLikeValues(fold))
        {
            return string.IsNullOrWhiteSpace(valuesLine) ? Missing(lang) : Values(lang, valuesLine);
        }

        if (LooksLikeCulture(fold))
        {
            return string.IsNullOrWhiteSpace(cultureLine) ? Missing(lang) : Culture(lang, cultureLine);
        }

        return null;
    }

    public static bool Handles(string? question)
    {
        if (string.IsNullOrWhiteSpace(question))
        {
            return false;
        }

        var fold = question.ToLowerInvariant();
        return LooksLikeShadowMail(fold)
               || LooksLikeHours(fold)
               || LooksLikeAvailability(fold)
               || LooksLikeValues(fold)
               || LooksLikeCulture(fold);
    }

    private static bool LooksLikeShadowMail(string fold)
        => fold.Contains("meelopen", StringComparison.Ordinal)
           || fold.Contains("meekijk", StringComparison.Ordinal)
           || fold.Contains("korte mail", StringComparison.Ordinal)
           || fold.Contains("shadow", StringComparison.Ordinal)
           || fold.Contains("staż", StringComparison.Ordinal)
           || fold.Contains("staz", StringComparison.Ordinal)
           || fold.Contains("însoț", StringComparison.Ordinal)
           || fold.Contains("insot", StringComparison.Ordinal)
           || fold.Contains("مرافقة", StringComparison.Ordinal);

    private static bool LooksLikeHours(string fold)
        => fold.Contains("uren", StringComparison.Ordinal)
           || fold.Contains(" uur", StringComparison.Ordinal)
           || fold.StartsWith("uur", StringComparison.Ordinal)
           || fold.Contains("hours", StringComparison.Ordinal)
           || fold.Contains("godzin", StringComparison.Ordinal)
           || fold.Contains("ore pe", StringComparison.Ordinal)
           || fold.Contains("ساعات", StringComparison.Ordinal);

    private static bool LooksLikeAvailability(string fold)
        => fold.Contains("beschik", StringComparison.Ordinal)
           || fold.Contains("per direct", StringComparison.Ordinal)
           || fold.Contains("available", StringComparison.Ordinal)
           || fold.Contains("dostęp", StringComparison.Ordinal)
           || fold.Contains("dostep", StringComparison.Ordinal)
           || fold.Contains("disponibil", StringComparison.Ordinal)
           || fold.Contains("متاح", StringComparison.Ordinal);

    private static bool LooksLikeValues(string fold)
        => fold.Contains("waarde", StringComparison.Ordinal)
           || fold.Contains("vind ik belangrijk", StringComparison.Ordinal)
           || fold.Contains("values", StringComparison.Ordinal)
           || fold.Contains("what matters", StringComparison.Ordinal)
           || fold.Contains("wartoś", StringComparison.Ordinal)
           || fold.Contains("wartosc", StringComparison.Ordinal)
           || fold.Contains("valori", StringComparison.Ordinal)
           || fold.Contains("قيم", StringComparison.Ordinal);

    private static bool LooksLikeCulture(string fold)
        => fold.Contains("cultuur", StringComparison.Ordinal)
           || fold.Contains("werksfeer", StringComparison.Ordinal)
           || fold.Contains("culture", StringComparison.Ordinal)
           || fold.Contains("kultura", StringComparison.Ordinal)
           || fold.Contains("cultură", StringComparison.Ordinal)
           || fold.Contains("cultura", StringComparison.Ordinal)
           || fold.Contains("ثقافة", StringComparison.Ordinal);

    private static string? Hours(string lang, int? minHours, int? maxHours)
    {
        if (minHours is null && maxHours is null)
        {
            return null;
        }

        if (minHours is int min && maxHours is int max && min != max)
        {
            return lang switch
            {
                "en" => $"You want to work {min} to {max} hours a week.",
                "pl" => $"Chcesz pracować od {min} do {max} godzin tygodniowo.",
                "ro" => $"Vrei să lucrezi {min} până la {max} ore pe săptămână.",
                "ar" => $"تريد العمل من {min} إلى {max} ساعة في الأسبوع.",
                _ => $"Je wilt {min} tot {max} uur per week werken."
            };
        }

        var hours = minHours ?? maxHours;
        return lang switch
        {
            "en" => $"You want to work {hours} hours a week.",
            "pl" => $"Chcesz pracować {hours} godzin tygodniowo.",
            "ro" => $"Vrei să lucrezi {hours} ore pe săptămână.",
            "ar" => $"تريد العمل {hours} ساعة في الأسبوع.",
            _ => $"Je wilt {hours} uur per week werken."
        };
    }

    private static string Availability(string lang, DateOnly? availableFrom, DateOnly today)
    {
        if (availableFrom is DateOnly from && from > today)
        {
            var date = from.ToString("d MMM yyyy", System.Globalization.CultureInfo.GetCultureInfo(lang switch
            {
                "en" => "en-GB",
                "pl" => "pl-PL",
                "ro" => "ro-RO",
                "ar" => "ar",
                _ => "nl-NL"
            }));
            return lang switch
            {
                "en" => $"You are available from {date}.",
                "pl" => $"Jesteś dostępny od {date}.",
                "ro" => $"Ești disponibil din {date}.",
                "ar" => $"أنت متاح من {date}.",
                _ => $"Je bent beschikbaar vanaf {date}."
            };
        }

        return lang switch
        {
            "en" => "You are available right away.",
            "pl" => "Jesteś dostępny od zaraz.",
            "ro" => "Ești disponibil imediat.",
            "ar" => "أنت متاح فوراً.",
            _ => "Je bent per direct beschikbaar."
        };
    }

    private static string Values(string lang, string line) => lang switch
    {
        "en" => $"What matters most to you is {line}.",
        "pl" => $"Najważniejsze jest dla ciebie: {line}.",
        "ro" => $"Pentru tine contează cel mai mult: {line}.",
        "ar" => $"الأهم لك هو: {line}.",
        _ => $"Het belangrijkst voor jou is {line}."
    };

    private static string Culture(string lang, string line) => lang switch
    {
        "en" => $"The workplace that fits you is {line}.",
        "pl" => $"Pasuje do ciebie miejsce pracy: {line}.",
        "ro" => $"Locul de muncă care ți se potrivește este: {line}.",
        "ar" => $"مكان العمل الذي يناسبك هو: {line}.",
        _ => $"De werksfeer die bij je past is {line}."
    };

    private static string ShadowMail(string lang) => lang switch
    {
        "en" => "You can send a short mail like this. Subject: Spending a morning with you. Hello, my name is [name]. I would like to spend one morning with you to see what the work is like. May I come and watch? Kind regards, [name].",
        "pl" => "Możesz wysłać krótki mail. Temat: Jeden poranek z wami. Dzień dobry, nazywam się [imię]. Chcę spędzić jeden poranek, żeby zobaczyć, jak wygląda ta praca. Czy mogę przyjść i popatrzeć? Pozdrawiam, [imię].",
        "ro" => "Poți trimite un mail scurt. Subiect: O dimineață cu voi. Bună, mă numesc [nume]. Vreau să stau o dimineață ca să văd cum este munca. Pot să vin să mă uit? Cu respect, [nume].",
        "ar" => "يمكنك إرسال رسالة قصيرة. الموضوع: صباح معكم. مرحباً، اسمي [الاسم]. أريد أن أقضي صباحاً لأرى كيف يسير العمل. هل يمكنني أن أحضر وأتابع؟ مع التحية، [الاسم].",
        _ => "Zo kun je een korte mail schrijven. Onderwerp: Meelopen. Hallo, ik ben [naam]. Ik wil graag één ochtend meelopen om te zien hoe het werk gaat. Mag ik komen kijken? Groet, [naam]."
    };

    private static string Missing(string lang) => lang switch
    {
        "en" => "That is not in your profile yet.",
        "pl" => "Tego jeszcze nie ma w twoim profilu.",
        "ro" => "Asta nu este încă în profilul tău.",
        "ar" => "هذا ليس في ملفك بعد.",
        _ => "Dat staat nog niet in je profiel."
    };
}
