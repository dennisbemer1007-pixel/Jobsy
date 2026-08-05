using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Jobsy.Infrastructure.Data.Migrations
{
    [DbContext(typeof(JobsyDbContext))]
    [Migration("20260805140000_AddFreePublishUntil")]
    public partial class AddFreePublishUntil : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateOnly>(
                name: "FreePublishUntil",
                table: "PlatformFeatureSettings",
                type: "date",
                nullable: true,
                defaultValue: new DateOnly(2026, 11, 18));
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "FreePublishUntil",
                table: "PlatformFeatureSettings");
        }
    }
}
