namespace Jobsy.Core.Enums;

public enum InterviewStatus
{
    Proposing = 0,
    Scheduled = 1,
    RescheduleRequested = 2,
    Cancelled = 3,
    Done = 4
}

public enum InterviewFormat
{
    OnSite = 0,
    Online = 1,
    Phone = 2
}

public enum InterviewSlotProposer
{
    Employer = 0,
    Candidate = 1
}

public enum InterviewChangeKind
{
    Reschedule = 0,
    Cancel = 1
}

public enum InterviewReminderMoment
{
    WeekBefore = 0,
    DayBefore18 = 1,
    TwoHoursBefore = 2
}

public enum InterviewReminderChannel
{
    Email = 0,
    Push = 1
}

public enum AfterMatchOutcomeKind
{
    Offer = 0,
    NextRound = 1,
    NotNow = 2
}

public enum ApplicationMessageBlockReason
{
    None = 0,
    IbanBeforeOffer = 1,
    EmployerPhoneBeforeInterview = 2
}
