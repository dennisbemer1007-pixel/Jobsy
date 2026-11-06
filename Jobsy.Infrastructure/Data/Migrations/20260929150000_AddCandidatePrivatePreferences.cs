using System;
using Jobsy.Infrastructure.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Jobsy.Infrastructure.Data.Migrations
{
    /// <summary>
    /// Private candidate dislikes (never in PreferencesJson / employer paths).
    /// </summary>
    [DbContext(typeof(JobsyDbContext))]
    [Migration("20260929150000_AddCandidatePrivatePreferences")]
    public partial class AddCandidatePrivatePreferences : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CandidatePrivatePreferences",
                columns: table => new
                {
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    DislikesJson = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false, defaultValue: "[]"),
                    CustomDislikesJson = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false, defaultValue: "[]"),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CandidatePrivatePreferences", x => x.UserId);
                    table.ForeignKey(
                        name: "FK_CandidatePrivatePreferences_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CandidatePrivatePreferences");
        }
    }
}
