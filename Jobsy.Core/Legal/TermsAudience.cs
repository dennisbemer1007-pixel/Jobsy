namespace Jobsy.Core.Legal;

/// <summary>
/// Which terms document a reader is on. Both routes render the same page component; this decides
/// the body and which pill of the audience switch is current (04.2).
/// </summary>
public enum TermsAudience
{
    Employer,
    Candidate
}
