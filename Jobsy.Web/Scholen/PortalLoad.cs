using System.Net;

namespace Jobsy.Web.Scholen;

/// <summary>Distinguishes a missing class (404) from a failed portal call.</summary>
public static class PortalLoad
{
    public static bool IsNotFound(Exception ex)
        => ex is HttpRequestException { StatusCode: HttpStatusCode.NotFound };
}
