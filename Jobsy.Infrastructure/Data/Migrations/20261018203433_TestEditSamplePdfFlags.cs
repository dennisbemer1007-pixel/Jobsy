using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Jobsy.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class TestEditSamplePdfFlags : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "TestEditModeEnabled",
                table: "PlatformFeatureSettings",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "SamplePdfEnabled",
                table: "PlatformFeatureSettings",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AlterColumn<bool>(
                name: "OccupationDayInLifeEnabled",
                table: "PlatformFeatureSettings",
                type: "boolean",
                nullable: false,
                oldClrType: typeof(bool),
                oldType: "boolean",
                oldDefaultValue: true);

            migrationBuilder.AlterColumn<string>(
                name: "AdminFlagOverridesJson",
                table: "PlatformFeatureSettings",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text",
                oldDefaultValue: "[]");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<bool>(
                name: "OccupationDayInLifeEnabled",
                table: "PlatformFeatureSettings",
                type: "boolean",
                nullable: false,
                defaultValue: true,
                oldClrType: typeof(bool),
                oldType: "boolean");

            migrationBuilder.AlterColumn<string>(
                name: "AdminFlagOverridesJson",
                table: "PlatformFeatureSettings",
                type: "text",
                nullable: false,
                defaultValue: "[]",
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.DropColumn(
                name: "SamplePdfEnabled",
                table: "PlatformFeatureSettings");

            migrationBuilder.DropColumn(
                name: "TestEditModeEnabled",
                table: "PlatformFeatureSettings");
        }
    }
}
