using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Jobsy.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class VacancyHighlightedUntil : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "HighlightedUntil",
                table: "Vacancies",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Vacancies_IsHighlighted_HighlightedUntil",
                table: "Vacancies",
                columns: new[] { "IsHighlighted", "HighlightedUntil" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Vacancies_IsHighlighted_HighlightedUntil",
                table: "Vacancies");

            migrationBuilder.DropColumn(
                name: "HighlightedUntil",
                table: "Vacancies");
        }
    }
}
