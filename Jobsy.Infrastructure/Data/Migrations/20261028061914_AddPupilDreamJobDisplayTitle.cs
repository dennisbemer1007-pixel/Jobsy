using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Jobsy.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddPupilDreamJobDisplayTitle : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "DreamJobDisplayTitle",
                table: "PupilResults",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DreamJobDisplayTitle",
                table: "PupilProgresses",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DreamJobDisplayTitle",
                table: "PupilResults");

            migrationBuilder.DropColumn(
                name: "DreamJobDisplayTitle",
                table: "PupilProgresses");
        }
    }
}
