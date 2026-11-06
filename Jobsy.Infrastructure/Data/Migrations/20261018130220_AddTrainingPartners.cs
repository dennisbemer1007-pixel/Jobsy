using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Jobsy.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddTrainingPartners : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "FeedApiSecretName",
                table: "TrainingProviders",
                type: "character varying(120)",
                maxLength: 120,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "FeedKind",
                table: "TrainingProviders",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "FeedMappingJson",
                table: "TrainingProviders",
                type: "character varying(4000)",
                maxLength: 4000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "FeedUrl",
                table: "TrainingProviders",
                type: "character varying(1024)",
                maxLength: 1024,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ImportIntervalHours",
                table: "TrainingProviders",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<bool>(
                name: "IsDemoPartner",
                table: "TrainingProviders",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "LastImportedAtUtc",
                table: "TrainingProviders",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CareerStepsCsv",
                table: "TrainingOffers",
                type: "character varying(400)",
                maxLength: 400,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "CompetenciesCsv",
                table: "TrainingOffers",
                type: "character varying(200)",
                maxLength: 200,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<DateTime>(
                name: "ImportedAtUtc",
                table: "TrainingOffers",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LearnBulletsJson",
                table: "TrainingOffers",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PartnerExternalId",
                table: "TrainingOffers",
                type: "character varying(120)",
                maxLength: 120,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "PriceEuro",
                table: "TrainingOffers",
                type: "numeric(10,2)",
                precision: 10,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PriceLabel",
                table: "TrainingOffers",
                type: "character varying(80)",
                maxLength: 80,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ReasonEn",
                table: "TrainingOffers",
                type: "character varying(400)",
                maxLength: 400,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ReasonNl",
                table: "TrainingOffers",
                type: "character varying(400)",
                maxLength: 400,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Section",
                table: "TrainingOffers",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateOnly>(
                name: "StartsOn",
                table: "TrainingOffers",
                type: "date",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "TrainingPartnersEnabled",
                table: "PlatformFeatureSettings",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateTable(
                name: "TrainingEnrollments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    OfferId = table.Column<Guid>(type: "uuid", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    ProgressCurrent = table.Column<int>(type: "integer", nullable: false),
                    ProgressTotal = table.Column<int>(type: "integer", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CompletedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    VerifiedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    VerificationSource = table.Column<int>(type: "integer", nullable: true),
                    CertificateTitle = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TrainingEnrollments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TrainingEnrollments_TrainingOffers_OfferId",
                        column: x => x.OfferId,
                        principalTable: "TrainingOffers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_TrainingEnrollments_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_TrainingOffers_ProviderId_PartnerExternalId",
                table: "TrainingOffers",
                columns: new[] { "ProviderId", "PartnerExternalId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TrainingEnrollments_OfferId",
                table: "TrainingEnrollments",
                column: "OfferId");

            migrationBuilder.CreateIndex(
                name: "IX_TrainingEnrollments_UserId_OfferId",
                table: "TrainingEnrollments",
                columns: new[] { "UserId", "OfferId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TrainingEnrollments");

            migrationBuilder.DropIndex(
                name: "IX_TrainingOffers_ProviderId_PartnerExternalId",
                table: "TrainingOffers");

            migrationBuilder.DropColumn(
                name: "FeedApiSecretName",
                table: "TrainingProviders");

            migrationBuilder.DropColumn(
                name: "FeedKind",
                table: "TrainingProviders");

            migrationBuilder.DropColumn(
                name: "FeedMappingJson",
                table: "TrainingProviders");

            migrationBuilder.DropColumn(
                name: "FeedUrl",
                table: "TrainingProviders");

            migrationBuilder.DropColumn(
                name: "ImportIntervalHours",
                table: "TrainingProviders");

            migrationBuilder.DropColumn(
                name: "IsDemoPartner",
                table: "TrainingProviders");

            migrationBuilder.DropColumn(
                name: "LastImportedAtUtc",
                table: "TrainingProviders");

            migrationBuilder.DropColumn(
                name: "CareerStepsCsv",
                table: "TrainingOffers");

            migrationBuilder.DropColumn(
                name: "CompetenciesCsv",
                table: "TrainingOffers");

            migrationBuilder.DropColumn(
                name: "ImportedAtUtc",
                table: "TrainingOffers");

            migrationBuilder.DropColumn(
                name: "LearnBulletsJson",
                table: "TrainingOffers");

            migrationBuilder.DropColumn(
                name: "PartnerExternalId",
                table: "TrainingOffers");

            migrationBuilder.DropColumn(
                name: "PriceEuro",
                table: "TrainingOffers");

            migrationBuilder.DropColumn(
                name: "PriceLabel",
                table: "TrainingOffers");

            migrationBuilder.DropColumn(
                name: "ReasonEn",
                table: "TrainingOffers");

            migrationBuilder.DropColumn(
                name: "ReasonNl",
                table: "TrainingOffers");

            migrationBuilder.DropColumn(
                name: "Section",
                table: "TrainingOffers");

            migrationBuilder.DropColumn(
                name: "StartsOn",
                table: "TrainingOffers");

            migrationBuilder.DropColumn(
                name: "TrainingPartnersEnabled",
                table: "PlatformFeatureSettings");
        }
    }
}
