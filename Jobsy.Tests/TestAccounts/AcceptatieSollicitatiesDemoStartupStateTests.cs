using Jobsy.Infrastructure.Ops;

namespace Jobsy.Tests.TestAccounts;

public sealed class AcceptatieSollicitatiesDemoStartupStateTests
{
    [Fact]
    public void RecordError_includes_type_and_truncates_without_email()
    {
        var ex = new InvalidOperationException(
            "The instance of entity type 'Company' cannot be tracked. Contact user@example.com for help.");
        AcceptatieSollicitatiesDemoStartupState.RecordError(ex);
        var snap = AcceptatieSollicitatiesDemoStartupState.Snapshot();
        Assert.NotNull(snap.Outcome);
        Assert.StartsWith("error:InvalidOperationException:", snap.Outcome, StringComparison.Ordinal);
        Assert.Contains("[email]", snap.Outcome, StringComparison.Ordinal);
        Assert.DoesNotContain("user@example.com", snap.Outcome, StringComparison.Ordinal);
        Assert.True(snap.Outcome!.Length <= 200);
    }

    [Fact]
    public void Format_error_outcome_puts_postgres_constraint_name_before_message()
    {
        var outcome = AcceptatieSollicitatiesDemoStartupState.FormatErrorOutcome(
            "DbUpdateException",
            "FK_Applications_Vacancies_VacancyId",
            "23503: insert or update on table \"Applications\" violates foreign key constraint");

        Assert.StartsWith(
            "error:DbUpdateException:FK_Applications_Vacancies_VacancyId:",
            outcome,
            StringComparison.Ordinal);
        Assert.True(outcome.Length <= 200);
    }
}
