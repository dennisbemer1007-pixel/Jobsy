using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Jobsy.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class VacancyCategories : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CategoryFieldsJson",
                table: "Vacancies",
                type: "character varying(8000)",
                maxLength: 8000,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "CategoryId",
                table: "Vacancies",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "VacancyCategories",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Slug = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Name = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    ColorHex = table.Column<string>(type: "character varying(7)", maxLength: 7, nullable: false),
                    PublishCostTokens = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: false),
                    HighlightAvailable = table.Column<bool>(type: "boolean", nullable: false),
                    HighlightCostTokens = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: false),
                    PushBomAvailable = table.Column<bool>(type: "boolean", nullable: false),
                    PushBomCostTokens = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: true),
                    IsAlwaysFree = table.Column<bool>(type: "boolean", nullable: false),
                    ExtraFieldsJson = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    PlacementKind = table.Column<int>(type: "integer", nullable: false),
                    SortOrder = table.Column<int>(type: "integer", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    ShowInMapFilter = table.Column<bool>(type: "boolean", nullable: false),
                    ShowInLegend = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VacancyCategories", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Vacancies_CategoryId",
                table: "Vacancies",
                column: "CategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_Vacancies_Status_CategoryId",
                table: "Vacancies",
                columns: new[] { "Status", "CategoryId" });

            migrationBuilder.CreateIndex(
                name: "IX_VacancyCategories_IsActive_SortOrder",
                table: "VacancyCategories",
                columns: new[] { "IsActive", "SortOrder" });

            migrationBuilder.CreateIndex(
                name: "IX_VacancyCategories_PlacementKind",
                table: "VacancyCategories",
                column: "PlacementKind");

            migrationBuilder.CreateIndex(
                name: "IX_VacancyCategories_Slug",
                table: "VacancyCategories",
                column: "Slug",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Vacancies_VacancyCategories_CategoryId",
                table: "Vacancies",
                column: "CategoryId",
                principalTable: "VacancyCategories",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Vacancies_VacancyCategories_CategoryId",
                table: "Vacancies");

            migrationBuilder.DropTable(
                name: "VacancyCategories");

            migrationBuilder.DropIndex(
                name: "IX_Vacancies_CategoryId",
                table: "Vacancies");

            migrationBuilder.DropIndex(
                name: "IX_Vacancies_Status_CategoryId",
                table: "Vacancies");

            migrationBuilder.DropColumn(
                name: "CategoryFieldsJson",
                table: "Vacancies");

            migrationBuilder.DropColumn(
                name: "CategoryId",
                table: "Vacancies");
        }
    }
}
