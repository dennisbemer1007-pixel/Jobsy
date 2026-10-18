using Jobsy.Infrastructure.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Jobsy.Infrastructure.Data.Migrations
{
    [DbContext(typeof(JobsyDbContext))]
    [Migration("20261018120000_AddTestItemExamplesEnabled")]
    public partial class AddTestItemExamplesEnabled : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                ALTER TABLE "PlatformFeatureSettings" ADD COLUMN IF NOT EXISTS "TestItemExamplesEnabled" boolean NOT NULL DEFAULT false;
                """);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "TestItemExamplesEnabled",
                table: "PlatformFeatureSettings");
        }
    }
}
