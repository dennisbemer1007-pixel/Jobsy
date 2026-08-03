using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Jobsy.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AmbassadeurRoleAndCommission : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ReferredByAmbassadeurTrackingCode",
                table: "Users",
                type: "character varying(32)",
                maxLength: 32,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "ReferredByAmbassadeurUserId",
                table: "Users",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "CommissionAmbassadeurRateSnapshot",
                table: "Companies",
                type: "numeric(5,4)",
                precision: 5,
                scale: 4,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "ReferredByAmbassadeurUserId",
                table: "Companies",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "AmbassadeurProfiles",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    CompanyName = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    KvkNumber = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    VatNumber = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true),
                    Address = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    PostalCode = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: true),
                    City = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    Country = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    Iban = table.Column<string>(type: "character varying(34)", maxLength: 34, nullable: true),
                    TrackingCode = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true),
                    BaseCommissionPercentage = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: false),
                    CommissionPercentageOverride = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: true),
                    AgreementSignedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    AgreementVersion = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    OnboardingCompletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AmbassadeurProfiles", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AmbassadeurProfiles_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AmbassadeurSettings",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CandidateThreshold = table.Column<int>(type: "integer", nullable: false),
                    PercentPerThreshold = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: false),
                    MaxCommissionPercentage = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AmbassadeurSettings", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Users_ReferredByAmbassadeurUserId",
                table: "Users",
                column: "ReferredByAmbassadeurUserId");

            migrationBuilder.CreateIndex(
                name: "IX_Companies_ReferredByAmbassadeurUserId",
                table: "Companies",
                column: "ReferredByAmbassadeurUserId");

            migrationBuilder.CreateIndex(
                name: "IX_AmbassadeurProfiles_TrackingCode",
                table: "AmbassadeurProfiles",
                column: "TrackingCode",
                unique: true,
                filter: "\"TrackingCode\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_AmbassadeurProfiles_UserId",
                table: "AmbassadeurProfiles",
                column: "UserId",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Companies_Users_ReferredByAmbassadeurUserId",
                table: "Companies",
                column: "ReferredByAmbassadeurUserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_Users_Users_ReferredByAmbassadeurUserId",
                table: "Users",
                column: "ReferredByAmbassadeurUserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Companies_Users_ReferredByAmbassadeurUserId",
                table: "Companies");

            migrationBuilder.DropForeignKey(
                name: "FK_Users_Users_ReferredByAmbassadeurUserId",
                table: "Users");

            migrationBuilder.DropTable(
                name: "AmbassadeurProfiles");

            migrationBuilder.DropTable(
                name: "AmbassadeurSettings");

            migrationBuilder.DropIndex(
                name: "IX_Users_ReferredByAmbassadeurUserId",
                table: "Users");

            migrationBuilder.DropIndex(
                name: "IX_Companies_ReferredByAmbassadeurUserId",
                table: "Companies");

            migrationBuilder.DropColumn(
                name: "ReferredByAmbassadeurTrackingCode",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "ReferredByAmbassadeurUserId",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "CommissionAmbassadeurRateSnapshot",
                table: "Companies");

            migrationBuilder.DropColumn(
                name: "ReferredByAmbassadeurUserId",
                table: "Companies");
        }
    }
}
