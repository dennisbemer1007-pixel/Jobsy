using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Jobsy.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddUwvFeaturesAndToolkit : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "UwvFeaturesEnabled",
                table: "PlatformFeatureSettings",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateTable(
                name: "CandidateUwvAdviseurShares",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    TokenHash = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    SectionsJson = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    Language = table.Column<string>(type: "character varying(8)", maxLength: 8, nullable: false),
                    ExpiresAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    RevokedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CandidateUwvAdviseurShares", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CandidateUwvToolkitStates",
                columns: table => new
                {
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    OndernemerAnswersJson = table.Column<string>(type: "text", nullable: true),
                    OndernemerOutcomeKey = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true),
                    OndernemerCompletedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DrijfverenDriverKeysJson = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    DrijfverenCompletedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CandidateUwvToolkitStates", x => x.UserId);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CandidateUwvAdviseurShares_TokenHash",
                table: "CandidateUwvAdviseurShares",
                column: "TokenHash");

            migrationBuilder.CreateIndex(
                name: "IX_CandidateUwvAdviseurShares_UserId_CreatedAtUtc",
                table: "CandidateUwvAdviseurShares",
                columns: new[] { "UserId", "CreatedAtUtc" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CandidateUwvAdviseurShares");

            migrationBuilder.DropTable(
                name: "CandidateUwvToolkitStates");

            migrationBuilder.DropColumn(
                name: "UwvFeaturesEnabled",
                table: "PlatformFeatureSettings");
        }
    }
}
