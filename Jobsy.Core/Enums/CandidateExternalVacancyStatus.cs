namespace Jobsy.Core.Enums;

public enum CandidateExternalVacancyStatus
{
    Saved = 0,
    Applied = 1,
    Closed = 2,
    /// <summary>Hidden from Bewaard list; detail still reachable by link.</summary>
    RemovedFromSaved = 3
}
