namespace Jobsy.Core.Enums;

/// <summary>
/// Employment category for vacancy placement pricing and discovery.
/// Distinct from sector <see cref="WorkType"/> (horeca, logistiek, …).
/// </summary>
public enum VacancyKind
{
    /// <summary>Commercial hiring: fixed jobs, side jobs, BBL.</summary>
    Regular = 0,

    /// <summary>Internships / stageplekken for students and pupils.</summary>
    Internship = 1,

    /// <summary>Local volunteer / maatschappelijke initiatieven.</summary>
    Volunteer = 2
}
