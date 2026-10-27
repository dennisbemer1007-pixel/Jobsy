using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Jobsy.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddPupilPuzzlesJson : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // AddPupilPassportV2Flags replays EnsureColumns at apply time (IF NOT EXISTS), so avoid plain AddColumn.
            migrationBuilder.Sql(
                """
                ALTER TABLE "PupilProgresses" ADD COLUMN IF NOT EXISTS "PuzzlesJson" text NOT NULL DEFAULT '{{}}';
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                ALTER TABLE "PupilProgresses" DROP COLUMN IF EXISTS "PuzzlesJson";
                """);
        }
    }
}
