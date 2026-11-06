namespace Jobsy.Core.Authorization;

/// <summary>Anonymous parent read-only passport access via pupil code (separate from pupil and staff auth).</summary>
public static class ParentPassportAuthDefaults
{
    public const string Scheme = "ParentPassport";
    public const string CookieName = "Lobsy.Ouder";

    /// <summary>Days after <see cref="Entities.Scholen.PupilCode.CreatedAtUtc"/> that parent lookup stays valid.</summary>
    public static readonly TimeSpan AccessWindow = TimeSpan.FromDays(30);
}

public static class ParentPassportClaimTypes
{
    public const string PupilCodeId = "parent_pupil_code_id";

    /// <summary>Unix seconds when parent access ends (min of code window and cookie).</summary>
    public const string AccessExpiresUtc = "parent_access_expires";
}
