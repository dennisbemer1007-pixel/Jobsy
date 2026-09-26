using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Jobsy.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddUserDeviceSessions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "DeviceSessionId",
                table: "WebPushSubscriptions",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "SessionVersion",
                table: "Users",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "MinimumSessionVersion",
                table: "PlatformFeatureSettings",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "DeviceLoginHandoffs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CodeHash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    RememberDevice = table.Column<bool>(type: "boolean", nullable: false),
                    ReturnUrl = table.Column<string>(type: "character varying(2048)", maxLength: 2048, nullable: true),
                    UserAgent = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ExpiresAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UsedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DeviceLoginHandoffs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DeviceLoginHandoffs_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "UserDeviceSessions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    RefreshTokenHash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    PreviousRefreshTokenHash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    PreviousTokenGraceUntilUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    FamilyId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    LastUsedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ExpiresAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    RevokedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    RevokedReason = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    UserAgent = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    DeviceName = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserDeviceSessions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserDeviceSessions_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_WebPushSubscriptions_DeviceSessionId",
                table: "WebPushSubscriptions",
                column: "DeviceSessionId");

            migrationBuilder.CreateIndex(
                name: "IX_DeviceLoginHandoffs_CodeHash",
                table: "DeviceLoginHandoffs",
                column: "CodeHash",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DeviceLoginHandoffs_ExpiresAtUtc",
                table: "DeviceLoginHandoffs",
                column: "ExpiresAtUtc");

            migrationBuilder.CreateIndex(
                name: "IX_DeviceLoginHandoffs_UserId",
                table: "DeviceLoginHandoffs",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_UserDeviceSessions_FamilyId",
                table: "UserDeviceSessions",
                column: "FamilyId");

            migrationBuilder.CreateIndex(
                name: "IX_UserDeviceSessions_PreviousRefreshTokenHash",
                table: "UserDeviceSessions",
                column: "PreviousRefreshTokenHash");

            migrationBuilder.CreateIndex(
                name: "IX_UserDeviceSessions_RefreshTokenHash",
                table: "UserDeviceSessions",
                column: "RefreshTokenHash");

            migrationBuilder.CreateIndex(
                name: "IX_UserDeviceSessions_UserId_RevokedAtUtc",
                table: "UserDeviceSessions",
                columns: new[] { "UserId", "RevokedAtUtc" });

            migrationBuilder.AddForeignKey(
                name: "FK_WebPushSubscriptions_UserDeviceSessions_DeviceSessionId",
                table: "WebPushSubscriptions",
                column: "DeviceSessionId",
                principalTable: "UserDeviceSessions",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_WebPushSubscriptions_UserDeviceSessions_DeviceSessionId",
                table: "WebPushSubscriptions");

            migrationBuilder.DropTable(
                name: "DeviceLoginHandoffs");

            migrationBuilder.DropTable(
                name: "UserDeviceSessions");

            migrationBuilder.DropIndex(
                name: "IX_WebPushSubscriptions_DeviceSessionId",
                table: "WebPushSubscriptions");

            migrationBuilder.DropColumn(
                name: "DeviceSessionId",
                table: "WebPushSubscriptions");

            migrationBuilder.DropColumn(
                name: "SessionVersion",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "MinimumSessionVersion",
                table: "PlatformFeatureSettings");
        }
    }
}
