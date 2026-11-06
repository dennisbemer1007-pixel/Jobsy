using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Jobsy.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddTokenRequests : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "TokenRequests",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OrganisationCompanyId = table.Column<Guid>(type: "uuid", nullable: false),
                    BranchCompanyId = table.Column<Guid>(type: "uuid", nullable: false),
                    RequestedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Amount = table.Column<int>(type: "integer", nullable: false),
                    Reason = table.Column<int>(type: "integer", nullable: false),
                    Note = table.Column<string>(type: "character varying(280)", maxLength: 280, nullable: true),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    HandledByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    HandledAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TokenRequests", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TokenRequests_Companies_BranchCompanyId",
                        column: x => x.BranchCompanyId,
                        principalTable: "Companies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TokenRequests_Companies_OrganisationCompanyId",
                        column: x => x.OrganisationCompanyId,
                        principalTable: "Companies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TokenRequests_Users_HandledByUserId",
                        column: x => x.HandledByUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_TokenRequests_Users_RequestedByUserId",
                        column: x => x.RequestedByUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_TokenRequests_BranchCompanyId_Status",
                table: "TokenRequests",
                columns: new[] { "BranchCompanyId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_TokenRequests_HandledByUserId",
                table: "TokenRequests",
                column: "HandledByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_TokenRequests_OrganisationCompanyId_Status",
                table: "TokenRequests",
                columns: new[] { "OrganisationCompanyId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_TokenRequests_RequestedByUserId",
                table: "TokenRequests",
                column: "RequestedByUserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TokenRequests");
        }
    }
}
