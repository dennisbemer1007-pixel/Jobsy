namespace Jobsy.Core.Rules;

/// <summary>
/// Motiverende micro-copy op vaste mijlpalen in de diepte-analyse (competence 150 / career 200).
/// </summary>
public static class DeepAnalysisBoosters
{
    /// <summary>Show a booster after every N newly completed answers.</summary>
    public const int Interval = 25;

    public static readonly int[] Milestones = [25, 50, 75, 100, 125, 150, 175];

    public static bool IsMilestone(int answeredCount)
        => answeredCount > 0 && answeredCount % Interval == 0;

    public static string? TryMessage(int answeredCount, int questionCount = DeepAnalysisCatalog.QuestionCount)
    {
        if (!IsMilestone(answeredCount) || answeredCount > questionCount)
        {
            return null;
        }

        var left = Math.Max(0, questionCount - answeredCount);
        return answeredCount switch
        {
            25 => "Nice — eerste streepje gezet! Je zit al op een zesde. Even schouders los, en door.",
            50 => "Halverwege de eerste helft? Nee: je bent al een derde verder. Dat telt. Goed bezig!",
            75 => "Halverwege! Je kent jezelf al beter. Nog een stukje — je kunt dit.",
            100 => "Honderd vragen! De finish is in zicht. Pak desnoods een slok water, en maak het af.",
            125 => left > 0
                ? $"Sterke sprint — nog {left} vraagjes. Je bent er bijna!"
                : "Bijna klaar — de laatste loodjes wegen het zwaarst, maar jij tilt ze.",
            150 => left > 0
                ? $"Mooi, 150 gedaan. Nog {left} tot de finish — houd dit ritme."
                : "Klaar! Wat een doorzettingsvermogen.",
            175 => left > 0
                ? $"Laatste loodjes — nog {left}. Je maakt het af."
                : "Bijna klaar — je bent er!",
            _ => $"Mooi, {answeredCount} van de {questionCount}. Blijf in je ritme — je doet het goed."
        };
    }

    public static string TitleFor(int answeredCount) => answeredCount switch
    {
        25 => "Eerste mijlpaal!",
        50 => "Je zit in de flow",
        75 => "Halverwege!",
        100 => "Honderd!",
        125 => "Sterke sprint",
        150 => "Bijna thuis",
        175 => "Laatste sprint",
        _ => "Goed bezig!"
    };
}
