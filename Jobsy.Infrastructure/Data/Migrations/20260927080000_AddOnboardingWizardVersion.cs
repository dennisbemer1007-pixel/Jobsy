using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Jobsy.Infrastructure.Data;

#nullable disable

namespace Jobsy.Infrastructure.Data.Migrations
{
    [DbContext(typeof(JobsyDbContext))]
    [Migration("20260927080000_AddOnboardingWizardVersion")]
    public class AddOnboardingWizardVersion : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "WizardVersion",
                table: "CandidateOnboardings",
                type: "integer",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<bool>(
                name: "FinishReached",
                table: "CandidateOnboardings",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "FinishReached",
                table: "CandidateOnboardings");

            migrationBuilder.DropColumn(
                name: "WizardVersion",
                table: "CandidateOnboardings");
        }
    }
}
