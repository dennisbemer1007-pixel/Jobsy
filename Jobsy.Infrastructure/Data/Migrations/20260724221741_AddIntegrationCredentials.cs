using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Jobsy.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddIntegrationCredentials : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "IntegrationCredentials",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Key = table.Column<int>(type: "integer", nullable: false),
                    ApiKey = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    Model = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_IntegrationCredentials", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_IntegrationCredentials_Key",
                table: "IntegrationCredentials",
                column: "Key",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "IntegrationCredentials");
        }
    }
}
