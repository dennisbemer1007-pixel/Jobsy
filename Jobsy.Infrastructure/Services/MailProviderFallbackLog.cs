namespace Jobsy.Infrastructure.Services;

/// <summary>Lets the Lettermint-to-Resend fallback warning fire once per process.</summary>
internal static class MailProviderFallbackLog
{
    private static int _lettermintMissingKey;

    public static bool ShouldLogLettermintFallback()
        => Interlocked.Exchange(ref _lettermintMissingKey, 1) == 0;

    internal static void ResetForTests()
        => Interlocked.Exchange(ref _lettermintMissingKey, 0);
}
