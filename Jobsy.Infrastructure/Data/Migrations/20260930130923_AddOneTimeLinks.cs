using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Jobsy.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddOneTimeLinks : Migration
    {
        // Users.SchoolId, PersonalDataAccessLogs.SubjectPupilCodeId and their index/foreign key
        // already come from the earlier AddScholenFoundation migration; repeating them here made
        // every fresh database fail with "column ... already exists".

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "OneTimeLinks",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Purpose = table.Column<int>(type: "integer", nullable: false),
                    TokenHash = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: true),
                    CompanyId = table.Column<Guid>(type: "uuid", nullable: true),
                    Email = table.Column<string>(type: "character varying(254)", maxLength: 254, nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ExpiresAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UsedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedByUserId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OneTimeLinks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OneTimeLinks_Companies_CompanyId",
                        column: x => x.CompanyId,
                        principalTable: "Companies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_OneTimeLinks_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_OneTimeLinks_CompanyId",
                table: "OneTimeLinks",
                column: "CompanyId");

            migrationBuilder.CreateIndex(
                name: "IX_OneTimeLinks_Purpose_UserId_UsedAtUtc",
                table: "OneTimeLinks",
                columns: new[] { "Purpose", "UserId", "UsedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_OneTimeLinks_TokenHash",
                table: "OneTimeLinks",
                column: "TokenHash",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_OneTimeLinks_UserId",
                table: "OneTimeLinks",
                column: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "OneTimeLinks");
        }
    }
}
