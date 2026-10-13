using Jobsy.Infrastructure.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Jobsy.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    [DbContext(typeof(JobsyDbContext))]
    [Migration("20261005083000_AddCompactTestPdfEnabled")]
    public partial class AddCompactTestPdfEnabled : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "CompactTestPdfEnabled",
                table: "PlatformFeatureSettings",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CompactTestPdfEnabled",
                table: "PlatformFeatureSettings");
        }
    }
}
