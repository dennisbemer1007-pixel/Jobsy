using Jobsy.Infrastructure.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Jobsy.Infrastructure.Data.Migrations;

[DbContext(typeof(JobsyDbContext))]
[Migration("20261018190000_TestEditSamplePdfFlags")]
public partial class TestEditSamplePdfFlags : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<bool>(
            name: "TestEditModeEnabled",
            table: "PlatformFeatureSettings",
            type: "boolean",
            nullable: false,
            defaultValue: false);

        migrationBuilder.AddColumn<bool>(
            name: "SamplePdfEnabled",
            table: "PlatformFeatureSettings",
            type: "boolean",
            nullable: false,
            defaultValue: false);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(name: "TestEditModeEnabled", table: "PlatformFeatureSettings");
        migrationBuilder.DropColumn(name: "SamplePdfEnabled", table: "PlatformFeatureSettings");
    }
}
