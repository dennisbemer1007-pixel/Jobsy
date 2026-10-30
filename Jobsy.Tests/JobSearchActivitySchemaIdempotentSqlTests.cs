namespace Jobsy.Tests;

public sealed class JobSearchActivitySchemaIdempotentSqlTests
{
    [Fact]
    public void Ensure_schema_covers_activity_tables_and_feature_column()
    {
        var sql = Jobsy.Infrastructure.Data.Migrations.JobSearchActivitySchemaIdempotentSql.EnsureSchema;
        Assert.Contains("JobSearchActivities", sql, StringComparison.Ordinal);
        Assert.Contains("ActivityAgreements", sql, StringComparison.Ordinal);
        Assert.Contains("JobSearchActivityAttachments", sql, StringComparison.Ordinal);
        Assert.Contains("ActivityLogEnabled", sql, StringComparison.Ordinal);
        Assert.Contains("IF NOT EXISTS", sql, StringComparison.Ordinal);
    }
}
