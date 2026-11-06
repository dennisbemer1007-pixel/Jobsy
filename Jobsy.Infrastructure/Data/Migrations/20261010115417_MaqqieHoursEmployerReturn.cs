using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Jobsy.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class MaqqieHoursEmployerReturn : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "EmployerReturnNote",
                table: "MaqqieHoursWeeks",
                type: "character varying(280)",
                maxLength: 280,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "EmployerReturnedAtUtc",
                table: "MaqqieHoursWeeks",
                type: "timestamp with time zone",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "EmployerReturnNote",
                table: "MaqqieHoursWeeks");

            migrationBuilder.DropColumn(
                name: "EmployerReturnedAtUtc",
                table: "MaqqieHoursWeeks");
        }
    }
}
