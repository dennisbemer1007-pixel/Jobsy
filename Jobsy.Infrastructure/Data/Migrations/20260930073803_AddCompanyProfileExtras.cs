using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Jobsy.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddCompanyProfileExtras : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Source",
                table: "CompanyCultureProfiles",
                type: "character varying(16)",
                maxLength: 16,
                nullable: false,
                defaultValue: "Full");

            migrationBuilder.AddColumn<string>(
                name: "WorkTypeLabels",
                table: "Companies",
                type: "character varying(512)",
                maxLength: 512,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "CompanyValuesProfiles",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CompanyId = table.Column<Guid>(type: "uuid", nullable: false),
                    CardIdsJson = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    AutonomyPercent = table.Column<int>(type: "integer", nullable: false),
                    ConnectionPercent = table.Column<int>(type: "integer", nullable: false),
                    AchievementPercent = table.Column<int>(type: "integer", nullable: false),
                    StabilityPercent = table.Column<int>(type: "integer", nullable: false),
                    ImpactPercent = table.Column<int>(type: "integer", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CompanyValuesProfiles", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CompanyValuesProfiles_Companies_CompanyId",
                        column: x => x.CompanyId,
                        principalTable: "Companies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CompanyValuesProfiles_CompanyId",
                table: "CompanyValuesProfiles",
                column: "CompanyId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CompanyValuesProfiles");

            migrationBuilder.DropColumn(
                name: "Source",
                table: "CompanyCultureProfiles");

            migrationBuilder.DropColumn(
                name: "WorkTypeLabels",
                table: "Companies");
        }
    }
}
