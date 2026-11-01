namespace Jobsy.Tests;

public class MigrationStartupDiagnosticsTests
{
    [Theory]
    [InlineData("The model for context 'JobsyDbContext' has pending changes.", true)]
    [InlineData("42701: column \"Foo\" of relation \"Bar\" already exists", false)]
    public void Pending_model_changes_message_detection(string message, bool expected)
    {
        var ex = new InvalidOperationException(message);
        var pendingModelChanges = ex.Message.Contains("pending changes", StringComparison.OrdinalIgnoreCase);
        Assert.Equal(expected, pendingModelChanges);
    }

    [Fact]
    public void Pupil_idempotent_sql_includes_mbo_hbo_progress_column()
    {
        var sql = Jobsy.Infrastructure.Data.Migrations.PupilPassportV2SchemaIdempotentSql.EnsureColumns;
        Assert.Contains("MboHboProgressJson", sql, StringComparison.Ordinal);
        Assert.Contains("ADD COLUMN IF NOT EXISTS", sql, StringComparison.Ordinal);
    }
}
