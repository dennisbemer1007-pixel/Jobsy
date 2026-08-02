using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Jobsy.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class RegistrationPasswordAndSbiRole : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsIntermediarySbi",
                table: "CompanyRegistrations",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "PasswordHash",
                table: "CompanyRegistrations",
                type: "character varying(512)",
                maxLength: 512,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PrimarySbiCode",
                table: "CompanyRegistrations",
                type: "character varying(16)",
                maxLength: 16,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsIntermediarySbi",
                table: "CompanyRegistrations");

            migrationBuilder.DropColumn(
                name: "PasswordHash",
                table: "CompanyRegistrations");

            migrationBuilder.DropColumn(
                name: "PrimarySbiCode",
                table: "CompanyRegistrations");
        }
    }
}
