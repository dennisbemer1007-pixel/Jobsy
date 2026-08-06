using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Jobsy.Infrastructure.Data.Migrations
{
    [DbContext(typeof(JobsyDbContext))]
    [Migration("20260806160000_CandidateCertificatesAndAddressPrivacy")]
    public partial class CandidateCertificatesAndAddressPrivacy : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "SnapshotCertificatesJson",
                table: "Applications",
                type: "character varying(4000)",
                maxLength: 4000,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "SnapshotShowAddressOnCv",
                table: "Applications",
                type: "boolean",
                nullable: false,
                defaultValue: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(name: "SnapshotCertificatesJson", table: "Applications");
            migrationBuilder.DropColumn(name: "SnapshotShowAddressOnCv", table: "Applications");
        }
    }
}
