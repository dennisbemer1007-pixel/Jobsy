using Jobsy.Infrastructure.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Jobsy.Infrastructure.Data.Migrations;

/// <summary>
/// <see cref="AddPassportPdfDnaBlocksEnabled"/> predates merges that already recorded
/// <c>20261016163000_AddPupilPassportV2Flags</c>, so EF skips it on databases created mid-merge.
/// </summary>
<<<<<<< HEAD
[DbContext(typeof(JobsyDbContext))]
=======
>>>>>>> 56d4fc33c (Fix error shell variant, migration attributes, and 404 test expectations.)
[Migration("20261016170000_EnsurePassportPdfDnaBlocksAfterMerge")]
public partial class EnsurePassportPdfDnaBlocksAfterMerge : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(PlatformFeatureSchemaIdempotentSql.EnsurePassportPdfDnaBlocksEnabled);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(
            name: "PassportPdfDnaBlocksEnabled",
            table: "PlatformFeatureSettings");
    }
}
