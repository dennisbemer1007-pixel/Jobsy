using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Jobsy.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddCompanyVerificationLetters : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CompanyManualVerificationRequests",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CompanyId = table.Column<Guid>(type: "uuid", nullable: false),
                    RequestedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Reason = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    Message = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
                    AttachmentIdsJson = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    DecidedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DecidedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    DecisionStatus = table.Column<int>(type: "integer", nullable: true),
                    DecisionNote = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CompanyManualVerificationRequests", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CompanyManualVerificationRequests_Companies_CompanyId",
                        column: x => x.CompanyId,
                        principalTable: "Companies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CompanyManualVerificationRequests_Users_DecidedByUserId",
                        column: x => x.DecidedByUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_CompanyManualVerificationRequests_Users_RequestedByUserId",
                        column: x => x.RequestedByUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "CompanyVerificationDecisions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CompanyId = table.Column<Guid>(type: "uuid", nullable: false),
                    KvkNumber = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    AdminUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Outcome = table.Column<int>(type: "integer", nullable: false),
                    Method = table.Column<int>(type: "integer", nullable: false),
                    Reason = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CompanyVerificationDecisions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CompanyVerificationDecisions_Companies_CompanyId",
                        column: x => x.CompanyId,
                        principalTable: "Companies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CompanyVerificationDecisions_Users_AdminUserId",
                        column: x => x.AdminUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "CompanyVerificationEmailChallenges",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CompanyId = table.Column<Guid>(type: "uuid", nullable: false),
                    RequestedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Email = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    CodeHash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    ExpiresAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    FailedAttempts = table.Column<int>(type: "integer", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ConsumedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LockedUntilUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CompanyVerificationEmailChallenges", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CompanyVerificationEmailChallenges_Companies_CompanyId",
                        column: x => x.CompanyId,
                        principalTable: "Companies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CompanyVerificationEmailChallenges_Users_RequestedByUserId",
                        column: x => x.RequestedByUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "CompanyVerificationLetters",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CompanyId = table.Column<Guid>(type: "uuid", nullable: false),
                    KvkNumber = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    RequestedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    CodeHash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    ExpiresAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    FailedAttempts = table.Column<int>(type: "integer", nullable: false),
                    ResendCount = table.Column<int>(type: "integer", nullable: false),
                    ResendOfLetterId = table.Column<Guid>(type: "uuid", nullable: true),
                    AddressLine1 = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    AddressLine2 = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    PostalCode = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    City = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    Country = table.Column<string>(type: "character varying(2)", maxLength: 2, nullable: false),
                    Provider = table.Column<int>(type: "integer", nullable: false),
                    ProviderLetterId = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    SentAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeliveredAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    UsedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    BlockedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CompanyVerificationLetters", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CompanyVerificationLetters_Companies_CompanyId",
                        column: x => x.CompanyId,
                        principalTable: "Companies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CompanyVerificationLetters_CompanyVerificationLetters_Resen~",
                        column: x => x.ResendOfLetterId,
                        principalTable: "CompanyVerificationLetters",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_CompanyVerificationLetters_Users_RequestedByUserId",
                        column: x => x.RequestedByUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CompanyManualVerificationRequests_CompanyId",
                table: "CompanyManualVerificationRequests",
                column: "CompanyId");

            migrationBuilder.CreateIndex(
                name: "IX_CompanyManualVerificationRequests_DecidedAtUtc",
                table: "CompanyManualVerificationRequests",
                column: "DecidedAtUtc");

            migrationBuilder.CreateIndex(
                name: "IX_CompanyManualVerificationRequests_DecidedByUserId",
                table: "CompanyManualVerificationRequests",
                column: "DecidedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_CompanyManualVerificationRequests_RequestedByUserId",
                table: "CompanyManualVerificationRequests",
                column: "RequestedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_CompanyVerificationDecisions_AdminUserId",
                table: "CompanyVerificationDecisions",
                column: "AdminUserId");

            migrationBuilder.CreateIndex(
                name: "IX_CompanyVerificationDecisions_CompanyId",
                table: "CompanyVerificationDecisions",
                column: "CompanyId");

            migrationBuilder.CreateIndex(
                name: "IX_CompanyVerificationDecisions_CreatedAtUtc",
                table: "CompanyVerificationDecisions",
                column: "CreatedAtUtc");

            migrationBuilder.CreateIndex(
                name: "IX_CompanyVerificationDecisions_KvkNumber",
                table: "CompanyVerificationDecisions",
                column: "KvkNumber");

            migrationBuilder.CreateIndex(
                name: "IX_CompanyVerificationEmailChallenges_CompanyId",
                table: "CompanyVerificationEmailChallenges",
                column: "CompanyId");

            migrationBuilder.CreateIndex(
                name: "IX_CompanyVerificationEmailChallenges_CreatedAtUtc",
                table: "CompanyVerificationEmailChallenges",
                column: "CreatedAtUtc");

            migrationBuilder.CreateIndex(
                name: "IX_CompanyVerificationEmailChallenges_RequestedByUserId",
                table: "CompanyVerificationEmailChallenges",
                column: "RequestedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_CompanyVerificationLetters_CompanyId",
                table: "CompanyVerificationLetters",
                column: "CompanyId");

            migrationBuilder.CreateIndex(
                name: "IX_CompanyVerificationLetters_CreatedAtUtc",
                table: "CompanyVerificationLetters",
                column: "CreatedAtUtc");

            migrationBuilder.CreateIndex(
                name: "IX_CompanyVerificationLetters_KvkNumber",
                table: "CompanyVerificationLetters",
                column: "KvkNumber");

            migrationBuilder.CreateIndex(
                name: "IX_CompanyVerificationLetters_RequestedByUserId",
                table: "CompanyVerificationLetters",
                column: "RequestedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_CompanyVerificationLetters_ResendOfLetterId",
                table: "CompanyVerificationLetters",
                column: "ResendOfLetterId");

            migrationBuilder.CreateIndex(
                name: "IX_CompanyVerificationLetters_Status",
                table: "CompanyVerificationLetters",
                column: "Status");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CompanyManualVerificationRequests");

            migrationBuilder.DropTable(
                name: "CompanyVerificationDecisions");

            migrationBuilder.DropTable(
                name: "CompanyVerificationEmailChallenges");

            migrationBuilder.DropTable(
                name: "CompanyVerificationLetters");
        }
    }
}
