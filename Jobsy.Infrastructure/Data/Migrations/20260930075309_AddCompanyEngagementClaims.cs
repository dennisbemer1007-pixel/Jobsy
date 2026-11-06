using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Jobsy.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddCompanyEngagementClaims : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CompanyEngagementClaims",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CompanyId = table.Column<Guid>(type: "uuid", nullable: false),
                    ItemId = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    ProofUrl = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    ProofText = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    Status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    CheckedSource = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: true),
                    CheckedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    CheckedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    RemovedReason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    RemovedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CompanyEngagementClaims", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CompanyEngagementClaims_Companies_CompanyId",
                        column: x => x.CompanyId,
                        principalTable: "Companies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CompanyEngagementClaims_Users_CheckedByUserId",
                        column: x => x.CheckedByUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "CompanyEngagementReports",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ClaimId = table.Column<Guid>(type: "uuid", nullable: false),
                    CompanyId = table.Column<Guid>(type: "uuid", nullable: false),
                    ItemId = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    ReporterEmail = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    Message = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ReviewedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CompanyEngagementReports", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CompanyEngagementReports_CompanyEngagementClaims_ClaimId",
                        column: x => x.ClaimId,
                        principalTable: "CompanyEngagementClaims",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CompanyEngagementClaims_CheckedByUserId",
                table: "CompanyEngagementClaims",
                column: "CheckedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_CompanyEngagementClaims_CompanyId_ItemId",
                table: "CompanyEngagementClaims",
                columns: new[] { "CompanyId", "ItemId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CompanyEngagementClaims_Status",
                table: "CompanyEngagementClaims",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_CompanyEngagementReports_ClaimId",
                table: "CompanyEngagementReports",
                column: "ClaimId");

            migrationBuilder.CreateIndex(
                name: "IX_CompanyEngagementReports_CreatedAtUtc",
                table: "CompanyEngagementReports",
                column: "CreatedAtUtc");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CompanyEngagementReports");

            migrationBuilder.DropTable(
                name: "CompanyEngagementClaims");
        }
    }
}
