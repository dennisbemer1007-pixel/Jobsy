using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Jobsy.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class SuitableFor65Plus : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "SuitableFor65Plus",
                table: "Vacancies",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateIndex(
                name: "IX_Vacancies_Status_SuitableFor65Plus",
                table: "Vacancies",
                columns: new[] { "Status", "SuitableFor65Plus" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Vacancies_Status_SuitableFor65Plus",
                table: "Vacancies");

            migrationBuilder.DropColumn(
                name: "SuitableFor65Plus",
                table: "Vacancies");
        }
    }
}
