using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Jobsy.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddContextualTips : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "ContextualTipsEnabled",
                table: "PlatformFeatureSettings",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateTable(
                name: "CandidateContextualTipsSettings",
                columns: table => new
                {
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    MomentTipsEnabled = table.Column<bool>(type: "boolean", nullable: false),
                    InterviewPrepReminderEnabled = table.Column<bool>(type: "boolean", nullable: false),
                    HiddenTipIdsJson = table.Column<string>(type: "character varying(8000)", maxLength: 8000, nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CandidateContextualTipsSettings", x => x.UserId);
                    table.ForeignKey(
                        name: "FK_CandidateContextualTipsSettings_Users_UserId",
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
                name: "CandidateContextualTipsSettings");

            migrationBuilder.DropColumn(
                name: "ContextualTipsEnabled",
                table: "PlatformFeatureSettings");
        }
    }
}
