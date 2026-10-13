using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Jobsy.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddAssessmentNormSnapshots : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AssessmentNormSnapshots",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Kind = table.Column<int>(type: "integer", nullable: false),
                    Domain = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    N = table.Column<int>(type: "integer", nullable: false),
                    Mean = table.Column<double>(type: "double precision", nullable: false),
                    P25 = table.Column<double>(type: "double precision", nullable: false),
                    P50 = table.Column<double>(type: "double precision", nullable: false),
                    P75 = table.Column<double>(type: "double precision", nullable: false),
                    ComputedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AssessmentNormSnapshots", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AssessmentNormSnapshots_Kind_Domain_ComputedAtUtc",
                table: "AssessmentNormSnapshots",
                columns: new[] { "Kind", "Domain", "ComputedAtUtc" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AssessmentNormSnapshots");
        }
    }
}
