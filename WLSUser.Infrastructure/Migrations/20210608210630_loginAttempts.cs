using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace WLSUser.Migrations
{
    public partial class loginAttempts : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "LoginAttempts",
                columns: table => new
                {
                    LoginAttemptID = table.Column<Guid>(nullable: false),
                    UserID = table.Column<int>(nullable: false),
                    Attempted = table.Column<DateTime>(nullable: false, defaultValueSql: "CURRENT_TIMESTAMP(6)"),
                    Success = table.Column<bool>(nullable: false, defaultValueSql: "0")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LoginAttempts", x => x.LoginAttemptID);
                });

            migrationBuilder.CreateIndex(
                name: "IX_LoginAttempts_LoginAttemptID",
                table: "LoginAttempts",
                column: "LoginAttemptID");

            migrationBuilder.CreateIndex(
                name: "IX_LoginAttempts_UserID",
                table: "LoginAttempts",
                column: "UserID");

            migrationBuilder.CreateIndex(
                name: "IX_LoginAttempts_UserID_Attempted",
                table: "LoginAttempts",
                columns: new[] { "UserID", "Attempted" },
                unique: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "LoginAttempts");
        }
    }
}
