namespace Jobsy.Core.Rules;

/// <summary>
/// Motiverende micro-copy op vaste mijlpalen in de 150-vragen diepte-analyse.
/// </summary>
public static class DeepAnalysisBoosters
{
    /// <summary>Show a booster after every N newly completed answers (25 keeps 150 neatly divided).</summary>
    public const int Interval = 25;

    public static readonly int[] Milestones = [25, 50, 75, 100, 125];

    public static bool IsMilestone(int answeredCount)
        => Milestones.Contains(answeredCount);

    public static string? TryMessage(int answeredCount, int questionCount = DeepAnalysisCatalog.QuestionCount)
    {
        if (!IsMilestone(answeredCount))
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
                ? $"Bijna klaar — nog {left} vraagjes. Je bent er bijna!"
                : "Bijna klaar — de laatste loodjes wegen het zwaarst, maar jij tilt ze.",
            _ => $"Mooi, {answeredCount} van de {questionCount}. Blijf in je ritme — je doet het goed."
        };
    }

    public static string TitleFor(int answeredCount) => answeredCount switch
    {
        25 => "Eerste mijlpaal!",
        50 => "Je zit in de flow",
        75 => "Halverwege!",
        100 => "Honderd!",
        125 => "Laatste sprint",
        _ => "Goed bezig!"
    };
}
