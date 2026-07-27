using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Jobsy.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class OrgSalaryTablesWithBranches : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_CompanySalaryTables_CompanyId",
                table: "CompanySalaryTables");

            migrationBuilder.AddColumn<bool>(
                name: "IsSystemWml",
                table: "CompanySalaryTables",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateTable(
                name: "CompanySalaryTableAllowedBranches",
                columns: table => new
                {
                    SalaryTableId = table.Column<Guid>(type: "uuid", nullable: false),
                    CompanyId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CompanySalaryTableAllowedBranches", x => new { x.SalaryTableId, x.CompanyId });
                    table.ForeignKey(
                        name: "FK_CompanySalaryTableAllowedBranches_Companies_CompanyId",
                        column: x => x.CompanyId,
                        principalTable: "Companies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CompanySalaryTableAllowedBranches_CompanySalaryTables_Salar~",
                        column: x => x.SalaryTableId,
                        principalTable: "CompanySalaryTables",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CompanySalaryTableChangeLogs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SalaryTableId = table.Column<Guid>(type: "uuid", nullable: false),
                    Action = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    ActorUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    ActorEmail = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    Message = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    DetailsJson = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CompanySalaryTableChangeLogs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CompanySalaryTableChangeLogs_CompanySalaryTables_SalaryTabl~",
                        column: x => x.SalaryTableId,
                        principalTable: "CompanySalaryTables",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            // Mark legacy WML rows and consolidate to one system table per organization.
            migrationBuilder.Sql("""
                UPDATE "CompanySalaryTables"
                SET "IsSystemWml" = TRUE,
                    "Name" = 'Wettelijk Minimumloon'
                WHERE "Name" IN ('WML', 'Wettelijk Minimumloon');

                WITH ranked AS (
                    SELECT t."Id" AS table_id,
                           COALESCE(c."ParentCompanyId", c."Id") AS org_id,
                           ROW_NUMBER() OVER (
                               PARTITION BY COALESCE(c."ParentCompanyId", c."Id")
                               ORDER BY CASE WHEN c."ParentCompanyId" IS NULL THEN 0 ELSE 1 END, t."Id"
                           ) AS rn
                    FROM "CompanySalaryTables" t
                    INNER JOIN "Companies" c ON c."Id" = t."CompanyId"
                    WHERE t."IsSystemWml" = TRUE
                ),
                keepers AS (
                    SELECT table_id, org_id FROM ranked WHERE rn = 1
                ),
                extras AS (
                    SELECT r.table_id, k.table_id AS keep_id
                    FROM ranked r
                    INNER JOIN keepers k ON k.org_id = r.org_id
                    WHERE r.rn > 1
                )
                UPDATE "Vacancies" v
                SET "SalaryTableId" = e.keep_id
                FROM extras e
                WHERE v."SalaryTableId" = e.table_id;

                WITH ranked AS (
                    SELECT t."Id" AS table_id,
                           COALESCE(c."ParentCompanyId", c."Id") AS org_id,
                           ROW_NUMBER() OVER (
                               PARTITION BY COALESCE(c."ParentCompanyId", c."Id")
                               ORDER BY CASE WHEN c."ParentCompanyId" IS NULL THEN 0 ELSE 1 END, t."Id"
                           ) AS rn
                    FROM "CompanySalaryTables" t
                    INNER JOIN "Companies" c ON c."Id" = t."CompanyId"
                    WHERE t."IsSystemWml" = TRUE
                )
                DELETE FROM "CompanySalaryTables" t
                USING ranked r
                WHERE t."Id" = r.table_id AND r.rn > 1;

                UPDATE "CompanySalaryTables" t
                SET "CompanyId" = COALESCE(c."ParentCompanyId", c."Id"),
                    "Name" = 'Wettelijk Minimumloon',
                    "IsSystemWml" = TRUE,
                    "IsActive" = TRUE
                FROM "Companies" c
                WHERE c."Id" = t."CompanyId" AND t."IsSystemWml" = TRUE;

                INSERT INTO "CompanySalaryTableAllowedBranches" ("SalaryTableId", "CompanyId")
                SELECT t."Id", t."CompanyId"
                FROM "CompanySalaryTables" t
                INNER JOIN "Companies" c ON c."Id" = t."CompanyId"
                WHERE t."IsSystemWml" = FALSE AND c."ParentCompanyId" IS NOT NULL
                ON CONFLICT DO NOTHING;

                UPDATE "CompanySalaryTables" t
                SET "CompanyId" = c."ParentCompanyId"
                FROM "Companies" c
                WHERE c."Id" = t."CompanyId"
                  AND t."IsSystemWml" = FALSE
                  AND c."ParentCompanyId" IS NOT NULL;
                """);

            migrationBuilder.CreateIndex(
                name: "IX_CompanySalaryTables_CompanyId_IsSystemWml",
                table: "CompanySalaryTables",
                columns: new[] { "CompanyId", "IsSystemWml" },
                unique: true,
                filter: "\"IsSystemWml\" = TRUE");

            migrationBuilder.CreateIndex(
                name: "IX_CompanySalaryTableAllowedBranches_CompanyId",
                table: "CompanySalaryTableAllowedBranches",
                column: "CompanyId");

            migrationBuilder.CreateIndex(
                name: "IX_CompanySalaryTableChangeLogs_CreatedAt",
                table: "CompanySalaryTableChangeLogs",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_CompanySalaryTableChangeLogs_SalaryTableId",
                table: "CompanySalaryTableChangeLogs",
                column: "SalaryTableId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CompanySalaryTableAllowedBranches");

            migrationBuilder.DropTable(
                name: "CompanySalaryTableChangeLogs");

            migrationBuilder.DropIndex(
                name: "IX_CompanySalaryTables_CompanyId_IsSystemWml",
                table: "CompanySalaryTables");

            migrationBuilder.DropColumn(
                name: "IsSystemWml",
                table: "CompanySalaryTables");

            migrationBuilder.CreateIndex(
                name: "IX_CompanySalaryTables_CompanyId",
                table: "CompanySalaryTables",
                column: "CompanyId");
        }
    }
}
