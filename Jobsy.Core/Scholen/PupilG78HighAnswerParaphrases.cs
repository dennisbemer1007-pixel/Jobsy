namespace Jobsy.Core.Scholen;

/// <summary>B1 paraphrases of G78 beroepentest items for high answers (je-form).</summary>
internal static class PupilG78HighAnswerParaphrases
{
  private static readonly IReadOnlyDictionary<int, string> ByItemId = new Dictionary<int, string>
    {
        [9016] = "je graag iets maakt of repareert",
        [9017] = "je graag wilt weten hoe dingen werken",
        [9018] = "je graag iets moois maakt, zoals een tekening of muziek",
        [9019] = "je graag iemand helpt die iets niet snapt",
        [9020] = "je graag de leiding neemt bij een groepsopdracht",
        [9021] = "je graag je spullen en planning netjes houdt",
        [9022] = "je graag met je handen aan iets tastbaars werkt",
        [9023] = "je graag een eigen verhaal, dans of outfit bedenkt",
        [9024] = "je het fijn vindt om anderen te steunen",
        [9025] = "je graag uitzoekt hoe een proefje of puzzel werkt",
        [9026] = "je graag anderen overtuigt van je idee",
        [9027] = "je graag vaste stappen en duidelijke lijstjes gebruikt",
        [9028] = "je graag buiten iets bouwt of fixt",
        [9029] = "je graag speelt of tekent om iets te laten zien",
        [9030] = "je graag met mensen oefent of speelt",
    };

    public static bool TryGet(int itemId, out string paraphrase)
        => ByItemId.TryGetValue(itemId, out paraphrase!);
}
