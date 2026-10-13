namespace Jobsy.Core.Rules;

/// <summary>
/// Fingerprint suffix for the competence source the coach and the who-am-i story share.
/// Basic quick-scan scores stay out of the suffix so an existing story does not look stale.
/// A completed deep test adds <c>deep:</c> and refreshes the story.
/// </summary>
public static class WhoAmICompetenceSource
{
    public static string? FingerprintSuffix(IReadOnlyList<(string Code, int Score)>? traits, bool deep)
    {
        if (!deep)
        {
            return null;
        }

        var body = traits is null
            ? ""
            : string.Join(
                ',',
                traits
                    .OrderBy(trait => trait.Code, StringComparer.Ordinal)
                    .Select(trait => trait.Code + "=" + trait.Score.ToString(System.Globalization.CultureInfo.InvariantCulture)));
        return "deep:" + body;
    }
}
