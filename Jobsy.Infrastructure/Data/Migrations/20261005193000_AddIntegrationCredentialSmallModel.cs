using Jobsy.Infrastructure.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Jobsy.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    [DbContext(typeof(JobsyDbContext))]
    [Migration("20261005193000_AddIntegrationCredentialSmallModel")]
    public partial class AddIntegrationCredentialSmallModel : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "SmallModel",
                table: "IntegrationCredentials",
                type: "character varying(64)",
                maxLength: 64,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "SmallModel",
                table: "IntegrationCredentials");
        }
    }
}
