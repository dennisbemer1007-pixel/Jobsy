using System.Text.RegularExpressions;
using Jobsy.Core.Interfaces;

namespace Jobsy.Infrastructure.Services.CandidateExternalVacancies;

public sealed class ExternalVacancyContactFinder : IExternalVacancyContactFinder
{
    private static readonly Regex EmailRegex = new(
        @"[a-zA-Z0-9._%+\-]+@[a-zA-Z0-9.\-]+\.[a-zA-Z]{2,}",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    private readonly IExternalVacancyUrlFetchService _fetch;

    public ExternalVacancyContactFinder(IExternalVacancyUrlFetchService fetch)
    {
        _fetch = fetch;
    }

    public async Task<IReadOnlyList<string>> FindEmployerEmailsAsync(
        Uri vacancyUrl,
        string companyName,
        CancellationToken cancellationToken = default)
    {
        var hosts = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { vacancyUrl.Host };
        var pages = new List<Uri>
        {
            new($"{vacancyUrl.Scheme}://{vacancyUrl.Host}/"),
            new($"{vacancyUrl.Scheme}://{vacancyUrl.Host}/contact")
        };

        var found = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var page in pages)
        {
            var fetched = await _fetch.FetchAsync(page, cancellationToken);
            if (fetched is null)
            {
                continue;
            }

            foreach (Match match in EmailRegex.Matches(fetched.Value.Html))
            {
                found.Add(match.Value.ToLowerInvariant());
            }
        }

        return found
            .OrderByDescending(ScoreEmail)
            .Take(5)
            .ToList();
    }

    private static int ScoreEmail(string email)
    {
        var lower = email.ToLowerInvariant();
        if (lower.Contains("vacature", StringComparison.Ordinal) || lower.Contains("sollicit", StringComparison.Ordinal))
        {
            return 3;
        }

        if (lower.Contains("hr@", StringComparison.Ordinal) || lower.Contains("jobs@", StringComparison.Ordinal))
        {
            return 2;
        }

        return 1;
    }
}
