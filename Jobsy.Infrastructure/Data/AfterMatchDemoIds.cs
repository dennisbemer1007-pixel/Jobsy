namespace Jobsy.Infrastructure.Data;

/// <summary>Deterministic after-match demo applications (Development / local E2E).</summary>
public static class AfterMatchDemoIds
{
    /// <summary>01 kandidaat wacht · 02 werkgever wat-nu · 10/11 berichten.</summary>
    public static readonly Guid WaitApp = Guid.Parse("77777771-0001-4000-8000-000000000001");

    /// <summary>04 kandidaat moment kiezen.</summary>
    public static readonly Guid PickSlotsApp = Guid.Parse("77777771-0002-4000-8000-000000000002");

    /// <summary>05 bevestiging · 06 mails · 07 herinneringen · 08 verzetten · 12 uitkomst (werkgever).</summary>
    public static readonly Guid ScheduledApp = Guid.Parse("77777771-0003-4000-8000-000000000003");

    /// <summary>09 werkgever verzetten.</summary>
    public static readonly Guid RescheduleApp = Guid.Parse("77777771-0004-4000-8000-000000000004");

    /// <summary>13 kandidaat nu niet.</summary>
    public static readonly Guid NotNowApp = Guid.Parse("77777771-0005-4000-8000-000000000005");

    public static readonly Guid DemoVacancyId = Guid.Parse("77777771-00aa-4000-8000-0000000000aa");
}
