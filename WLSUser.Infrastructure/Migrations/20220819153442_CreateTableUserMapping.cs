using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

namespace WLSUser.Migrations
{
    public partial class CreateTableUserMapping : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "UserMappings",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    UserId = table.Column<int>(nullable: false),
                    PlatformName = table.Column<string>(maxLength: 200, nullable: false),
                    PlatformCustomer = table.Column<string>(maxLength: 200, nullable: false),
                    PlatformRole = table.Column<string>(maxLength: 200, nullable: false),
                    PlatformUserId = table.Column<string>(maxLength: 100, nullable: false),
                    PlatformAccountId = table.Column<string>(maxLength: 100, nullable: true),
                    PlatformData = table.Column<string>(maxLength: 255, nullable: true),
                    PlatformPasswordHash = table.Column<string>(maxLength: 50, nullable: true),
                    PlatformPasswordSalt = table.Column<string>(maxLength: 50, nullable: true),
                    PlatformPassowrdMethod = table.Column<string>(maxLength: 20, nullable: true),
                    Created = table.Column<DateTime>(nullable: false),
                    Updated = table.Column<DateTime>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserMappings", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_UserMappings_UserId",
                table: "UserMappings",
                column: "UserId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "UserMappings");
        }
    }
}
