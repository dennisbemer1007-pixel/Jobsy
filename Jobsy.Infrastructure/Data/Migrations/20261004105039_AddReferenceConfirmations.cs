using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Jobsy.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddReferenceConfirmations : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ReferenceConfirmationConsentLogs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    ReferenceConfirmationId = table.Column<Guid>(type: "uuid", nullable: true),
                    Actor = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    Action = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    ConsentVersion = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    Text = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    AtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReferenceConfirmationConsentLogs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ReferenceConfirmationConsentLogs_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ReferenceConfirmations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CandidateReferenceId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    RoleTitle = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    Status = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    CandidateConsentAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ConsentVersion = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    WorkedHere = table.Column<bool>(type: "boolean", nullable: true),
                    PeriodText = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: true),
                    DidWell = table.Column<string>(type: "character varying(400)", maxLength: 400, nullable: true),
                    WorkAgain = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: true),
                    ExtraText = table.Column<string>(type: "character varying(400)", maxLength: 400, nullable: true),
                    ConfirmedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeclinedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ShowOnPartnerPassport = table.Column<bool>(type: "boolean", nullable: false),
                    ShareWorkedHere = table.Column<bool>(type: "boolean", nullable: false),
                    SharePeriod = table.Column<bool>(type: "boolean", nullable: false),
                    ShareDidWell = table.Column<bool>(type: "boolean", nullable: false),
                    ShareWorkAgain = table.Column<bool>(type: "boolean", nullable: false),
                    ShareExtra = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReferenceConfirmations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ReferenceConfirmations_CandidateReferences_CandidateReferen~",
                        column: x => x.CandidateReferenceId,
                        principalTable: "CandidateReferences",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ReferenceConfirmations_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ReferenceMisuseReports",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    ReferenceConfirmationId = table.Column<Guid>(type: "uuid", nullable: false),
                    Message = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReferenceMisuseReports", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ReferenceMisuseReports_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ReferenceConfirmationTokens",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ReferenceConfirmationId = table.Column<Guid>(type: "uuid", nullable: false),
                    TokenHash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    ExpiresAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UsedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReferenceConfirmationTokens", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ReferenceConfirmationTokens_ReferenceConfirmations_Referenc~",
                        column: x => x.ReferenceConfirmationId,
                        principalTable: "ReferenceConfirmations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ReferenceConfirmationConsentLogs_UserId",
                table: "ReferenceConfirmationConsentLogs",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_ReferenceConfirmations_CandidateReferenceId",
                table: "ReferenceConfirmations",
                column: "CandidateReferenceId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ReferenceConfirmations_UserId",
                table: "ReferenceConfirmations",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_ReferenceConfirmationTokens_ReferenceConfirmationId",
                table: "ReferenceConfirmationTokens",
                column: "ReferenceConfirmationId");

            migrationBuilder.CreateIndex(
                name: "IX_ReferenceConfirmationTokens_TokenHash",
                table: "ReferenceConfirmationTokens",
                column: "TokenHash",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ReferenceMisuseReports_ReferenceConfirmationId",
                table: "ReferenceMisuseReports",
                column: "ReferenceConfirmationId");

            migrationBuilder.CreateIndex(
                name: "IX_ReferenceMisuseReports_UserId",
                table: "ReferenceMisuseReports",
                column: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ReferenceConfirmationConsentLogs");

            migrationBuilder.DropTable(
                name: "ReferenceConfirmationTokens");

            migrationBuilder.DropTable(
                name: "ReferenceMisuseReports");

            migrationBuilder.DropTable(
                name: "ReferenceConfirmations");
        }
    }
}
