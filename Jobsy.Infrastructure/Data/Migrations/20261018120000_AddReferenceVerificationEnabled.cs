using Jobsy.Infrastructure.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Jobsy.Infrastructure.Data.Migrations;

[DbContext(typeof(JobsyDbContext))]
[Migration("20261018120000_AddReferenceVerificationEnabled")]
public partial class AddReferenceVerificationEnabled : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<bool>(
            name: "ReferenceVerificationEnabled",
            table: "PlatformFeatureSettings",
            type: "boolean",
            nullable: false,
            defaultValue: false);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(
            name: "ReferenceVerificationEnabled",
            table: "PlatformFeatureSettings");
    }
}
