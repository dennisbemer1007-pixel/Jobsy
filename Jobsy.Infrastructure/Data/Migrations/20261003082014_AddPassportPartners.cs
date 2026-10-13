using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Jobsy.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddPassportPartners : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PassportPartners",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CompanyId = table.Column<Guid>(type: "uuid", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    DisplayName = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    LogoPng = table.Column<byte[]>(type: "bytea", nullable: true),
                    LogoContentType = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true),
                    LogoUpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    MaxBranches = table.Column<int>(type: "integer", nullable: false, defaultValue: 1),
                    TermsVersion = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true),
                    TermsAcceptedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    TermsAcceptedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PassportPartners", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PassportPartners_Companies_CompanyId",
                        column: x => x.CompanyId,
                        principalTable: "Companies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PassportPartners_Users_TermsAcceptedByUserId",
                        column: x => x.TermsAcceptedByUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "PassportAccessLogs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CandidateUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    PassportPartnerId = table.Column<Guid>(type: "uuid", nullable: true),
                    ViewerUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    ShareLinkId = table.Column<Guid>(type: "uuid", nullable: true),
                    Kind = table.Column<int>(type: "integer", nullable: false),
                    OccurredAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PassportAccessLogs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PassportAccessLogs_PassportPartners_PassportPartnerId",
                        column: x => x.PassportPartnerId,
                        principalTable: "PassportPartners",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_PassportAccessLogs_Users_CandidateUserId",
                        column: x => x.CandidateUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PassportAccessLogs_Users_ViewerUserId",
                        column: x => x.ViewerUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "PassportPartnerCodes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PassportPartnerId = table.Column<Guid>(type: "uuid", nullable: false),
                    BranchCompanyId = table.Column<Guid>(type: "uuid", nullable: false),
                    CodeLookupHash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    CodeDisplay = table.Column<string>(type: "character varying(7)", maxLength: 7, nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    DeactivatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PassportPartnerCodes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PassportPartnerCodes_Companies_BranchCompanyId",
                        column: x => x.BranchCompanyId,
                        principalTable: "Companies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PassportPartnerCodes_PassportPartners_PassportPartnerId",
                        column: x => x.PassportPartnerId,
                        principalTable: "PassportPartners",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PassportPartnerCandidateLinks",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CandidateUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    PassportPartnerId = table.Column<Guid>(type: "uuid", nullable: false),
                    PartnerCodeId = table.Column<Guid>(type: "uuid", nullable: true),
                    Source = table.Column<int>(type: "integer", nullable: false),
                    StartedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ConsentGivenAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ConsentVersion = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true),
                    ContactConsentAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ConsentPromptDismissedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ReconfirmDueAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ReconfirmReminderSentAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    SuspendedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    RevokedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    RevokedReason = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PassportPartnerCandidateLinks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PassportPartnerCandidateLinks_PassportPartnerCodes_PartnerC~",
                        column: x => x.PartnerCodeId,
                        principalTable: "PassportPartnerCodes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_PassportPartnerCandidateLinks_PassportPartners_PassportPart~",
                        column: x => x.PassportPartnerId,
                        principalTable: "PassportPartners",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PassportPartnerCandidateLinks_Users_CandidateUserId",
                        column: x => x.CandidateUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PassportAccessLogs_CandidateUserId",
                table: "PassportAccessLogs",
                column: "CandidateUserId");

            migrationBuilder.CreateIndex(
                name: "IX_PassportAccessLogs_OccurredAtUtc",
                table: "PassportAccessLogs",
                column: "OccurredAtUtc");

            migrationBuilder.CreateIndex(
                name: "IX_PassportAccessLogs_PassportPartnerId",
                table: "PassportAccessLogs",
                column: "PassportPartnerId");

            migrationBuilder.CreateIndex(
                name: "IX_PassportAccessLogs_ViewerUserId",
                table: "PassportAccessLogs",
                column: "ViewerUserId");

            migrationBuilder.CreateIndex(
                name: "IX_PassportPartnerCandidateLinks_CandidateUserId_PassportPartn~",
                table: "PassportPartnerCandidateLinks",
                columns: new[] { "CandidateUserId", "PassportPartnerId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PassportPartnerCandidateLinks_PartnerCodeId",
                table: "PassportPartnerCandidateLinks",
                column: "PartnerCodeId");

            migrationBuilder.CreateIndex(
                name: "IX_PassportPartnerCandidateLinks_PassportPartnerId",
                table: "PassportPartnerCandidateLinks",
                column: "PassportPartnerId");

            migrationBuilder.CreateIndex(
                name: "IX_PassportPartnerCodes_BranchCompanyId",
                table: "PassportPartnerCodes",
                column: "BranchCompanyId");

            migrationBuilder.CreateIndex(
                name: "IX_PassportPartnerCodes_CodeLookupHash",
                table: "PassportPartnerCodes",
                column: "CodeLookupHash",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PassportPartnerCodes_PassportPartnerId",
                table: "PassportPartnerCodes",
                column: "PassportPartnerId");

            migrationBuilder.CreateIndex(
                name: "IX_PassportPartners_CompanyId",
                table: "PassportPartners",
                column: "CompanyId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PassportPartners_TermsAcceptedByUserId",
                table: "PassportPartners",
                column: "TermsAcceptedByUserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PassportAccessLogs");

            migrationBuilder.DropTable(
                name: "PassportPartnerCandidateLinks");

            migrationBuilder.DropTable(
                name: "PassportPartnerCodes");

            migrationBuilder.DropTable(
                name: "PassportPartners");
        }
    }
}
