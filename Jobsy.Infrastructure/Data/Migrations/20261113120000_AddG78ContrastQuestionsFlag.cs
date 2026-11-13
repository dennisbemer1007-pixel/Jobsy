using Jobsy.Infrastructure.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Jobsy.Infrastructure.Data.Migrations;

/// <inheritdoc />
[DbContext(typeof(JobsyDbContext))]
[Migration("20261113120000_AddG78ContrastQuestionsFlag")]
public partial class AddG78ContrastQuestionsFlag : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(PlatformFeatureSchemaIdempotentSql.EnsureFeatureFlagAdminOverrides);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
    }
}
