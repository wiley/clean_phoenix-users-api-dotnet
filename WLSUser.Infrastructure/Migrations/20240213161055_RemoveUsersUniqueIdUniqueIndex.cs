using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WLSUser.Migrations
{
    public partial class RemoveUsersUniqueIdUniqueIndex : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
             migrationBuilder.Sql($"ALTER TABLE Users MODIFY UserID int NOT NULL AUTO_INCREMENT FIRST");
            
            migrationBuilder.AlterColumn<int>(
                name: "Id",
                table: "Users",
                type: "int",
                nullable: true,
                defaultValueSql: "NULL");
            
            migrationBuilder.AlterColumn<string>(
                name: "UniqueID",
                table: "Users",
                maxLength: 255,
                nullable: true,
                defaultValueSql: "NULL");
            
            migrationBuilder.DropIndex(
                name: "IX_Users_UniqueID",
                table: "Users");

            // didn't find how to generate Foreign keys based on the model
            // so force the db changes manually
            migrationBuilder.DropForeignKey(
                name: "FK_UserRoleAccess_GrantedBy",
                table: "UserRoleAccess");

            migrationBuilder.AddForeignKey(
                name: "FK_UserRoleAccess_GrantedBy",
                table: "UserRoleAccess",
                column: "GrantedBy",
                principalTable: "Users",
                principalColumn: "UserID");

            migrationBuilder.DropForeignKey(
                name: "FK_UserRoles_UserID",
                table: "UserRoles");
            
            migrationBuilder.AddForeignKey(
                name: "FK_UserRoles_UserID",
                table: "UserRoles",
                column: "UserID",
                principalTable: "Users",
                principalColumn: "UserID");

            migrationBuilder.DropIndex(
                name: "IX_Users_Id",
                table: "Users");

        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_Users_Id",
                table: "Users",
                column: "Id",
                unique: true);

             migrationBuilder.CreateIndex(
                name: "IX_Users_UniqueID",
                table: "Users",
                column: "UniqueID",
                unique: true);
            
            // didn't find how to generate Foreign keys based on the model
            // so force the db changes manually
            migrationBuilder.DropForeignKey(
                name: "FK_UserRoleAccess_GrantedBy",
                table: "UserRoleAccess");

            migrationBuilder.AddForeignKey(
                name: "FK_UserRoleAccess_GrantedBy",
                table: "UserRoleAccess",
                column: "GrantedBy",
                principalTable: "Users",
                principalColumn: "ID");

            migrationBuilder.DropForeignKey(
                name: "FK_UserRoles_UserID",
                table: "UserRoles");
            
            migrationBuilder.AddForeignKey(
                name: "FK_UserRoles_UserID",
                table: "UserRoles",
                column: "UserID",
                principalTable: "Users",
                principalColumn: "ID");
        }
    }
}
