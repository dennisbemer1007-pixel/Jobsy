namespace Jobsy.Core.Options;

/// <summary>Sfeer A illustrated background on onboarding dream-job step (step 6).</summary>
public sealed class DreamJobIllustrationOptions
{
    /// <summary>Env / config key <c>DreamJobIllustrationEnabled</c>. Missing means off.</summary>
    public const string EnabledKey = "DreamJobIllustrationEnabled";
}

public static class DreamJobIllustrationGate
{
    /// <summary>Missing or unreadable value means the illustration is off.</summary>
    public static bool IsEnabled(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
        {
            return false;
        }

        return bool.TryParse(raw, out var enabled) && enabled;
    }
}
