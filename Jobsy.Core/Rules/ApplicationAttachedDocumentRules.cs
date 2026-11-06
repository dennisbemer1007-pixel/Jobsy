using Jobsy.Core.Passport;

namespace Jobsy.Core.Rules;

/// <summary>
/// Which PDF travels with an application: Lobsy-CV (basics) or DNA-paspoort (all tests done at apply).
/// </summary>
public static class ApplicationAttachedDocumentRules
{
    public const string LobsyCv = "lobsy-cv";
    public const string Passport = "passport";

    public static string KindForNewApplication(bool allTestsComplete, bool passportPdfV2Enabled)
        => allTestsComplete && passportPdfV2Enabled ? Passport : LobsyCv;

    /// <summary>
    /// Legacy rows without <paramref name="snapshotKind"/> keep passport when V2 is on (pre-change behaviour).
    /// </summary>
    public static bool UsePassportPdf(string? snapshotKind, bool passportPdfV2Enabled)
    {
        if (!passportPdfV2Enabled)
        {
            return false;
        }

        return snapshotKind switch
        {
            Passport => true,
            LobsyCv => false,
            _ => true
        };
    }

    public static string EmployerDocumentLabelKey(string? snapshotKind)
        => UsePassportPdf(snapshotKind, passportPdfV2Enabled: true)
            ? "WgApp.Attached.Passport"
            : "WgApp.Attached.LobsyCv";

    public static string? EmployerFollowUpNoteKey(string? snapshotKind)
        => string.Equals(snapshotKind, LobsyCv, StringComparison.Ordinal)
            ? "WgApp.Attached.PassportFollows"
            : null;

    public static string CvDownloadButtonKey(string? snapshotKind)
        => UsePassportPdf(snapshotKind, passportPdfV2Enabled: true)
            ? "WgApp.Action.PassportPdf"
            : "WgApp.Action.Cv";
}
