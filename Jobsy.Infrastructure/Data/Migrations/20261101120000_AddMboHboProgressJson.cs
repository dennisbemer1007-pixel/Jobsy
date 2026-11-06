using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Jobsy.Infrastructure.Data.Migrations;

[DbContext(typeof(JobsyDbContext))]
[Migration("20261101120000_AddMboHboProgressJson")]
public partial class AddMboHboProgressJson : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(
            """
            ALTER TABLE "PupilProgresses" ADD COLUMN IF NOT EXISTS "MboHboProgressJson" text;
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(
            """
            ALTER TABLE "PupilProgresses" DROP COLUMN IF EXISTS "MboHboProgressJson";
            """);
    }
}
