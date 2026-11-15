namespace Jobsy.Core.Scholen.MboHbo;

/// <summary>Mockup-aligned labels (schermteksten, niet de API-bloktitels).</summary>
public static class PupilMboHboMockupCopy
{
    public static string ShortBlockTitle(PupilMboHboBlock block)
        => block switch
        {
            PupilMboHboBlock.Vakken => "Vakkenpakket",
            PupilMboHboBlock.Interesse => "Interessetest",
            PupilMboHboBlock.Competentie => "Competentietest",
            PupilMboHboBlock.Beroepen => "Beroepentest",
            PupilMboHboBlock.Werkwaarden => "Wat je belangrijk vindt",
            _ => "",
        };

    public static string TransitionStatusBlurb(PupilMboHboBlock block)
        => block switch
        {
            PupilMboHboBlock.Vakken =>
                "40 van 40 vragen zijn bewaard. Je kunt later nog iets aanpassen.",
            PupilMboHboBlock.Interesse =>
                "Welke activiteiten vind je leuk? 48 vragen, ongeveer 10 minuten.",
            PupilMboHboBlock.Competentie =>
                "Waar ben je sterk in, en wat wil je nog leren?",
            PupilMboHboBlock.Beroepen =>
                "Welke beroepen passen mogelijk bij je?",
            PupilMboHboBlock.Werkwaarden =>
                "Wat telt voor jou in je werk en op je werkplek?",
            _ => PupilMboHboCopy.BlockMeta(block).Description,
        };

    public static string PassportCardTitle(PupilMboHboBlock block)
        => block switch
        {
            PupilMboHboBlock.Interesse => "Interesses",
            PupilMboHboBlock.Competentie => "Sterke kanten",
            PupilMboHboBlock.Vakken => "Vakken die je aanspreken",
            PupilMboHboBlock.Beroepen => "Beroepen die mogelijk passen",
            PupilMboHboBlock.Werkwaarden => "Wat je belangrijk vindt",
            _ => PupilMboHboCopy.BlockMeta(block).Title,
        };

    public const string PassportVolgendeStapTitle = "Volgende stap";

    public const string PassportVolgendeStapBody =
        "Bespreek deze uitkomsten met je loopbaanbegeleider of mentor. Een meeloopdag kan meer laten zien dan een test alleen. Je kunt je antwoorden later nog aanpassen.";

    public static readonly PupilMboHboBlock[] PassportCardOrder =
    [
        PupilMboHboBlock.Interesse,
        PupilMboHboBlock.Competentie,
        PupilMboHboBlock.Vakken,
        PupilMboHboBlock.Beroepen,
        PupilMboHboBlock.Werkwaarden,
    ];
}