using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Jobsy.Infrastructure.Data.Migrations
{
    [DbContext(typeof(JobsyDbContext))]
    [Migration("20260728214500_AddPlatformCompanySettings")]
    public partial class AddPlatformCompanySettings : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PlatformCompanySettings",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CompanyName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Slogan = table.Column<string>(type: "character varying(240)", maxLength: 240, nullable: true),
                    Address = table.Column<string>(type: "character varying(240)", maxLength: 240, nullable: true),
                    PostalCode = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    City = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: true),
                    Country = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: true),
                    KvkNumber = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true),
                    VatNumber = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true),
                    Phone = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: true),
                    Email = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PlatformCompanySettings", x => x.Id);
                });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "PlatformCompanySettings");
        }
    }
}
