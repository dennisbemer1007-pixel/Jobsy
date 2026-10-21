namespace Jobsy.Web.Hosting;

internal static class CandidateBffHttp
{
    public const string FetchHeaderName = "X-Jobsy-Bff";

    public static bool WantsJson(HttpContext http)
        => string.Equals(http.Request.Headers[FetchHeaderName], "1", StringComparison.Ordinal)
           || (http.Request.Headers.Accept.ToString()?.Contains("application/json", StringComparison.OrdinalIgnoreCase) ?? false);

    public static IResult JsonOk(string? redirect = null)
    {
        if (string.IsNullOrWhiteSpace(redirect))
        {
            return Results.Json(new { ok = true });
        }

        return Results.Json(new { ok = true, redirect });
    }

    public static IResult JsonFail(string message, int status = 400)
        => Results.Json(new { ok = false, error = message }, statusCode: status);
}
