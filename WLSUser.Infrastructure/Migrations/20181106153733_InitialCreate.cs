using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

namespace WLSUser.Migrations
{
    public partial class InitialCreate : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Users",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    UniqueID = table.Column<string>(nullable: false),
                    Username = table.Column<string>(maxLength: 255, nullable: false),
                    FirstName = table.Column<string>(maxLength: 100, nullable: false),
                    LastName = table.Column<string>(maxLength: 100, nullable: false),
                    UserType = table.Column<int>(nullable: false),
                    OrigPasswordSalt = table.Column<string>(maxLength: 50, nullable: true),
                    OrigPasswordHash = table.Column<string>(maxLength: 50, nullable: true),
                    StrongPasswordSalt = table.Column<string>(maxLength: 50, nullable: true),
                    StrongPasswordHash = table.Column<string>(maxLength: 50, nullable: true),
                    StrongPasswordSet = table.Column<DateTime>(nullable: true),
                    StrongPasswordGoodUntil = table.Column<DateTime>(nullable: true)
                },
                constraints: table => { table.PrimaryKey("PK_Users", x => x.Id); });

            migrationBuilder.CreateIndex(
                name: "IX_Users_UniqueID",
                table: "Users",
                column: "UniqueID",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Users_Username_UserType",
                table: "Users",
                columns: new[] { "Username", "UserType" });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Users");
        }
    }
}