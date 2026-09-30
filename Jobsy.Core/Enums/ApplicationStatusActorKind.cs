namespace Jobsy.Core.Enums;

/// <summary>Who caused an application status history row (never exposed to candidates).</summary>
public enum ApplicationStatusActorKind
{
    Candidate = 0,
    Employer = 1,
    System = 2
}
