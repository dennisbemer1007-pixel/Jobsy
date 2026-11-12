namespace Jobsy.Core.Scholen;

/// <summary>Short kid-language labels for passport “Belangrijk” (values 9031–9045, one per item).</summary>
public static class PupilG78ValueShortLabels
{
    public static string? ForItem(int itemId)
        => itemId switch
        {
            9031 => "Zelf kiezen hoe je iets doet",
            9032 => "Goede vrienden om je heen",
            9033 => "Heel goed worden in wat je doet",
            9034 => "Weten wat er gaat gebeuren",
            9035 => "Iets doen waar de wereld beter van wordt",
            9036 => "Zelf je dag indelen",
            9037 => "Bij mensen horen die elkaar helpen",
            9038 => "Trots zijn op wat je bereikt",
            9039 => "Rust en duidelijkheid",
            9040 => "Iets betekenen voor anderen",
            9041 => "Zelf mogen kiezen",
            9042 => "Dicht bij mensen die je kent",
            9043 => "Uitdagingen en groeien",
            9044 => "Weten waar je aan toe bent",
            9045 => "Meehelpen aan iets goeds",
            9048 => "Dat iedereen zich welkom voelt",
            9054 => "Dat iedereen zich goed voelt",
            9060 => "Sfeer belangrijker dan winnen",
            _ => null
        };

    public static string? FromClause(string? clauseText)
    {
        if (string.IsNullOrWhiteSpace(clauseText))
        {
            return null;
        }

        foreach (var (id, je) in PupilG78QuestionJeForms.All)
        {
            if (id is < 9031 or > 9045)
            {
                continue;
            }

            if (clauseText.Contains(je.TrimEnd('.'), StringComparison.Ordinal))
            {
                return ForItem(id);
            }
        }

        return null;
    }
}
