using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Jobsy.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddReferenceMisuseHandled : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "HandledAtUtc",
                table: "ReferenceMisuseReports",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "HandledByUserId",
                table: "ReferenceMisuseReports",
                type: "uuid",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "HandledAtUtc",
                table: "ReferenceMisuseReports");

            migrationBuilder.DropColumn(
                name: "HandledByUserId",
                table: "ReferenceMisuseReports");
        }
    }
}
