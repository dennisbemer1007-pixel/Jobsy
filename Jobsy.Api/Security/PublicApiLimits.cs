namespace Jobsy.Api.Security;

/// <summary>Hard caps on anonymous list and map endpoints (anti-scraping).</summary>
public static class PublicApiLimits
{
    /// <summary>Masterdata options returned in one anonymous response.</summary>
    public const int MaxMasterdataOptions = 500;

    public const int DefaultListTake = 100;
    public const int MaxListTakePublic = 200;
    /// <summary>Authenticated callers (any signed-in role) may request larger pages for internal tooling.</summary>
    public const int MaxListTakeAuthenticated = 1000;

    public const int MaxDiscoverTakePublic = 200;
    public const int MaxDiscoverTakeAuthenticated = 500;

    /// <summary>Upper bound on pins returned for one bbox request (after spatial sampling).</summary>
    public const int MaxPinsPerBbox = 2500;

    /// <summary>When bbox is omitted (legacy/tests), never return more than this many pins.</summary>
    public const int MaxPinsWithoutBbox = 250;
}
