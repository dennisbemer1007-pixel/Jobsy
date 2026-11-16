using Jobsy.Infrastructure.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Jobsy.Infrastructure.Data.Migrations;

/// <inheritdoc />
[DbContext(typeof(JobsyDbContext))]
[Migration("20261115201500_AddWerkgeverApkEmployeeEscoOccupation")]
public partial class AddWerkgeverApkEmployeeEscoOccupation : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>(
            name: "EscoOccupationId",
            table: "WerkgeverApkEmployeeCodes",
            type: "character varying(36)",
            maxLength: 36,
            nullable: true);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(
            name: "EscoOccupationId",
            table: "WerkgeverApkEmployeeCodes");
    }
}