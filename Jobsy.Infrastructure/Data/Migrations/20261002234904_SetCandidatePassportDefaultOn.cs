using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Jobsy.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class SetCandidatePassportDefaultOn : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<bool>(
                name: "CandidatePassportEnabled",
                table: "PlatformFeatureSettings",
                type: "boolean",
                nullable: false,
                defaultValue: true,
                oldClrType: typeof(bool),
                oldType: "boolean",
                oldDefaultValue: false);

            // One-shot: existing acceptatie/production rows were created with column default false
            // even though nobody chose OFF. Flip them ON once; later admin OFF choices stick.
            migrationBuilder.Sql(
                """UPDATE "PlatformFeatureSettings" SET "CandidatePassportEnabled" = TRUE;""");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Restore column default only. Do not flip row data back to false — an admin who
            // turned passport OFF (or left it ON after this migration) must keep their choice.
            migrationBuilder.AlterColumn<bool>(
                name: "CandidatePassportEnabled",
                table: "PlatformFeatureSettings",
                type: "boolean",
                nullable: false,
                defaultValue: false,
                oldClrType: typeof(bool),
                oldType: "boolean",
                oldDefaultValue: true);
        }
    }
}
