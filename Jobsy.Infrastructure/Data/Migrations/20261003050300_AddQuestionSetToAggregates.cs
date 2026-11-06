using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Jobsy.Infrastructure.Data.Migrations;

/// <inheritdoc />
public partial class AddQuestionSetToAggregates : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropIndex(
            name: "IX_SchoolYearAggregates_SchoolId_SchoolYearStart",
            table: "SchoolYearAggregates");

        migrationBuilder.DropIndex(
            name: "IX_SchoolClassAggregates_SchoolId_SchoolYearStart",
            table: "SchoolClassAggregates");

        migrationBuilder.AddColumn<int>(
            name: "QuestionSet",
            table: "SchoolYearAggregates",
            type: "integer",
            nullable: false,
            defaultValue: 2);

        migrationBuilder.AddColumn<int>(
            name: "QuestionSet",
            table: "SchoolClassAggregates",
            type: "integer",
            nullable: false,
            defaultValue: 2);

        // G78 class results that still carry legacy scoring version "1" → "g78-1".
        // VO-class results keep "1" until 04's cut-over (README §E). Normally 0 rows.
        migrationBuilder.Sql(
            """
            UPDATE "PupilResults"
            SET "ScoringVersion" = 'g78-1'
            WHERE "ScoringVersion" = '1'
              AND "SchoolClassId" IN (
                  SELECT "Id" FROM "SchoolClasses" WHERE "QuestionSet" = 1)
            """);

        // Class aggregates: backfill from class Level (Groep78 = 8 → set 1, else VO = 2).
        migrationBuilder.Sql(
            """
            UPDATE "SchoolClassAggregates"
            SET "QuestionSet" = CASE WHEN "Level" = 8 THEN 1 ELSE 2 END
            """);

        // Year/platform aggregates were computed from VO classes historically → VO (2).
        migrationBuilder.Sql(
            """
            UPDATE "SchoolYearAggregates"
            SET "QuestionSet" = 2
            """);

        migrationBuilder.CreateIndex(
            name: "IX_SchoolYearAggregates_SchoolId_SchoolYearStart_QuestionSet",
            table: "SchoolYearAggregates",
            columns: new[] { "SchoolId", "SchoolYearStart", "QuestionSet" });

        migrationBuilder.CreateIndex(
            name: "IX_SchoolClassAggregates_SchoolId_SchoolYearStart_QuestionSet",
            table: "SchoolClassAggregates",
            columns: new[] { "SchoolId", "SchoolYearStart", "QuestionSet" });
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropIndex(
            name: "IX_SchoolYearAggregates_SchoolId_SchoolYearStart_QuestionSet",
            table: "SchoolYearAggregates");

        migrationBuilder.DropIndex(
            name: "IX_SchoolClassAggregates_SchoolId_SchoolYearStart_QuestionSet",
            table: "SchoolClassAggregates");

        migrationBuilder.Sql(
            """
            UPDATE "PupilResults"
            SET "ScoringVersion" = '1'
            WHERE "ScoringVersion" = 'g78-1'
            """);

        migrationBuilder.DropColumn(
            name: "QuestionSet",
            table: "SchoolYearAggregates");

        migrationBuilder.DropColumn(
            name: "QuestionSet",
            table: "SchoolClassAggregates");

        migrationBuilder.CreateIndex(
            name: "IX_SchoolYearAggregates_SchoolId_SchoolYearStart",
            table: "SchoolYearAggregates",
            columns: new[] { "SchoolId", "SchoolYearStart" });

        migrationBuilder.CreateIndex(
            name: "IX_SchoolClassAggregates_SchoolId_SchoolYearStart",
            table: "SchoolClassAggregates",
            columns: new[] { "SchoolId", "SchoolYearStart" });
    }
}
