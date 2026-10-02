using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Jobsy.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddTestAccountFlags : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsTestData",
                table: "Vacancies",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsTestAccount",
                table: "Users",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsTestData",
                table: "Schools",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsTestData",
                table: "SchoolClasses",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsTestData",
                table: "Companies",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateIndex(
                name: "IX_Vacancies_IsTestData",
                table: "Vacancies",
                column: "IsTestData");

            migrationBuilder.CreateIndex(
                name: "IX_Users_IsTestAccount",
                table: "Users",
                column: "IsTestAccount");

            migrationBuilder.CreateIndex(
                name: "IX_Schools_IsTestData",
                table: "Schools",
                column: "IsTestData");

            migrationBuilder.CreateIndex(
                name: "IX_SchoolClasses_IsTestData",
                table: "SchoolClasses",
                column: "IsTestData");

            migrationBuilder.CreateIndex(
                name: "IX_Companies_IsTestData",
                table: "Companies",
                column: "IsTestData");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Vacancies_IsTestData",
                table: "Vacancies");

            migrationBuilder.DropIndex(
                name: "IX_Users_IsTestAccount",
                table: "Users");

            migrationBuilder.DropIndex(
                name: "IX_Schools_IsTestData",
                table: "Schools");

            migrationBuilder.DropIndex(
                name: "IX_SchoolClasses_IsTestData",
                table: "SchoolClasses");

            migrationBuilder.DropIndex(
                name: "IX_Companies_IsTestData",
                table: "Companies");

            migrationBuilder.DropColumn(
                name: "IsTestData",
                table: "Vacancies");

            migrationBuilder.DropColumn(
                name: "IsTestAccount",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "IsTestData",
                table: "Schools");

            migrationBuilder.DropColumn(
                name: "IsTestData",
                table: "SchoolClasses");

            migrationBuilder.DropColumn(
                name: "IsTestData",
                table: "Companies");
        }
    }
}
