using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Jobsy.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddDismissedVestigingSuggestions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "LastAutoPublishedVacancyCount",
                table: "Companies",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "DismissedVestigingSuggestions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CompanyId = table.Column<Guid>(type: "uuid", nullable: false),
                    KvkEstablishmentId = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    HiddenUntilUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    DismissedByUserId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DismissedVestigingSuggestions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DismissedVestigingSuggestions_Companies_CompanyId",
                        column: x => x.CompanyId,
                        principalTable: "Companies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_DismissedVestigingSuggestions_CompanyId_KvkEstablishmentId",
                table: "DismissedVestigingSuggestions",
                columns: new[] { "CompanyId", "KvkEstablishmentId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DismissedVestigingSuggestions_HiddenUntilUtc",
                table: "DismissedVestigingSuggestions",
                column: "HiddenUntilUtc");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DismissedVestigingSuggestions");

            migrationBuilder.DropColumn(
                name: "LastAutoPublishedVacancyCount",
                table: "Companies");
        }
    }
}
