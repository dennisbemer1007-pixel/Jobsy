using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Jobsy.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddSalesRecommendObjectionToken : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ObjectionTokenHash",
                table: "SalesManagerApplications",
                type: "character varying(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_SalesManagerApplications_ObjectionTokenHash",
                table: "SalesManagerApplications",
                column: "ObjectionTokenHash");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_SalesManagerApplications_ObjectionTokenHash",
                table: "SalesManagerApplications");

            migrationBuilder.DropColumn(
                name: "ObjectionTokenHash",
                table: "SalesManagerApplications");
        }
    }
}
