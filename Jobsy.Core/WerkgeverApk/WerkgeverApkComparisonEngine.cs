using System.Text.Json;
using Jobsy.Core.Enums;
using Jobsy.Core.Scholen.MboHbo;

namespace Jobsy.Core.WerkgeverApk;

public sealed record WerkgeverApkComparisonTopic(string Label, string Sentence);

public sealed record WerkgeverApkComparisonDto(
    IReadOnlyList<WerkgeverApkComparisonTopic> Agree,
    IReadOnlyList<WerkgeverApkComparisonTopic> Differ,
    IReadOnlyList<string> ConversationQuestions);

public static class WerkgeverApkComparisonEngine
{
    private const int AgreeThreshold = 4;
    private const int DifferGap = 2;

    private static readonly string[] DefaultConversationQuestions =
    [
        "Waar wil je het eerst over praten: waar jullie het eens zijn, of waar jullie anders kijken?",
        "Wat heb jij nodig om prettig te blijven werken in je team?",
        "Welke taak of nieuwe manier van werken zou jij wel willen uitproberen?"
    ];

    public static WerkgeverApkComparisonDto BuildComparison(
        PupilMboHboProgressState employee,
        IReadOnlyDictionary<string, int> managerAnswers,
        SchoolLevel level = SchoolLevel.Mbo3,
        string managerDisplayName = "je leidinggevende")
    {
        ArgumentNullException.ThrowIfNull(employee);
        ArgumentNullException.ThrowIfNull(managerAnswers);

        var agree = new List<WerkgeverApkComparisonTopic>();
        var differ = new List<WerkgeverApkComparisonTopic>();

        foreach (var q in WerkgeverApkManagerQuestionCatalog.All)
        {
            if (!managerAnswers.TryGetValue(q.Id, out var manager))
            {
                continue;
            }

            var self = WerkgeverApkEmployeeDimensionScores.SelfScore(q, employee, level);
            if (self is null)
            {
                continue;
            }

            if (self >= AgreeThreshold && manager >= AgreeThreshold)
            {
                agree.Add(new WerkgeverApkComparisonTopic(q.Label, AgreeSentence(q.Label, managerDisplayName)));
                continue;
            }

            if (Math.Abs(self.Value - manager) >= DifferGap)
            {
                differ.Add(new WerkgeverApkComparisonTopic(q.Label, DifferSentence(q.Label, self.Value, manager, managerDisplayName)));
            }
        }

        var conversation = new List<string>(DefaultConversationQuestions);
        if (differ.Count > 0)
        {
            conversation.Add($"Wat wil je {managerDisplayName} vertellen over {differ[0].Label.ToLowerInvariant()}?");
        }

        return new WerkgeverApkComparisonDto(agree, differ, conversation);
    }

    public static IReadOnlyDictionary<string, int> ParseManagerAnswers(string? answersJson)
    {
        if (string.IsNullOrWhiteSpace(answersJson))
        {
            return new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        }

        try
        {
            return JsonSerializer.Deserialize<Dictionary<string, int>>(answersJson)
                   ?? new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        }
        catch (JsonException)
        {
            return new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        }
    }

    private static string AgreeSentence(string label, string managerDisplayName)
        => label switch
        {
            "Samen & aardig" =>
                $"Jij en {managerDisplayName} herkennen allebei dat je prettig samenwerkt en rekening houdt met collega's.",
            "Afmaken & netjes werken" =>
                $"Jullie zien allebei dat je afmaakt wat is afgesproken en netjes werk aflevert.",
            "Verbinding & zorg" =>
                $"Jullie vinden allebei dat een warme band met collega's jou energie geeft.",
            _ => $"Jij en {managerDisplayName} herkennen dit beeld allebei bij {label.ToLowerInvariant()}."
        };

    private static string DifferSentence(string label, int self, int manager, string managerDisplayName)
    {
        var selfStrong = self >= AgreeThreshold;
        var managerStrong = manager >= AgreeThreshold;

        return label switch
        {
            "Nieuwe dingen proberen" when selfStrong && !managerStrong =>
                $"Jij herkent jezelf hierin duidelijk. {managerDisplayName} ziet jou vaker in vaste routines. Dat is iets om uit te wisselen.",
            "Zelf kiezen en uitdaging" when selfStrong && !managerStrong =>
                $"Jij wilt zelf keuzes maken. {managerDisplayName} denkt eerder dat zekerheid voor jou zwaarder weegt.",
            "Energie van mensen" when selfStrong && !managerStrong =>
                $"Jij haalt energie uit contact. {managerDisplayName} ziet jou vaker rustig en zelfstandig werken.",
            _ => selfStrong
                ? $"Jij herkent dit duidelijk bij jezelf. {managerDisplayName} ziet dit minder sterk. Praat daar eens over."
                : managerStrong
                    ? $"{managerDisplayName} herkent dit duidelijk bij jou. Jij ziet dit zelf minder sterk. Dat mag — het is een gespreksstarter."
                    : $"Jij en {managerDisplayName} kijken anders naar {label.ToLowerInvariant()}. Dat is geen fout, maar iets om te bespreken."
        };
    }
}
