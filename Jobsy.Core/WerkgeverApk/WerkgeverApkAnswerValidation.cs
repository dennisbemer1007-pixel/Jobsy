namespace Jobsy.Core.WerkgeverApk;

public static class WerkgeverApkAnswerValidation
{
    public const int AbChoiceA = 1;
    public const int AbChoiceB = 2;

    public const int CheckJa = 1;
    public const int CheckEenBeetje = 2;
    public const int CheckNogNiet = 3;

    public static bool IsValidAbChoice(int value) => value is AbChoiceA or AbChoiceB;

    public static bool IsValidCheck(int value) => value is >= CheckJa and <= CheckNogNiet;

    public static bool IsValidForItem(WerkgeverApkAbItem item, int value)
        => item.Kind switch
        {
            WerkgeverApkAbItemKind.Ab => IsValidAbChoice(value),
            WerkgeverApkAbItemKind.Check => IsValidCheck(value),
            _ => false,
        };
}