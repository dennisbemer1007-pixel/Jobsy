using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Jobsy.Infrastructure.Data.Migrations;

/// <inheritdoc />
public partial class StartVoTestFresh : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        // Cut-over to two separate tests (Dennis, 2 Oct 2026). Answers in VO classes were made on
        // the legacy 60-item flow, before the VO test existed. They belong to neither test and may
        // not be reused, moved or linked (anonymity). On acceptatie this is pilot/test data.
        // Fresh start: each code starts the VO test at question 1.
        // SessionVersion is bumped only for codes that still have progress, a result, or a
        // non-NotStarted status, so a second run is a no-op.
        migrationBuilder.Sql(
            """
            UPDATE "PupilCodes" AS c
            SET "Status" = 0,
                "SessionVersion" = c."SessionVersion" + 1
            FROM "SchoolClasses" sc
            WHERE sc."Id" = c."SchoolClassId"
              AND sc."QuestionSet" = 2
              AND (
                  c."Status" <> 0
                  OR EXISTS (SELECT 1 FROM "PupilProgresses" p WHERE p."PupilCodeId" = c."Id")
                  OR EXISTS (SELECT 1 FROM "PupilResults" r WHERE r."PupilCodeId" = c."Id")
              );

            DELETE FROM "PupilResults"
            WHERE "PupilCodeId" IN (
                SELECT c."Id"
                FROM "PupilCodes" c
                INNER JOIN "SchoolClasses" sc ON sc."Id" = c."SchoolClassId"
                WHERE sc."QuestionSet" = 2);

            DELETE FROM "PupilProgresses"
            WHERE "PupilCodeId" IN (
                SELECT c."Id"
                FROM "PupilCodes" c
                INNER JOIN "SchoolClasses" sc ON sc."Id" = c."SchoolClassId"
                WHERE sc."QuestionSet" = 2);

            DELETE FROM "SchoolClassAggregates" WHERE "QuestionSet" = 2;
            DELETE FROM "SchoolYearAggregates" WHERE "QuestionSet" = 2;
            """);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        // No-op: deleted legacy VO answers cannot be restored (pilot data; production is checked first).
    }
}
