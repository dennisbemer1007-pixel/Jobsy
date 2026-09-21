using Jobsy.Infrastructure.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Jobsy.Infrastructure.Data.Migrations
{
    [DbContext(typeof(JobsyDbContext))]
    [Migration("20260921140000_AddCandidateWhoAmIProfiles")]
    public class AddCandidateWhoAmIProfiles : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "SnapshotWhoAmIJson",
                table: "Applications",
                type: "text",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "CandidateWhoAmIProfiles",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    IncludeOnCv = table.Column<bool>(type: "boolean", nullable: false),
                    StoryText = table.Column<string>(type: "text", nullable: false),
                    KeywordsJson = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    InputFingerprint = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    FromOpenAi = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    StoryGeneratedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CandidateWhoAmIProfiles", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CandidateWhoAmIProfiles_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CandidateWhoAmIProfiles_UserId",
                table: "CandidateWhoAmIProfiles",
                column: "UserId",
                unique: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "CandidateWhoAmIProfiles");
            migrationBuilder.DropColumn(name: "SnapshotWhoAmIJson", table: "Applications");
        }
    }
}
