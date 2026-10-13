using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Jobsy.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddMfaTrustedDevices : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "MfaTrustedDevices",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    TokenHash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ExpiresAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    LastUsedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    UserAgentSummary = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    RevokedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    SessionVersionAtCreate = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MfaTrustedDevices", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MfaTrustedDevices_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_MfaTrustedDevices_TokenHash",
                table: "MfaTrustedDevices",
                column: "TokenHash",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_MfaTrustedDevices_UserId_RevokedAtUtc",
                table: "MfaTrustedDevices",
                columns: new[] { "UserId", "RevokedAtUtc" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "MfaTrustedDevices");
        }
    }
}
