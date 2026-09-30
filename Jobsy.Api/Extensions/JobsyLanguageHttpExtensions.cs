using Jobsy.Core.Localization;
using Microsoft.AspNetCore.Http;

namespace Jobsy.Api.Extensions;

public static class JobsyLanguageHttpExtensions
{
    public const string HeaderName = "X-Jobsy-Language";

    /// <summary>Reads X-Jobsy-Language once; unsupported/missing → nl.</summary>
    public static string GetJobsyLanguage(this HttpContext http)
    {
        if (http.Request.Headers.TryGetValue(HeaderName, out var values)
            && JobsyLanguages.IsSupported(values.ToString()))
        {
            return JobsyLanguages.Normalize(values.ToString());
        }

        return JobsyLanguages.Default;
    }
}
