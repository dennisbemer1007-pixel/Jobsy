using Jobsy.Infrastructure.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Jobsy.Infrastructure.Data.Migrations;

[DbContext(typeof(JobsyDbContext))]
[Migration("20261016120000_AddPassportPdfDnaBlocksEnabled")]
public partial class AddPassportPdfDnaBlocksEnabled : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        // Idempotent: acceptatie may serve traffic before background migrate completes (#692 outage).
        migrationBuilder.Sql(PlatformFeatureSchemaIdempotentSql.EnsurePassportPdfDnaBlocksEnabled);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(
            name: "PassportPdfDnaBlocksEnabled",
            table: "PlatformFeatureSettings");
    }
}
