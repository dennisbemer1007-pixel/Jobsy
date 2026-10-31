namespace Jobsy.Core.Scholen;

/// <summary>Passport bijlagen conversation prompts (address the pupil as "je").</summary>
public static class PupilG78PassportTalk
{
    public static IReadOnlyList<string> Questions(IReadOnlyList<PupilVakScoreEntry> scores, bool english)
    {
        var strong = scores.FirstOrDefault(PupilVakScores.IsStrong);
        if (strong is not null)
        {
            var label = PupilVakSubjectLabels.Nl(strong.SubjectKey);
            return english
                ?
                [
                    $"What do you like most about {label}?",
                    "When were you last really proud of yourself?",
                    "What would you still like to learn?"
                ]
                :
                [
                    $"Wat vind je het leukst aan {label}?",
                    "Wanneer was je laatst echt trots op jezelf?",
                    "Wat zou je graag nog willen leren?"
                ];
        }

        return english
            ?
            [
                "Which school subject do you enjoy most?",
                "When were you last really proud of yourself?",
                "What would you still like to learn?"
            ]
            :
            [
                "Welk vak vind jij het leukst?",
                "Wanneer was je laatst echt trots op jezelf?",
                "Wat zou je graag nog willen leren?"
            ];
    }
}
