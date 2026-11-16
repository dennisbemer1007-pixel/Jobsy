using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Jobsy.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddWerkgeverLoopbaanApk : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "EmployerLoopbaanApkEnabled",
                table: "PlatformFeatureSettings",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "LoopbaanApkEnabled",
                table: "Companies",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateTable(
                name: "WerkgeverApkTeams",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CompanyId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WerkgeverApkTeams", x => x.Id);
                    table.ForeignKey(
                        name: "FK_WerkgeverApkTeams_Companies_CompanyId",
                        column: x => x.CompanyId,
                        principalTable: "Companies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "WerkgeverApkEmployeeCodes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TeamId = table.Column<Guid>(type: "uuid", nullable: false),
                    Number = table.Column<int>(type: "integer", nullable: false),
                    Label = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: true),
                    CodeLookupHash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    CodeProtected = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    SessionVersion = table.Column<int>(type: "integer", nullable: false),
                    SharedWithManager = table.Column<bool>(type: "boolean", nullable: false),
                    SharedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    RevokedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LinkedUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    LastSeenAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WerkgeverApkEmployeeCodes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_WerkgeverApkEmployeeCodes_WerkgeverApkTeams_TeamId",
                        column: x => x.TeamId,
                        principalTable: "WerkgeverApkTeams",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "WerkgeverApkEmployeeProgress",
                columns: table => new
                {
                    EmployeeCodeId = table.Column<Guid>(type: "uuid", nullable: false),
                    ProgressJson = table.Column<string>(type: "text", nullable: false),
                    WelcomeSeen = table.Column<bool>(type: "boolean", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WerkgeverApkEmployeeProgress", x => x.EmployeeCodeId);
                    table.ForeignKey(
                        name: "FK_WerkgeverApkEmployeeProgress_WerkgeverApkEmployeeCodes_Empl~",
                        column: x => x.EmployeeCodeId,
                        principalTable: "WerkgeverApkEmployeeCodes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "WerkgeverApkManagerSheets",
                columns: table => new
                {
                    EmployeeCodeId = table.Column<Guid>(type: "uuid", nullable: false),
                    AnswersJson = table.Column<string>(type: "text", nullable: false),
                    Completed = table.Column<bool>(type: "boolean", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WerkgeverApkManagerSheets", x => x.EmployeeCodeId);
                    table.ForeignKey(
                        name: "FK_WerkgeverApkManagerSheets_WerkgeverApkEmployeeCodes_Employe~",
                        column: x => x.EmployeeCodeId,
                        principalTable: "WerkgeverApkEmployeeCodes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_WerkgeverApkEmployeeCodes_TeamId_CodeLookupHash",
                table: "WerkgeverApkEmployeeCodes",
                columns: new[] { "TeamId", "CodeLookupHash" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_WerkgeverApkTeams_CompanyId_Name",
                table: "WerkgeverApkTeams",
                columns: new[] { "CompanyId", "Name" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "WerkgeverApkEmployeeProgress");

            migrationBuilder.DropTable(
                name: "WerkgeverApkManagerSheets");

            migrationBuilder.DropTable(
                name: "WerkgeverApkEmployeeCodes");

            migrationBuilder.DropTable(
                name: "WerkgeverApkTeams");

            migrationBuilder.DropColumn(
                name: "EmployerLoopbaanApkEnabled",
                table: "PlatformFeatureSettings");

            migrationBuilder.DropColumn(
                name: "LoopbaanApkEnabled",
                table: "Companies");
        }
    }
}
