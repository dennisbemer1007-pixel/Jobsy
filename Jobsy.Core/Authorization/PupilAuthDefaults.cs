namespace Jobsy.Core.Authorization;

/// <summary>Separate pupil cookie scheme — never shares claims with staff/candidate auth.</summary>
public static class PupilAuthDefaults
{
    public const string Scheme = "Pupil";
    public const string CookieName = "Lobsy.Leerling";

    /// <summary>Sliding idle timeout for the pupil session cookie.</summary>
    public static readonly TimeSpan IdleTimeout = TimeSpan.FromMinutes(20);

    /// <summary>Absolute lifetime from issued-at claim, regardless of activity.</summary>
    public static readonly TimeSpan AbsoluteTimeout = TimeSpan.FromMinutes(90);
}

/// <summary>Claim types on the pupil principal. No name, e-mail or staff role.</summary>
public static class PupilClaimTypes
{
    public const string PupilCodeId = "pupil_code_id";
    public const string ClassId = "class_id";
    public const string SchoolId = "school_id";
    public const string SessionVersion = "session_version";
    public const string IssuedAt = "iat";
    /// <summary>Display-only class label (e.g. "2B") for chrome; never a pupil name.</summary>
    public const string ClassLabel = "class_label";
    /// <summary>Display-only masked code (e.g. "K7Q-M2P") for chrome.</summary>
    public const string CodeDisplay = "code_display";
}
