using Jobsy.Infrastructure.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Jobsy.Infrastructure.Data.Migrations;

[DbContext(typeof(JobsyDbContext))]
[Migration("20261004230000_AddWhoAmILastAttempt")]
public class AddWhoAmILastAttempt : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<DateTime>(
            name: "LastAttemptUtc",
            table: "CandidateWhoAmIProfiles",
            type: "timestamp with time zone",
            nullable: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(
            name: "LastAttemptUtc",
            table: "CandidateWhoAmIProfiles");
    }
}
