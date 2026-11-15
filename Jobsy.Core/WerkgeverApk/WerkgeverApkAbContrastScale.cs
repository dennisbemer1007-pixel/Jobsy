namespace Jobsy.Core.WerkgeverApk;

/// <summary>
/// Five-point bipolar scale aligned with G78 contrast items (g78-2):
/// <see cref="Jobsy.Core.Scholen.QuestionSets.PupilQuestionSetRegistry"/> V2 labels.
/// </summary>
public static class WerkgeverApkAbContrastScale
{
    public const int FullA = 1;
    public const int LeanA = 2;
    public const int Both = 3;
    public const int LeanB = 4;
    public const int FullB = 5;

    public static readonly IReadOnlyList<(int Value, string LabelNl)> Labels =
    [
        (1, "Veel meer A"),
        (2, "Een beetje meer A"),
        (3, "Allebei even"),
        (4, "Een beetje meer B"),
        (5, "Veel meer B"),
    ];

    public static int FillPercent(int answerValue) => answerValue switch
    {
        1 => 0,
        2 => 25,
        3 => 50,
        4 => 75,
        5 => 100,
        _ => 0,
    };
}