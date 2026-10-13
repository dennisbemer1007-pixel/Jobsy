using System.Net;

namespace Jobsy.Core.Email;

/// <summary>
/// Legacy helpers removed in emails-2. Prefer <see cref="EmailLinks"/>, <see cref="EmailFormat"/>,
/// and <see cref="EmailRenderer"/>. Kept only as a thin Escape shim for transitional call sites.
/// </summary>
public static class EmailLayout
{
    public static string Escape(string? value)
        => WebUtility.HtmlEncode(value ?? string.Empty);

    public static string Absolute(string? publicWebBaseUrl, string relativePath)
        => EmailLinks.For(
                string.IsNullOrWhiteSpace(publicWebBaseUrl)
                    ? throw new ArgumentException("Public web base URL is required.", nameof(publicWebBaseUrl))
                    : publicWebBaseUrl)
            .Absolute(relativePath);

    public static string LoginUrl(string? publicWebBaseUrl)
        => EmailLinks.For(Require(publicWebBaseUrl)).Login;

    public static string VacancyUrl(string? publicWebBaseUrl, Guid vacancyId)
        => EmailLinks.For(Require(publicWebBaseUrl)).Vacancy(vacancyId);

    private static string Require(string? publicWebBaseUrl)
        => string.IsNullOrWhiteSpace(publicWebBaseUrl)
            ? throw new ArgumentException("Public web base URL is required.", nameof(publicWebBaseUrl))
            : publicWebBaseUrl;
}
