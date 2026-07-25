using Microsoft.EntityFrameworkCore.Migrations;
using NetTopologySuite.Geometries;

#nullable disable

namespace Jobsy.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class Sprint4HomeLocationAndTokenProducts : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Point>(
                name: "HomeLocation",
                table: "Users",
                type: "geometry(Point, 4326)",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Users_HomeLocation",
                table: "Users",
                column: "HomeLocation")
                .Annotation("Npgsql:IndexMethod", "GIST");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Users_HomeLocation",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "HomeLocation",
                table: "Users");
        }
    }
}
