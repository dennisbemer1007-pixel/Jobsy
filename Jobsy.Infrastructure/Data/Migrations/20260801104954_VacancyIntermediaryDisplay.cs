using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Jobsy.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class VacancyIntermediaryDisplay : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "IntermediaryCompanyId",
                table: "Vacancies",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "ShowClientAddressOnMap",
                table: "Vacancies",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateIndex(
                name: "IX_Vacancies_IntermediaryCompanyId",
                table: "Vacancies",
                column: "IntermediaryCompanyId");

            migrationBuilder.AddForeignKey(
                name: "FK_Vacancies_Companies_IntermediaryCompanyId",
                table: "Vacancies",
                column: "IntermediaryCompanyId",
                principalTable: "Companies",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Vacancies_Companies_IntermediaryCompanyId",
                table: "Vacancies");

            migrationBuilder.DropIndex(
                name: "IX_Vacancies_IntermediaryCompanyId",
                table: "Vacancies");

            migrationBuilder.DropColumn(
                name: "IntermediaryCompanyId",
                table: "Vacancies");

            migrationBuilder.DropColumn(
                name: "ShowClientAddressOnMap",
                table: "Vacancies");
        }
    }
}
