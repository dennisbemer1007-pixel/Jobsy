using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Jobsy.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddPersonalDataAccessLog : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "Iban",
                table: "SalesManagerProfiles",
                type: "character varying(512)",
                maxLength: 512,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(34)",
                oldMaxLength: 34,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Iban",
                table: "PartnerAffiliateProfiles",
                type: "character varying(512)",
                maxLength: 512,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(34)",
                oldMaxLength: 34,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Iban",
                table: "AmbassadeurProfiles",
                type: "character varying(512)",
                maxLength: 512,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(34)",
                oldMaxLength: 34,
                oldNullable: true);

            migrationBuilder.CreateTable(
                name: "PersonalDataAccessLogs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OccurredAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ActorUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    ActorRole = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    SubjectUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    SubjectCompanyId = table.Column<Guid>(type: "uuid", nullable: true),
                    Resource = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    Action = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    Reason = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    SupportAccessGrantId = table.Column<Guid>(type: "uuid", nullable: true),
                    CorrelationId = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    IpHash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PersonalDataAccessLogs", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PersonalDataAccessLogs_ActorUserId",
                table: "PersonalDataAccessLogs",
                column: "ActorUserId");

            migrationBuilder.CreateIndex(
                name: "IX_PersonalDataAccessLogs_OccurredAt",
                table: "PersonalDataAccessLogs",
                column: "OccurredAt");

            migrationBuilder.CreateIndex(
                name: "IX_PersonalDataAccessLogs_Resource_OccurredAt",
                table: "PersonalDataAccessLogs",
                columns: new[] { "Resource", "OccurredAt" });

            migrationBuilder.CreateIndex(
                name: "IX_PersonalDataAccessLogs_SubjectUserId",
                table: "PersonalDataAccessLogs",
                column: "SubjectUserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PersonalDataAccessLogs");

            migrationBuilder.AlterColumn<string>(
                name: "Iban",
                table: "SalesManagerProfiles",
                type: "character varying(34)",
                maxLength: 34,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(512)",
                oldMaxLength: 512,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Iban",
                table: "PartnerAffiliateProfiles",
                type: "character varying(34)",
                maxLength: 34,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(512)",
                oldMaxLength: 512,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Iban",
                table: "AmbassadeurProfiles",
                type: "character varying(34)",
                maxLength: 34,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(512)",
                oldMaxLength: 512,
                oldNullable: true);
        }
    }
}
