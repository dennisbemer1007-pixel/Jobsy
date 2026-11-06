using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Jobsy.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddCandidateApplyRateLimits : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "CandidateApplyPerDayLimit",
                table: "PlatformFeatureSettings",
                type: "integer",
                nullable: false,
                defaultValue: 100);

            migrationBuilder.AddColumn<int>(
                name: "CandidateApplyPerHourLimit",
                table: "PlatformFeatureSettings",
                type: "integer",
                nullable: false,
                defaultValue: 30);

            migrationBuilder.AddColumn<int>(
                name: "CandidateApplyPerMinuteLimit",
                table: "PlatformFeatureSettings",
                type: "integer",
                nullable: false,
                defaultValue: 5);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CandidateApplyPerDayLimit",
                table: "PlatformFeatureSettings");

            migrationBuilder.DropColumn(
                name: "CandidateApplyPerHourLimit",
                table: "PlatformFeatureSettings");

            migrationBuilder.DropColumn(
                name: "CandidateApplyPerMinuteLimit",
                table: "PlatformFeatureSettings");
        }
    }
}
