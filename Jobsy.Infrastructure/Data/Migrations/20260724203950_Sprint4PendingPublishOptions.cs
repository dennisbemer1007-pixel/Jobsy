using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Jobsy.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class Sprint4PendingPublishOptions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "RequestedExtend",
                table: "Vacancies",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "RequestedHighlight",
                table: "Vacancies",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "RequestedPushBom",
                table: "Vacancies",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "RequestedExtend",
                table: "Vacancies");

            migrationBuilder.DropColumn(
                name: "RequestedHighlight",
                table: "Vacancies");

            migrationBuilder.DropColumn(
                name: "RequestedPushBom",
                table: "Vacancies");
        }
    }
}
