using Jobsy.Infrastructure.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Jobsy.Infrastructure.Data.Migrations
{
    [DbContext(typeof(JobsyDbContext))]
    [Migration("20260924100000_AddAtsListingEnrichmentFields")]
    public class AddAtsListingEnrichmentFields : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "StartDateText",
                table: "AtsScrapedListings",
                type: "character varying(256)",
                maxLength: 256,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RequirementsText",
                table: "AtsScrapedListings",
                type: "character varying(4000)",
                maxLength: 4000,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "AiEnrichedAtUtc",
                table: "AtsScrapedListings",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "AiEnrichedFromOpenAi",
                table: "AtsScrapedListings",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(name: "StartDateText", table: "AtsScrapedListings");
            migrationBuilder.DropColumn(name: "RequirementsText", table: "AtsScrapedListings");
            migrationBuilder.DropColumn(name: "AiEnrichedAtUtc", table: "AtsScrapedListings");
            migrationBuilder.DropColumn(name: "AiEnrichedFromOpenAi", table: "AtsScrapedListings");
        }
    }
}
