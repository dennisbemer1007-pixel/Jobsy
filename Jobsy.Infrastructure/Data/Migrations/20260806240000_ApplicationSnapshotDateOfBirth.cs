using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Jobsy.Infrastructure.Data.Migrations
{
    [DbContext(typeof(JobsyDbContext))]
    [Migration("20260806240000_ApplicationSnapshotDateOfBirth")]
    public partial class ApplicationSnapshotDateOfBirth : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateOnly>(
                name: "SnapshotDateOfBirth",
                table: "Applications",
                type: "date",
                nullable: true);

            // Backfill from linked candidate profile when still available.
            migrationBuilder.Sql(
                """
                UPDATE "Applications" AS a
                SET "SnapshotDateOfBirth" = u."DateOfBirth"
                FROM "Users" AS u
                WHERE a."CandidateUserId" = u."Id"
                  AND a."SnapshotDateOfBirth" IS NULL
                  AND u."DateOfBirth" IS NOT NULL;
                """);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "SnapshotDateOfBirth",
                table: "Applications");
        }
    }
}
