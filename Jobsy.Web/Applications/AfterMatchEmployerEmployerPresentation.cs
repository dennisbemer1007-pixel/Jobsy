using Jobsy.Core.Contracts;

namespace Jobsy.Web.Applications;

internal static class AfterMatchEmployerEmployerPresentation
{
    internal static string CandidateFirstName(AfterMatchThreadDto thread)
        => AfterMatchMailSsrFormatting.CandidateFirstName(thread);
}
