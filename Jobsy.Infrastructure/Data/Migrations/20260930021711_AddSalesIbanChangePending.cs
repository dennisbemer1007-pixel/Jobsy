using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Jobsy.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddSalesIbanChangePending : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "SalesIbanChangePendings",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    EncryptedIban = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    HolderName = table.Column<string>(type: "character varying(70)", maxLength: 70, nullable: false),
                    Method = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    EmailTokenHash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    FailedAttempts = table.Column<int>(type: "integer", nullable: false),
                    LastAcceptedTotpCodeHash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ExpiresAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ConsumedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SalesIbanChangePendings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SalesIbanChangePendings_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SalesIbanChangePendings_EmailTokenHash",
                table: "SalesIbanChangePendings",
                column: "EmailTokenHash");

            migrationBuilder.CreateIndex(
                name: "IX_SalesIbanChangePendings_UserId",
                table: "SalesIbanChangePendings",
                column: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SalesIbanChangePendings");
        }
    }
}
