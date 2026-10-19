using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Jobsy.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddCompanyMapInitiativeFollowUp : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "HubPhotoUrlsJson",
                table: "Companies",
                type: "text",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "WorkInitiativeAcceptances",
                columns: table => new
                {
                    InitiativeId = table.Column<Guid>(type: "uuid", nullable: false),
                    BillingCompanyId = table.Column<Guid>(type: "uuid", nullable: false),
                    AcceptCostTokens = table.Column<decimal>(type: "numeric", nullable: false),
                    AcceptSpendTransactionId = table.Column<Guid>(type: "uuid", nullable: true),
                    AcceptedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WorkInitiativeAcceptances", x => x.InitiativeId);
                    table.ForeignKey(
                        name: "FK_WorkInitiativeAcceptances_WorkInitiatives_InitiativeId",
                        column: x => x.InitiativeId,
                        principalTable: "WorkInitiatives",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "WorkInitiativeAcceptances");

            migrationBuilder.DropColumn(
                name: "HubPhotoUrlsJson",
                table: "Companies");
        }
    }
}
