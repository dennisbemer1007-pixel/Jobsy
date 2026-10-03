using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Jobsy.Infrastructure.Data.Migrations;

/// <inheritdoc />
public partial class AddClassQuestionSet : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<int>(
            name: "QuestionSet",
            table: "SchoolClasses",
            type: "integer",
            nullable: false,
            defaultValue: 2);

        // Existing classes default to the VO test (Dennis, 2 Oct 2026). Their legacy answers
        // are handled by the cut-over in 04 (README §E). Idempotent on empty and populated DBs.
        migrationBuilder.Sql(
            """
            UPDATE "SchoolClasses"
            SET "QuestionSet" = CASE WHEN "Level" = 8 THEN 1 ELSE 2 END
            """);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(
            name: "QuestionSet",
            table: "SchoolClasses");
    }
}
