using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Jobsy.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddAtsScrapePipeline : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AtsScrapeSources",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    Domain = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    ListUrl = table.Column<string>(type: "character varying(2048)", maxLength: 2048, nullable: false),
                    IsEnabled = table.Column<bool>(type: "boolean", nullable: false),
                    DefaultLatitude = table.Column<double>(type: "double precision", nullable: false),
                    DefaultLongitude = table.Column<double>(type: "double precision", nullable: false),
                    DefaultLocationLabel = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    PreferredCompanyId = table.Column<Guid>(type: "uuid", nullable: true),
                    LastScrapedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AtsScrapeSources", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AtsScrapeSources_Companies_PreferredCompanyId",
                        column: x => x.PreferredCompanyId,
                        principalTable: "Companies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "AtsScrapedListings",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SourceId = table.Column<Guid>(type: "uuid", nullable: false),
                    DedupHash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    SourceUrl = table.Column<string>(type: "character varying(2048)", maxLength: 2048, nullable: false),
                    CompanyName = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    Title = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    LocationLabel = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    PostalCode = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: true),
                    Description = table.Column<string>(type: "character varying(20000)", maxLength: 20000, nullable: false),
                    SalaryText = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    HourlyWage = table.Column<decimal>(type: "numeric(8,2)", precision: 8, scale: 2, nullable: true),
                    HoursText = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    MinHoursPerWeek = table.Column<decimal>(type: "numeric(5,1)", precision: 5, scale: 1, nullable: true),
                    MaxHoursPerWeek = table.Column<decimal>(type: "numeric(5,1)", precision: 5, scale: 1, nullable: true),
                    TagsJson = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    ImageUrl = table.Column<string>(type: "character varying(600000)", maxLength: 600000, nullable: true),
                    Latitude = table.Column<double>(type: "double precision", nullable: true),
                    Longitude = table.Column<double>(type: "double precision", nullable: true),
                    CompletenessScore = table.Column<int>(type: "integer", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    RejectReason = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    ScrapedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    LastCheckedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ExpiresAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ReviewedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LinkedVacancyId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AtsScrapedListings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AtsScrapedListings_AtsScrapeSources_SourceId",
                        column: x => x.SourceId,
                        principalTable: "AtsScrapeSources",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AtsScrapedListings_Vacancies_LinkedVacancyId",
                        column: x => x.LinkedVacancyId,
                        principalTable: "Vacancies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AtsScrapedListings_DedupHash",
                table: "AtsScrapedListings",
                column: "DedupHash",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AtsScrapedListings_LastCheckedAtUtc",
                table: "AtsScrapedListings",
                column: "LastCheckedAtUtc");

            migrationBuilder.CreateIndex(
                name: "IX_AtsScrapedListings_LinkedVacancyId",
                table: "AtsScrapedListings",
                column: "LinkedVacancyId");

            migrationBuilder.CreateIndex(
                name: "IX_AtsScrapedListings_SourceId",
                table: "AtsScrapedListings",
                column: "SourceId");

            migrationBuilder.CreateIndex(
                name: "IX_AtsScrapedListings_Status_ScrapedAtUtc",
                table: "AtsScrapedListings",
                columns: new[] { "Status", "ScrapedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_AtsScrapeSources_Domain",
                table: "AtsScrapeSources",
                column: "Domain",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AtsScrapeSources_IsEnabled_LastScrapedAtUtc",
                table: "AtsScrapeSources",
                columns: new[] { "IsEnabled", "LastScrapedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_AtsScrapeSources_PreferredCompanyId",
                table: "AtsScrapeSources",
                column: "PreferredCompanyId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AtsScrapedListings");

            migrationBuilder.DropTable(
                name: "AtsScrapeSources");
        }
    }
}
