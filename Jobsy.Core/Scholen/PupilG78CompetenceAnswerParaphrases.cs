namespace Jobsy.Core.Scholen;

/// <summary>Je-form clauses for G78 competence items 9001–9015 (concrete panel copy).</summary>
internal static class PupilG78CompetenceAnswerParaphrases
{
    private static readonly IReadOnlyDictionary<int, string> ByItemId = new Dictionary<int, string>
    {
        [9001] = "je snel merkt als een vriend verdrietig is",
        [9002] = "je je huiswerk afmaakt, ook als je geen zin hebt",
        [9003] = "je rustig blijft als je een slecht cijfer terugkrijgt",
        [9004] = "je graag een nieuwe manier bedenkt om iets te doen",
        [9005] = "je in een nieuwe klas snel met anderen praat",
        [9006] = "je graag een klasgenoot helpt die iets niet snapt",
        [9007] = "je een plan maakt voordat je aan een klus begint",
        [9008] = "je bij een lastige toets blijft nadenken zonder paniek",
        [9009] = "je het leuk vindt om iets nieuws te proberen",
        [9010] = "je je op je gemak voelt in een drukke groep",
        [9011] = "je rekening houdt met anderen in een groep",
        [9012] = "je taken niet laat liggen tot het echt niet meer kan",
        [9013] = "je niet snel in paniek raakt als iets tegenzit",
        [9014] = "je openstaat voor een nieuwe manier van werken",
        [9015] = "je je durft te melden in een groep",
    };

    public static bool TryGet(int itemId, out string clause)
        => ByItemId.TryGetValue(itemId, out clause!);
}
