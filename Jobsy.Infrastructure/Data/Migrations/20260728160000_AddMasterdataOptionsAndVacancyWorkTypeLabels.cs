using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Jobsy.Infrastructure.Data.Migrations
{
    [DbContext(typeof(JobsyDbContext))]
    [Migration("20260728160000_AddMasterdataOptionsAndVacancyWorkTypeLabels")]
    public partial class AddMasterdataOptionsAndVacancyWorkTypeLabels : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "WorkTypeLabels",
                table: "Vacancies",
                type: "character varying(512)",
                maxLength: 512,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "MasterdataOptions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Category = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Value = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    Label = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    SortOrder = table.Column<int>(type: "integer", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    ShowOnCandidate = table.Column<bool>(type: "boolean", nullable: false),
                    ShowOnVacancy = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MasterdataOptions", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_MasterdataOptions_Category_SortOrder",
                table: "MasterdataOptions",
                columns: new[] { "Category", "SortOrder" });

            migrationBuilder.CreateIndex(
                name: "IX_MasterdataOptions_Category_Value",
                table: "MasterdataOptions",
                columns: new[] { "Category", "Value" },
                unique: true);

            // Backfill vacancy labels from existing WorkTypes flags where possible.
            migrationBuilder.Sql("""
                UPDATE "Vacancies"
                SET "WorkTypeLabels" = TRIM(BOTH ', ' FROM CONCAT_WS(', ',
                    CASE WHEN ("WorkTypes" & 1) <> 0 THEN 'Horeca' END,
                    CASE WHEN ("WorkTypes" & 2) <> 0 THEN 'Winkel' END,
                    CASE WHEN ("WorkTypes" & 4) <> 0 THEN 'Logistiek' END,
                    CASE WHEN ("WorkTypes" & 8) <> 0 THEN 'Tuinbouw' END,
                    CASE WHEN ("WorkTypes" & 16) <> 0 THEN 'Zorg' END,
                    CASE WHEN ("WorkTypes" & 32) <> 0 THEN 'Kantoor' END,
                    CASE WHEN ("WorkTypes" & 64) <> 0 THEN 'Bouw' END,
                    CASE WHEN ("WorkTypes" & 128) <> 0 THEN 'Schoonmaak' END,
                    CASE WHEN ("WorkTypes" & 256) <> 0 THEN 'Productie' END
                ))
                WHERE "WorkTypes" <> 0 AND ("WorkTypeLabels" IS NULL OR "WorkTypeLabels" = '');
                """);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "MasterdataOptions");
            migrationBuilder.DropColumn(name: "WorkTypeLabels", table: "Vacancies");
        }
    }
}
