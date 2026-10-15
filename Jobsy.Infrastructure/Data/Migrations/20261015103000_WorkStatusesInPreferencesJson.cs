using Jobsy.Infrastructure.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Jobsy.Infrastructure.Data.Migrations;

/// <inheritdoc />
[DbContext(typeof(JobsyDbContext))]
[Migration("20261015103000_WorkStatusesInPreferencesJson")]
public partial class WorkStatusesInPreferencesJson : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        // Work status codes live in Users."PreferencesJson" → workStatuses (string array).
        // Idempotent marker migration: no schema change; safe on acceptatie re-deploy.
        migrationBuilder.Sql("SELECT 1;");
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
    }
}
