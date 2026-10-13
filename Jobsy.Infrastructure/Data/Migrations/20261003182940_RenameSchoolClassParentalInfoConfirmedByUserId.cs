using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Jobsy.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class RenameSchoolClassParentalInfoConfirmedByUserId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // AddScholenFoundation created this column as ParentalInfoConfirmedByInvitedUserId.
            // The model and every later snapshot already call it ParentalInfoConfirmedByUserId,
            // so databases built from the chain never received a rename.
            migrationBuilder.RenameColumn(
                name: "ParentalInfoConfirmedByInvitedUserId",
                table: "SchoolClasses",
                newName: "ParentalInfoConfirmedByUserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "ParentalInfoConfirmedByUserId",
                table: "SchoolClasses",
                newName: "ParentalInfoConfirmedByInvitedUserId");
        }
    }
}
