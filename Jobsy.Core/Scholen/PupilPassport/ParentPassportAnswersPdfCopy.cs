using Jobsy.Core.Scholen;

namespace Jobsy.Core.Scholen.PupilPassport;

public static class ParentPassportAnswersPdfCopy
{
    public static string Get(string key, string? language)
    {
        if (PupilVerhaalLocalized.IsEnglish(language))
        {
            return key switch
            {
                "Ouder.AnswersPdf.Header" => "TEST ANSWERS",
                "Ouder.AnswersPdf.Intro" =>
                    "These are the answers your child chose during the test. There are no scores or percentages.",
                "Ouder.AnswersPdf.AnswerLabel" => "Chosen answer",
                "Ouder.AnswersPdf.Footer" =>
                    "No grade and no school advice. Lobsy does not store names. Answers are deleted after this school year.",
                _ => Nl(key)
            };
        }

        return Nl(key);
    }

    private static string Nl(string key) => key switch
    {
        "Ouder.AnswersPdf.Header" => "TESTANTWOORDEN",
        "Ouder.AnswersPdf.Intro" =>
            "Dit zijn de antwoorden die je kind tijdens de test heeft gekozen. Er staan geen scores of percentages bij.",
        "Ouder.AnswersPdf.AnswerLabel" => "Gekozen antwoord",
        "Ouder.AnswersPdf.Footer" =>
            "Geen cijfer en geen schooladvies. Lobsy bewaart geen namen. Antwoorden worden na dit schooljaar verwijderd.",
        _ => key
    };
}
