namespace Jobsy.Core.Scholen;

/// <summary>Hand-written dat-clause variants (9001–9060) for subordinate use after &quot;dat&quot;.</summary>
public static class PupilG78QuestionDatForms
{
    private static readonly Dictionary<int, string> DatJe = Build();

    public static string? ForItem(int itemId) =>
        DatJe.TryGetValue(itemId, out var t) ? t : null;

    public static IReadOnlyDictionary<int, string> All => DatJe;

    private static Dictionary<int, string> Build() => new()
    {
        [9001] = "je het snel merkt als een vriend verdrietig is",
        [9002] = "je je huiswerk afmaakt, ook als je geen zin hebt",
        [9003] = "je rustig blijft als je een slecht cijfer terugkrijgt",
        [9004] = "je graag een nieuwe manier bedenkt om iets te doen",
        [9005] = "je in een nieuwe klas snel met anderen praat",
        [9006] = "je graag een klasgenoot helpt die iets niet snapt",
        [9007] = "je een plan maakt voordat je aan een klus begint",
        [9008] = "je bij een lastige toets blijft nadenken zonder paniek",
        [9009] = "je het leuk vindt om iets nieuws te proberen",
        [9010] = "je je op je gemak voelt in een drukke groep",
        [9011] = "je het lastig vindt om rekening te houden met anderen",
        [9012] = "je taken laat liggen tot het echt niet meer kan",
        [9013] = "je snel in paniek raakt als iets tegenzit",
        [9014] = "je liever iets doet op de manier die je kent",
        [9015] = "je je graag op de achtergrond houdt in een groep",
        [9016] = "je het leuk vindt om iets te maken of te repareren",
        [9017] = "je graag wilt weten hoe dingen werken",
        [9018] = "je graag iets moois maakt, zoals een tekening of muziek",
        [9019] = "je graag iemand helpt die iets niet snapt",
        [9020] = "je graag de leiding neemt bij een groepsopdracht",
        [9021] = "je je spullen en planning graag netjes houdt",
        [9022] = "je graag met je handen aan iets tastbaars werkt",
        [9023] = "je graag een eigen verhaal, dans of outfit bedenkt",
        [9024] = "je het fijn vindt om anderen te steunen",
        [9025] = "je graag uitzoekt hoe een proefje of puzzels werken",
        [9026] = "je anderen graag overtuigt van jouw idee",
        [9027] = "je van vaste stappen en duidelijke lijstjes houdt",
        [9028] = "je het leuk vindt om buiten iets te bouwen of te fixen",
        [9029] = "je graag speelt of tekent om iets te laten zien",
        [9030] = "je het leuk vindt om met mensen te oefenen of te spelen",
        [9031] = "je later zelf wilt kunnen kiezen hoe je iets doet",
        [9032] = "je het belangrijk vindt om goede vrienden om je heen te hebben",
        [9033] = "je heel goed wilt worden in iets wat je doet",
        [9034] = "je het fijn vindt als je weet wat er gaat gebeuren",
        [9035] = "je later iets wilt doen waar de wereld beter van wordt",
        [9036] = "je later zelf wilt bepalen hoe je dag eruitziet",
        [9037] = "je later bij mensen wilt horen die elkaar helpen",
        [9038] = "je later trots wilt zijn op wat je hebt bereikt",
        [9039] = "je later rust en duidelijkheid in je leven wilt",
        [9040] = "je later iets wilt betekenen voor anderen",
        [9041] = "je het heel belangrijk vindt om zelf te kunnen kiezen",
        [9042] = "je later dicht bij mensen wilt staan die je kent",
        [9043] = "je later uitdagingen wilt aangaan en groeien",
        [9044] = "je later wilt weten waar je aan toe bent",
        [9045] = "je later wilt meewerken aan iets goeds voor iedereen",
        [9046] = "je het liefst op je eigen tempo werkt",
        [9047] = "je het liefst samen in een groepje werkt",
        [9048] = "je het belangrijk vindt dat iedereen zich welkom voelt",
        [9049] = "je het leuk vindt als het een beetje druk en gezellig is",
        [9050] = "je het leuk vindt als dingen vaak anders gaan",
        [9051] = "je het fijn vindt als je nieuwe ideeën mag proberen",
        [9052] = "je graag zelf wilt beslissen hoe je iets aanpakt",
        [9053] = "je graag taken deelt met anderen in een team",
        [9054] = "je erop let of iemand in de groep zich prettig voelt",
        [9055] = "je het fijn vindt als er duidelijke regels zijn",
        [9056] = "je je makkelijk aanpast als het plan verandert",
        [9057] = "je het saai vindt als alles altijd hetzelfde blijft",
        [9058] = "je je thuis voelt als je zelf de ruimte krijgt",
        [9059] = "je liever samen leert dan helemaal alleen",
        [9060] = "je sfeer en omgang belangrijker vindt dan winnen",
    };
}
