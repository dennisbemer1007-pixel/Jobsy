namespace Jobsy.Core.Uwv;

public static class UwvDriverKeys
{
    public const string Security = "security";
    public const string Connection = "connection";
    public const string Growth = "growth";

    public static IReadOnlyList<string> All { get; } =
    [
        Security,
        Connection,
        Growth
    ];

    public static bool IsValid(string key)
        => All.Contains(key, StringComparer.Ordinal);
}
