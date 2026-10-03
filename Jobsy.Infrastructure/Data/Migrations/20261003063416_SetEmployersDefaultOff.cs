using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Jobsy.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class SetEmployersDefaultOff : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<bool>(
                name: "EmployersEnabled",
                table: "PlatformFeatureSettings",
                type: "boolean",
                nullable: false,
                defaultValue: false,
                oldClrType: typeof(bool),
                oldType: "boolean",
                oldDefaultValue: true);

            // One-shot: decision 20 (03-10-2026). Existing rows were created with the column
            // default true. Turn the employer part OFF until phase 2. Later admin ON choices stick.
            // No other data is deleted or modified.
            migrationBuilder.Sql(
                """UPDATE "PlatformFeatureSettings" SET "EmployersEnabled" = FALSE;""");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Restore the column default only. Do not flip row data back to true.
            migrationBuilder.AlterColumn<bool>(
                name: "EmployersEnabled",
                table: "PlatformFeatureSettings",
                type: "boolean",
                nullable: false,
                defaultValue: true,
                oldClrType: typeof(bool),
                oldType: "boolean",
                oldDefaultValue: false);
        }
    }
}
