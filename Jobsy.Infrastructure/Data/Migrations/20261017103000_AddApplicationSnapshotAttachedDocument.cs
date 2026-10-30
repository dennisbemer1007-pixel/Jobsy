using Jobsy.Infrastructure.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Jobsy.Infrastructure.Data.Migrations;

[DbContext(typeof(JobsyDbContext))]
[Migration("20261017103000_AddApplicationSnapshotAttachedDocument")]
public partial class AddApplicationSnapshotAttachedDocument : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(
            """
            ALTER TABLE "Applications"
            ADD COLUMN IF NOT EXISTS "SnapshotAttachedDocument" character varying(16);
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(
            name: "SnapshotAttachedDocument",
            table: "Applications");
    }
}
