using System;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Jobsy.Infrastructure.Data.Migrations
{
    [DbContext(typeof(JobsyDbContext))]
    [Migration("20260804080000_StageExclusivitySettings")]
    public partial class StageExclusivitySettings : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ExclusivitySettings",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    SchoolDomain = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    StudentNumberPattern = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    IsOpenOption = table.Column<bool>(type: "boolean", nullable: false),
                    SortOrder = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ExclusivitySettings", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ExclusivityEducations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ExclusivitySettingId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    SortOrder = table.Column<int>(type: "integer", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ExclusivityEducations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ExclusivityEducations_ExclusivitySettings_ExclusivitySettingId",
                        column: x => x.ExclusivitySettingId,
                        principalTable: "ExclusivitySettings",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.AddColumn<Guid>(
                name: "ExclusivitySettingId",
                table: "Vacancies",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "StudentNumber",
                table: "Applications",
                type: "character varying(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SchoolEmail",
                table: "Applications",
                type: "character varying(256)",
                maxLength: 256,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "StudyProgram",
                table: "Applications",
                type: "character varying(256)",
                maxLength: 256,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "StudyYear",
                table: "Applications",
                type: "character varying(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ExclusivityValidationStatus",
                table: "Applications",
                type: "character varying(32)",
                maxLength: 32,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_ExclusivitySettings_SortOrder",
                table: "ExclusivitySettings",
                column: "SortOrder");

            migrationBuilder.CreateIndex(
                name: "IX_ExclusivitySettings_SchoolDomain",
                table: "ExclusivitySettings",
                column: "SchoolDomain",
                unique: true,
                filter: "\"SchoolDomain\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_ExclusivitySettings_IsOpenOption",
                table: "ExclusivitySettings",
                column: "IsOpenOption",
                unique: true,
                filter: "\"IsOpenOption\" = TRUE");

            migrationBuilder.CreateIndex(
                name: "IX_ExclusivityEducations_ExclusivitySettingId_SortOrder",
                table: "ExclusivityEducations",
                columns: new[] { "ExclusivitySettingId", "SortOrder" });

            migrationBuilder.CreateIndex(
                name: "IX_Vacancies_ExclusivitySettingId",
                table: "Vacancies",
                column: "ExclusivitySettingId");

            migrationBuilder.AddForeignKey(
                name: "FK_Vacancies_ExclusivitySettings_ExclusivitySettingId",
                table: "Vacancies",
                column: "ExclusivitySettingId",
                principalTable: "ExclusivitySettings",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            // Seed default open option + example schools.
            migrationBuilder.Sql("""
                INSERT INTO "ExclusivitySettings" ("Id", "Name", "SchoolDomain", "StudentNumberPattern", "IsActive", "IsOpenOption", "SortOrder", "CreatedAt", "UpdatedAt")
                VALUES
                ('a0000000-0000-4000-8000-000000000001', 'Open voor alle studenten', NULL, NULL, TRUE, TRUE, 0, NOW(), NULL),
                ('a0000000-0000-4000-8000-000000000002', 'Exclusief voor Inholland', 'student.inholland.nl', '^\d{7,8}$', TRUE, FALSE, 10, NOW(), NULL),
                ('a0000000-0000-4000-8000-000000000003', 'Exclusief voor Albeda', 'student.albeda.nl', '^\d{6,10}$', TRUE, FALSE, 20, NOW(), NULL),
                ('a0000000-0000-4000-8000-000000000004', 'Exclusief voor Zadkine', 'student.zadkine.nl', '^\d{6,10}$', TRUE, FALSE, 30, NOW(), NULL),
                ('a0000000-0000-4000-8000-000000000005', 'Exclusief voor ROC Mondriaan', 'student.rocmondriaan.nl', '^\d{6,10}$', TRUE, FALSE, 40, NOW(), NULL),
                ('a0000000-0000-4000-8000-000000000006', 'Exclusief voor Hogeschool Rotterdam', 'hr.nl', '^\d{7,8}$', TRUE, FALSE, 50, NOW(), NULL)
                ON CONFLICT ("Id") DO NOTHING;

                INSERT INTO "ExclusivityEducations" ("Id", "ExclusivitySettingId", "Name", "SortOrder", "IsActive")
                SELECT gen_random_uuid(), s."Id", 'Algemeen', 0, TRUE
                FROM "ExclusivitySettings" s
                WHERE s."IsOpenOption" = FALSE
                  AND NOT EXISTS (
                    SELECT 1 FROM "ExclusivityEducations" e WHERE e."ExclusivitySettingId" = s."Id"
                  );
                """);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Vacancies_ExclusivitySettings_ExclusivitySettingId",
                table: "Vacancies");

            migrationBuilder.DropTable(name: "ExclusivityEducations");
            migrationBuilder.DropTable(name: "ExclusivitySettings");

            migrationBuilder.DropIndex(
                name: "IX_Vacancies_ExclusivitySettingId",
                table: "Vacancies");

            migrationBuilder.DropColumn(name: "ExclusivitySettingId", table: "Vacancies");
            migrationBuilder.DropColumn(name: "StudentNumber", table: "Applications");
            migrationBuilder.DropColumn(name: "SchoolEmail", table: "Applications");
            migrationBuilder.DropColumn(name: "StudyProgram", table: "Applications");
            migrationBuilder.DropColumn(name: "StudyYear", table: "Applications");
            migrationBuilder.DropColumn(name: "ExclusivityValidationStatus", table: "Applications");
        }
    }
}
