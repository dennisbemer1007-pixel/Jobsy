using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Jobsy.Infrastructure.Data.Migrations
{
    [DbContext(typeof(JobsyDbContext))]
    [Migration("20260729104500_AddAccountUnsubscribeChallenge")]
    public partial class AddAccountUnsubscribeChallenge : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "UnsubscribeReasonCode",
                table: "Users",
                type: "character varying(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "UnsubscribeReasonOther",
                table: "Users",
                type: "character varying(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "UnsubscribeVerificationCode",
                table: "Users",
                type: "character varying(6)",
                maxLength: 6,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "UnsubscribeVerificationExpiresAt",
                table: "Users",
                type: "timestamp with time zone",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "UnsubscribeReasonCode",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "UnsubscribeReasonOther",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "UnsubscribeVerificationCode",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "UnsubscribeVerificationExpiresAt",
                table: "Users");
        }
    }
}
