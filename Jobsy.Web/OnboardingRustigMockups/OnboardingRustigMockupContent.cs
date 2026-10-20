namespace Jobsy.Web.OnboardingRustigMockups;

public static class OnboardingRustigMockupContent
{
    public sealed record TestQuestionMock(
        string TabLabel,
        string Statement,
        int QuestionIndex,
        int SelectedAnswer,
        IReadOnlyList<string> TabStates);

    public sealed record TestResultMock(string TabLabel, string Title, string Summary);

    public static TestResultMock TestResult(string? testKey) => testKey switch
    {
        "werkt" => new("Hoe je werkt", "Samen & aardig", "Je werkt graag samen en houdt rekening met anderen."),
        "leuk" => new("Leuk", "Praktisch bezig", "Je krijgt energie van doen, maken en aanpakken."),
        "sfeer" => new("Sfeer", "Informeel & warm", "Je voelt je op je gemak in een relaxte werksfeer."),
        "waarden" => new("Waarden", "Zinvol & betrokken", "Je wilt dat je werk ertoe doet voor anderen of de wereld."),
        _ => new("Test", "Jouw uitslag", "Dit is een voorbeeldresultaat voor de mockup."),
    };

    public static (string Title, string Lead) TestIntroCopy(string? testKey) => testKey switch
    {
        "werkt" => ("Hoe je werkt", "5 korte stellingen over samenwerken en tempo op het werk."),
        "leuk" => ("Leuk", "Wat geeft jou energie als je bezig bent?"),
        "sfeer" => ("Sfeer", "Welke werksfeer past het best bij jou?"),
        "waarden" => ("Waarden", "Wat vind jij belangrijk in werk en organisatie?"),
        _ => ("Test", "Beantwoord een paar korte stellingen."),
    };

    public static TestQuestionMock TestQuestion(string? testKey) => testKey switch
    {
        "werkt" => new TestQuestionMock(
            "Hoe je werkt",
            "Ik werk graag samen met anderen om een klus af te ronden.",
            1, 4,
            ["is-current", "", "", ""]),
        "leuk" => new TestQuestionMock(
            "Leuk",
            "Ik werk graag met machines, gereedschap of buiten in de praktijk.",
            1, 3,
            ["is-done", "is-current", "", ""]),
        "sfeer" => new TestQuestionMock(
            "Sfeer",
            "Ik voel me op mijn gemak in een informele werksfeer.",
            2, 2,
            ["is-done", "is-done", "is-current", ""]),
        "waarden" => new TestQuestionMock(
            "Waarden",
            "Ik vind het belangrijk dat mijn werk bijdraagt aan iets zinnigs.",
            3, 5,
            ["is-done", "is-done", "is-done", "is-current"]),
        _ => new TestQuestionMock(
            "Test",
            "Ik werk graag samen met anderen om een klus af te ronden.",
            1, 3,
            ["is-current", "", "", ""]),
    };

    public static readonly string[] TransportLabels =
    [
        "Fiets",
        "Auto",
        "Openbaar vervoer",
        "Lopen",
        "Scooter",
    ];

    public static readonly string[] TravelMinutes = ["15", "30", "45", "60"];

    public static readonly string[] EducationLabels =
    [
        "Geen",
        "VMBO",
        "HAVO/VWO",
        "MBO",
        "HBO",
        "WO",
    ];

    public static readonly string[] DreamJobTitles =
    [
        "Verpleegkundige",
        "Software developer",
        "Leerkracht basisonderwijs",
        "Elektricien",
        "Marketing specialist",
    ];

    public static readonly string[] WorkStatusLabels =
    [
        "Scholier",
        "Student (mbo/hbo/wo)",
        "Stagiair / afstudeerder",
        "In loondienst, fulltime",
        "In loondienst, parttime",
        "Flexwerker",
        "Zzp'er",
        "Op dit moment geen baan",
        "Herintreder",
        "Gepensioneerd",
        "Ondernemer",
    ];

    public sealed record AvailRow(string Day, bool[] On);

    public static readonly AvailRow[] AvailDays =
    [
        new("Ma", [true, true, false, false]),
        new("Di", [true, true, false, false]),
        new("Wo", [false, true, true, false]),
        new("Do", [true, true, false, false]),
        new("Vr", [true, false, false, false]),
        new("Za", [false, false, true, false]),
        new("Zo", [false, false, true, false]),
    ];
}
