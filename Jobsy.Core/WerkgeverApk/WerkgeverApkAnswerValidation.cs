using Jobsy.Core.Rules;

namespace Jobsy.Core.WerkgeverApk;

public static class WerkgeverApkAnswerValidation
{
    /// <summary>Full lean toward pole A (G78 contrast 1).</summary>
    public const int AbChoiceA = WerkgeverApkAbContrastScale.FullA;

    /// <summary>Full lean toward pole B (G78 contrast 5).</summary>
    public const int AbChoiceB = WerkgeverApkAbContrastScale.FullB;

    public const int CheckJa = 1;
    public const int CheckEenBeetje = 2;
    public const int CheckNogNiet = 3;

    public static bool IsValidAbContrast(int value) => LikertAnswerJson.IsValidAnswer(value);

    public static bool IsValidCheck(int value) => value is >= CheckJa and <= CheckNogNiet;

    public static bool IsValidForItem(WerkgeverApkAbItem item, int value)
        => item.Kind switch
        {
            WerkgeverApkAbItemKind.Ab => IsValidAbContrast(value),
            WerkgeverApkAbItemKind.Check => IsValidCheck(value),
            _ => false,
        };
}
