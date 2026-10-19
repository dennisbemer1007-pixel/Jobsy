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
            migrationBuilder.Sql(
                """
                ALTER TABLE "PlatformFeatureSettings" ADD COLUMN IF NOT EXISTS "TestEditModeEnabled" boolean NOT NULL DEFAULT false;
                ALTER TABLE "PlatformFeatureSettings" ADD COLUMN IF NOT EXISTS "SamplePdfEnabled" boolean NOT NULL DEFAULT false;
                """);

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
