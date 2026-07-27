using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Jobsy.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddSearchImpressionsAndSiteVisits : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "SiteVisits",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: true),
                    AnonymousKey = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    Path = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SiteVisits", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SiteVisits_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "VacancySearchImpressions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    VacancyId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: true),
                    AnonymousKey = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VacancySearchImpressions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_VacancySearchImpressions_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_VacancySearchImpressions_Vacancies_VacancyId",
                        column: x => x.VacancyId,
                        principalTable: "Vacancies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SiteVisits_AnonymousKey",
                table: "SiteVisits",
                column: "AnonymousKey");

            migrationBuilder.CreateIndex(
                name: "IX_SiteVisits_CreatedAt",
                table: "SiteVisits",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_SiteVisits_UserId",
                table: "SiteVisits",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_VacancySearchImpressions_CreatedAt",
                table: "VacancySearchImpressions",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_VacancySearchImpressions_UserId",
                table: "VacancySearchImpressions",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_VacancySearchImpressions_VacancyId",
                table: "VacancySearchImpressions",
                column: "VacancyId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SiteVisits");

            migrationBuilder.DropTable(
                name: "VacancySearchImpressions");
        }
    }
}
