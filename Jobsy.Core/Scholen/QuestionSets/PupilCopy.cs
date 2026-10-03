using Jobsy.Core.Enums;

namespace Jobsy.Core.Scholen.QuestionSets;

/// <summary>
/// Picks the VO copy variant (<c>key.Vo</c> / "docent") for a class's own test.
/// Infrastructure uses the Dutch helpers; Web looks up <see cref="Key"/>.
/// </summary>
public static class PupilCopy
{
    public static string Key(PupilQuestionSet set, string baseKey)
        => set == PupilQuestionSet.Vo ? baseKey + ".Vo" : baseKey;

    public static string Key(PupilQuestionSetDef def, string baseKey)
        => Key(def.Set, baseKey);

    public static string LoginInvalid(PupilQuestionSet set) => set == PupilQuestionSet.Vo
        ? "Die code klopt niet bij deze klas. Kijk goed op je kaartje of vraag je docent."
        : "Die code klopt niet bij deze klas. Kijk goed op je kaartje of vraag je leraar.";

    public static string LoginCooldown(PupilQuestionSet set) => set == PupilQuestionSet.Vo
        ? "Even pauze. Probeer het over een kwartier opnieuw of vraag je docent."
        : "Even pauze. Probeer het over een kwartier opnieuw of vraag je leraar.";

    public static string LoginWindow(PupilQuestionSet set) => set == PupilQuestionSet.Vo
        ? "Het testvenster van je klas is dicht. Je docent zet het weer open."
        : "Het testvenster van je klas is dicht. Je leraar zet het weer open.";

    public static string AnswersSavedWindow(PupilQuestionSet set) => set == PupilQuestionSet.Vo
        ? "Je antwoorden zijn bewaard. Je docent zet de test weer open."
        : "Je antwoorden zijn bewaard. Je leraar zet de test weer open.";
}
