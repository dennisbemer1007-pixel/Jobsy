using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Jobsy.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddCompanyAccessRequests : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "Kind",
                table: "EstablishmentTakeoverRequests",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "LetterCodeHash",
                table: "EstablishmentTakeoverRequests",
                type: "character varying(128)",
                maxLength: 128,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "LetterExpiresAtUtc",
                table: "EstablishmentTakeoverRequests",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "LetterFailedAttempts",
                table: "EstablishmentTakeoverRequests",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTime>(
                name: "LetterSentAtUtc",
                table: "EstablishmentTakeoverRequests",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "LetterVerifiedAtUtc",
                table: "EstablishmentTakeoverRequests",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ManagersNotifiedAtUtc",
                table: "EstablishmentTakeoverRequests",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "CompanyAccessRequests",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TargetCompanyId = table.Column<Guid>(type: "uuid", nullable: false),
                    KvkNumber = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    RequestedVestigingIdsJson = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: false),
                    RequestedRole = table.Column<int>(type: "integer", nullable: false),
                    RequesterName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    RequesterFunction = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    RequesterEmail = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    RequesterPhone = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: true),
                    Message = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    EmailConfirmationCodeHash = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    EmailConfirmationExpiresAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    EmailConfirmationFailedAttempts = table.Column<int>(type: "integer", nullable: false),
                    EmailConfirmedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    RequesterToken = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    DecidedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    DecidedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DecisionReason = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    GrantedRole = table.Column<int>(type: "integer", nullable: true),
                    GrantedVestigingIdsJson = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
                    ReminderSentAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    EscalatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CompanyAccessRequests", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CompanyAccessRequests_Companies_TargetCompanyId",
                        column: x => x.TargetCompanyId,
                        principalTable: "Companies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CompanyAccessRequests_Users_DecidedByUserId",
                        column: x => x.DecidedByUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "IX_EstablishmentTakeoverRequests_Kind",
                table: "EstablishmentTakeoverRequests",
                column: "Kind");

            migrationBuilder.CreateIndex(
                name: "IX_CompanyAccessRequests_CreatedAtUtc",
                table: "CompanyAccessRequests",
                column: "CreatedAtUtc");

            migrationBuilder.CreateIndex(
                name: "IX_CompanyAccessRequests_DecidedByUserId",
                table: "CompanyAccessRequests",
                column: "DecidedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_CompanyAccessRequests_RequesterEmail",
                table: "CompanyAccessRequests",
                column: "RequesterEmail");

            migrationBuilder.CreateIndex(
                name: "IX_CompanyAccessRequests_Status",
                table: "CompanyAccessRequests",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_CompanyAccessRequests_TargetCompanyId_RequesterEmail_Status",
                table: "CompanyAccessRequests",
                columns: new[] { "TargetCompanyId", "RequesterEmail", "Status" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CompanyAccessRequests");

            migrationBuilder.DropIndex(
                name: "IX_EstablishmentTakeoverRequests_Kind",
                table: "EstablishmentTakeoverRequests");

            migrationBuilder.DropColumn(
                name: "Kind",
                table: "EstablishmentTakeoverRequests");

            migrationBuilder.DropColumn(
                name: "LetterCodeHash",
                table: "EstablishmentTakeoverRequests");

            migrationBuilder.DropColumn(
                name: "LetterExpiresAtUtc",
                table: "EstablishmentTakeoverRequests");

            migrationBuilder.DropColumn(
                name: "LetterFailedAttempts",
                table: "EstablishmentTakeoverRequests");

            migrationBuilder.DropColumn(
                name: "LetterSentAtUtc",
                table: "EstablishmentTakeoverRequests");

            migrationBuilder.DropColumn(
                name: "LetterVerifiedAtUtc",
                table: "EstablishmentTakeoverRequests");

            migrationBuilder.DropColumn(
                name: "ManagersNotifiedAtUtc",
                table: "EstablishmentTakeoverRequests");
        }
    }
}
