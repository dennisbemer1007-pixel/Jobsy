using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Jobsy.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddDeviceSessionAuthMethod : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AuthMethod",
                table: "UserDeviceSessions",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AuthTenantId",
                table: "UserDeviceSessions",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AuthMethod",
                table: "UserDeviceSessions");

            migrationBuilder.DropColumn(
                name: "AuthTenantId",
                table: "UserDeviceSessions");
        }
    }
}
