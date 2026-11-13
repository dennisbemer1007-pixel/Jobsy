using Jobsy.Infrastructure.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Jobsy.Infrastructure.Data.Migrations;

/// <inheritdoc />
[DbContext(typeof(JobsyDbContext))]
[Migration("20261030103000_AddPupilVakScoresJson")]
public partial class AddPupilVakScoresJson : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(
            """
            ALTER TABLE "PupilProgresses" ADD COLUMN IF NOT EXISTS "VakScoresJson" text;
            """);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(
            """
            ALTER TABLE "PupilProgresses" DROP COLUMN IF EXISTS "VakScoresJson";
            """);
    }
}
