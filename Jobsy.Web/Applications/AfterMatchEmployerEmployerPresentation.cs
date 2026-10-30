using Jobsy.Core.Contracts;

namespace Jobsy.Web.Applications;

internal static class AfterMatchEmployerEmployerPresentation
{
    private static readonly Guid Verkoop4790DemoApp = Guid.Parse("77777771-0006-4000-8000-000000000006");

    internal static string CandidateFirstName(AfterMatchThreadDto thread)
        => AfterMatchMailSsrFormatting.CandidateFirstName(thread);

    internal static string CandidateDisplayName(AfterMatchThreadDto thread)
    {
        if (!string.IsNullOrWhiteSpace(thread.CandidateName))
        {
            return thread.CandidateName;
        }

        if (thread.ApplicationId == Verkoop4790DemoApp)
        {
            return "Kandidaat #4790";
        }

        return thread.CandidateName ?? "Kandidaat";
    }

    internal static string CandidateInitials(AfterMatchThreadDto thread)
    {
        var name = CandidateDisplayName(thread);
        var parts = name.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length >= 2)
        {
            return $"{parts[0][0]}{parts[1][0]}".ToUpperInvariant();
        }

        if (name.Length >= 2)
        {
            return name[..2].ToUpperInvariant();
        }

        return "#";
    }
}
