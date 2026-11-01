namespace Jobsy.Core.Scholen.MboHbo;

public static class PupilMboHboCopy
{
    public static (string Title, string Description, int QuestionCount) BlockMeta(PupilMboHboBlock block)
        => block switch
        {
            PupilMboHboBlock.Vakken => (
                "Vakkenpakket",
                "Welke vakken en vakgebieden spreken je aan?",
                40),
            PupilMboHboBlock.Interesse => (
                "Interessetest (RIASEC)",
                "Welke soort activiteiten vind je leuk om te doen?",
                48),
            PupilMboHboBlock.Competentie => (
                "Competentietest",
                "Waar ben je sterk in en wat wil je verder ontwikkelen?",
                40),
            PupilMboHboBlock.Beroepen => (
                "Beroepentest",
                "Welke beroepen en werkvelden passen mogelijk bij je?",
                50),
            PupilMboHboBlock.Werkwaarden => (
                "Werkwaarden & werkcultuur",
                "Wat vind je belangrijk in je werk en je werkplek?",
                41),
            _ => ("", "", 0)
        };
}
