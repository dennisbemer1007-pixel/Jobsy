using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Jobsy.Infrastructure.Data.Migrations
{
    [DbContext(typeof(JobsyDbContext))]
    [Migration("20260828140000_SalesCommissionStaffels")]
    public partial class SalesCommissionStaffels : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "Year2DirectCommissionRate",
                table: "SalesCommercialSettings",
                type: "numeric(5,4)",
                precision: 5,
                scale: 4,
                nullable: false,
                defaultValue: 0.10m);

            migrationBuilder.AddColumn<decimal>(
                name: "Year3DirectCommissionRate",
                table: "SalesCommercialSettings",
                type: "numeric(5,4)",
                precision: 5,
                scale: 4,
                nullable: false,
                defaultValue: 0.05m);

            migrationBuilder.AddColumn<decimal>(
                name: "ReferredYear1DirectCommissionRate",
                table: "SalesCommercialSettings",
                type: "numeric(5,4)",
                precision: 5,
                scale: 4,
                nullable: false,
                defaultValue: 0.20m);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Year2DirectCommissionRate",
                table: "SalesCommercialSettings");

            migrationBuilder.DropColumn(
                name: "Year3DirectCommissionRate",
                table: "SalesCommercialSettings");

            migrationBuilder.DropColumn(
                name: "ReferredYear1DirectCommissionRate",
                table: "SalesCommercialSettings");
        }
    }
}
