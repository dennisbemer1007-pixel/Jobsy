using Jobsy.Core.Interfaces;

namespace Jobsy.Core.Rules;

public static class UploadScanRules
{
    public const string InfectedMessage =
        "Dit bestand is geweigerd omdat het onveilig lijkt. Kies een ander CV.";

    public const string UnavailableMessage =
        "We kunnen je CV nu niet controleren. Probeer het later opnieuw.";

    /// <summary>
    /// Infected uploads are always rejected. Unavailable rejects only when <paramref name="failClosed"/> is true.
    /// Clean, and Unavailable while fail-open, are accepted. The caller must already have passed the magic-byte gate.
    /// </summary>
    public static bool TryAccept(UploadScanResult result, bool failClosed, out string? error)
    {
        switch (result.Verdict)
        {
            case UploadMalwareVerdict.Infected:
                error = InfectedMessage;
                return false;
            case UploadMalwareVerdict.Unavailable when failClosed:
                error = UnavailableMessage;
                return false;
            case UploadMalwareVerdict.Clean:
            case UploadMalwareVerdict.Unavailable:
                error = null;
                return true;
            default:
                error = failClosed ? UnavailableMessage : null;
                return !failClosed;
        }
    }
}
