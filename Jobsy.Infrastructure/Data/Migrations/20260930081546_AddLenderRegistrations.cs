using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Jobsy.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddLenderRegistrations : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "LenderRegistrations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CompanyId = table.Column<Guid>(type: "uuid", nullable: false),
                    Status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    Source = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true),
                    Reference = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    CheckedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DecidedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    Note = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    ValidUntil = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LenderRegistrations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LenderRegistrations_Companies_CompanyId",
                        column: x => x.CompanyId,
                        principalTable: "Companies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_LenderRegistrations_Users_DecidedByUserId",
                        column: x => x.DecidedByUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "IX_LenderRegistrations_CompanyId",
                table: "LenderRegistrations",
                column: "CompanyId");

            migrationBuilder.CreateIndex(
                name: "IX_LenderRegistrations_CompanyId_CreatedAtUtc",
                table: "LenderRegistrations",
                columns: new[] { "CompanyId", "CreatedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_LenderRegistrations_DecidedByUserId",
                table: "LenderRegistrations",
                column: "DecidedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_LenderRegistrations_Status",
                table: "LenderRegistrations",
                column: "Status");

            // One-shot backfill: existing intermediary bureaus get NotChecked (does not block live vacancies).
            migrationBuilder.Sql("""
                INSERT INTO "LenderRegistrations" ("Id", "CompanyId", "Status", "Source", "Reference", "CheckedAtUtc", "DecidedByUserId", "Note", "ValidUntil", "CreatedAtUtc")
                SELECT gen_random_uuid(), c."Id", 'NotChecked', NULL, NULL, NULL, NULL, NULL, NULL, NOW() AT TIME ZONE 'utc'
                FROM "Companies" c
                WHERE c."Type" = 1
                  AND NOT EXISTS (
                      SELECT 1 FROM "LenderRegistrations" lr WHERE lr."CompanyId" = c."Id"
                  );
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "LenderRegistrations");
        }
    }
}
