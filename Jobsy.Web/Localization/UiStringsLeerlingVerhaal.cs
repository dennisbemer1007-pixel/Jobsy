namespace Jobsy.Web.Localization;

/// <summary>D12: nl-only pupil story + dream-job strings (prefixes LeerlingStory. / LeerlingDroom. / LeerlingPdf.).</summary>
public static class UiStringsLeerlingVerhaal
{
    public static void MergeNl(IDictionary<string, string> nl)
    {
        ArgumentNullException.ThrowIfNull(nl);
        foreach (var (key, value) in Jobsy.Core.Scholen.PupilVerhaalCopy.All)
        {
            nl[key] = value;
        }
    }
}
