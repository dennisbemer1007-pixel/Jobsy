using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Jobsy.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddApplicationStatusHistory : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ApplicationStatusHistories",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ApplicationId = table.Column<Guid>(type: "uuid", nullable: false),
                    Kind = table.Column<int>(type: "integer", nullable: false),
                    FromStatus = table.Column<int>(type: "integer", nullable: true),
                    ToStatus = table.Column<int>(type: "integer", nullable: true),
                    OccurredAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ActorKind = table.Column<int>(type: "integer", nullable: false),
                    ActorUserId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ApplicationStatusHistories", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ApplicationStatusHistories_Applications_ApplicationId",
                        column: x => x.ApplicationId,
                        principalTable: "Applications",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ApplicationStatusHistories_ApplicationId_EmployerViewed",
                table: "ApplicationStatusHistories",
                column: "ApplicationId",
                unique: true,
                filter: "\"Kind\" = 2");

            migrationBuilder.CreateIndex(
                name: "IX_ApplicationStatusHistories_ApplicationId_OccurredAtUtc",
                table: "ApplicationStatusHistories",
                columns: new[] { "ApplicationId", "OccurredAtUtc" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ApplicationStatusHistories");
        }
    }
}
