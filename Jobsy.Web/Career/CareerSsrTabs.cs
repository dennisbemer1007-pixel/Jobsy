namespace Jobsy.Web.Career;

internal static class CareerSsrTabs
{
    internal const string VoorJou = "voor-jou";
    internal const string Omscholen = "omscholen";
    internal const string Kansberoepen = "kansberoepen";

    internal static string Normalize(string? raw)
        => raw?.Trim().ToLowerInvariant() switch
        {
            Omscholen => Omscholen,
            Kansberoepen => Kansberoepen,
            _ => VoorJou,
        };

    internal static IReadOnlyList<string> All { get; } = [VoorJou, Omscholen, Kansberoepen];
}
