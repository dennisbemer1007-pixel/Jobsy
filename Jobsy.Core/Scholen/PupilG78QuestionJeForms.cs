using System.Text.RegularExpressions;

namespace Jobsy.Core.Scholen;

/// <summary>Reviewed je/jij story clauses per G78 item (9001–9060), fact-aligned with the Likert stem.</summary>
public static partial class PupilG78QuestionJeForms
{
    private static readonly Dictionary<int, string> AgreeJe = Build();

    public static string? ForItem(int itemId) =>
        AgreeJe.TryGetValue(itemId, out var t) ? t : null;

    public static IReadOnlyDictionary<int, string> All => AgreeJe;

    /// <summary>Detects common ik→je mistakes (bare verb stem immediately after "je").</summary>
    public static bool HasBareStemAfterJe(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return false;
        }

        foreach (Match m in BareJeStemRegex().Matches(text))
        {
            if (BareStems.Contains(m.Groups[1].Value))
            {
                return true;
            }
        }

        return false;
    }

    [GeneratedRegex(@"\bje\s+([a-zA-ZáéíóúäëïöüÁÉÍÓÚÄËÏÖÜ]+)", RegexOptions.CultureInvariant)]
    private static partial Regex BareJeStemRegex();

    private static readonly HashSet<string> BareStems = new(StringComparer.OrdinalIgnoreCase)
    {
        "begin", "doe", "maak", "help", "vind", "houd", "werk", "zoek", "neem", "deel", "leer", "speel",
        "bedenk", "overtuig", "blijf", "raak", "voel", "pas", "laat", "ken", "heb", "ga", "zit", "sta", "lig",
        "denk", "geef", "kom", "eet", "drink", "loop", "ren", "schrijf", "lees", "kies", "word", "beweeg", "merk",
        "luister", "praat", "probeer", "durf"
    };

    private static Dictionary<int, string> Build() => new()
    {
        [9001] = "Je merkt het snel als een vriend verdrietig is.",
        [9002] = "Je maakt je huiswerk af, ook als je geen zin hebt.",
        [9003] = "Je blijft rustig als je een slecht cijfer terugkrijgt.",
        [9004] = "Je bedenkt graag een nieuwe manier om iets te doen.",
        [9005] = "In een nieuwe klas praat je snel met anderen.",
        [9006] = "Je helpt graag een klasgenoot die iets niet snapt.",
        [9007] = "Je maakt een plan voordat je aan een klus begint.",
        [9008] = "Bij een lastige toets blijf je nadenken zonder paniek.",
        [9009] = "Je vindt het leuk om iets nieuws te proberen.",
        [9010] = "Je voelt je op je gemak in een drukke groep.",
        [9011] = "Je vindt het lastig om rekening te houden met anderen.",
        [9012] = "Je laat taken liggen tot het echt niet meer kan.",
        [9013] = "Je raakt snel in paniek als iets tegenzit.",
        [9014] = "Je doet liever iets op de manier die je kent.",
        [9015] = "Je houdt je graag op de achtergrond in een groep.",
        [9016] = "Je vindt het leuk om iets te maken of te repareren.",
        [9017] = "Je wilt graag weten hoe dingen werken.",
        [9018] = "Je maakt graag iets moois, zoals een tekening of muziek.",
        [9019] = "Je helpt graag iemand die iets niet snapt.",
        [9020] = "Je neemt graag de leiding bij een groepsopdracht.",
        [9021] = "Je houdt je spullen en planning graag netjes.",
        [9022] = "Je werkt graag met je handen aan iets tastbaars.",
        [9023] = "Je bedenkt graag een eigen verhaal, dans of outfit.",
        [9024] = "Je vindt het fijn om anderen te steunen.",
        [9025] = "Je zoekt graag uit hoe een proefje of puzzels werkt.",
        [9026] = "Je overtuigt anderen graag van jouw idee.",
        [9027] = "Je houdt van vaste stappen en duidelijke lijstjes.",
        [9028] = "Je vindt het leuk om buiten iets te bouwen of te fixen.",
        [9029] = "Je speelt of tekent graag om iets te laten zien.",
        [9030] = "Je vindt het leuk om met mensen te oefenen of te spelen.",
        [9031] = "Je wilt later zelf kunnen kiezen hoe je iets doet.",
        [9032] = "Je vindt het belangrijk om goede vrienden om je heen te hebben.",
        [9033] = "Je wilt heel goed worden in iets wat je doet.",
        [9034] = "Je vindt het fijn als je weet wat er gaat gebeuren.",
        [9035] = "Je wilt later iets doen waar de wereld beter van wordt.",
        [9036] = "Je wilt later zelf bepalen hoe je dag eruitziet.",
        [9037] = "Je wilt later bij mensen horen die elkaar helpen.",
        [9038] = "Je wilt later trots zijn op wat je hebt bereikt.",
        [9039] = "Je wilt later rust en duidelijkheid in je leven.",
        [9040] = "Je wilt later iets betekenen voor anderen.",
        [9041] = "Je vindt het heel belangrijk om zelf te kunnen kiezen.",
        [9042] = "Je wilt later dicht bij mensen staan die je kent.",
        [9043] = "Je wilt later uitdagingen aangaan en groeien.",
        [9044] = "Je wilt later weten waar je aan toe bent.",
        [9045] = "Je wilt later meewerken aan iets goeds voor iedereen.",
        [9046] = "Je werkt het liefst op je eigen tempo.",
        [9047] = "Je werkt het liefst samen in een groepje.",
        [9048] = "Je vindt het belangrijk dat iedereen zich welkom voelt.",
        [9049] = "Je vindt het leuk als het een beetje druk en gezellig is.",
        [9050] = "Je vindt het leuk als dingen vaak anders gaan.",
        [9051] = "Je vindt het fijn als je nieuwe ideeën mag proberen.",
        [9052] = "Je wilt graag zelf beslissen hoe je iets aanpakt.",
        [9053] = "Je deelt graag taken met anderen in een team.",
        [9054] = "Je let erop of iemand in de groep zich prettig voelt.",
        [9055] = "Je vindt het fijn als er duidelijke regels zijn.",
        [9056] = "Je past je makkelijk aan als het plan verandert.",
        [9057] = "Je vindt het saai als alles altijd hetzelfde blijft.",
        [9058] = "Je voelt je thuis als je zelf de ruimte krijgt.",
        [9059] = "Je leert liever samen dan helemaal alleen.",
        [9060] = "Je vindt sfeer en omgang belangrijker dan winnen.",
    };
}
