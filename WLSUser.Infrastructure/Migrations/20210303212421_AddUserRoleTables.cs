using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

namespace WLSUser.Migrations
{
    public partial class AddUserRoleTables : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            #region Create Tables

            migrationBuilder.CreateTable(
                name: "AccessTypes",
                columns: table => new
                {
                    AccessTypeID = table.Column<int>(nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    AccessTypeName = table.Column<string>(maxLength: 245, nullable: false)
                },
                constraints: table => { table.PrimaryKey("PK_AccessTypes", x => x.AccessTypeID); });

            migrationBuilder.CreateTable(
                name: "Brands",
                columns: table => new
                {
                    BrandID = table.Column<int>(nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    BrandName = table.Column<string>(maxLength: 245, nullable: false)
                },
                constraints: table => { table.PrimaryKey("PK_Brands", x => x.BrandID); });

            migrationBuilder.CreateTable(
                name: "RoleTypes",
                columns: table => new
                {
                    RoleTypeID = table.Column<int>(nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    BrandID = table.Column<int>(nullable: false),
                    RoleName = table.Column<string>(maxLength: 245, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RoleTypes", x => x.RoleTypeID);
                    table.ForeignKey("FK_RoleTypes_BrandID", x => x.BrandID, "Brands", "BrandID");
                });

            migrationBuilder.CreateTable(
                name: "UserRoles",
                columns: table => new
                {
                    UserRoleID = table.Column<int>(nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    UserID = table.Column<int>(nullable: false),
                    RoleTypeID = table.Column<int>(nullable: false),
                    Created = table.Column<DateTime>(nullable: false, defaultValueSql: "CURRENT_TIMESTAMP(6)")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserRoles", x => x.UserRoleID);
                    table.ForeignKey("FK_UserRoles_UserID", x => x.UserID, "Users", "ID");
                    table.ForeignKey("FK_UserRoles_RoleTypeID", x => x.RoleTypeID, "RoleTypes", "RoleTypeID");
                });

            migrationBuilder.CreateTable(
                name: "UserRoleAccess",
                columns: table => new
                {
                    UserRoleID = table.Column<int>(nullable: false),
                    AccessTypeID = table.Column<int>(nullable: false),
                    AccessRefID = table.Column<int>(nullable: false),
                    GrantedBy = table.Column<int>(nullable: false),
                    Created = table.Column<DateTime>(nullable: false, defaultValueSql: "CURRENT_TIMESTAMP(6)")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserRoleAccess", x => new { x.UserRoleID, x.AccessTypeID, x.AccessRefID });
                    table.ForeignKey("FK_UserRoleAccess_UserRoleID", x => x.UserRoleID, "UserRoles", "UserRoleID");
                    table.ForeignKey("FK_UserRoleAccess_AccessTypeID", x => x.AccessTypeID, "AccessTypes", "AccessTypeID");
                    table.ForeignKey("FK_UserRoleAccess_GrantedBy", x => x.GrantedBy, "Users", "ID");
                });

            #endregion

            #region Create Indexes

            migrationBuilder.CreateIndex(
                name: "IX_AccessTypes_AccessTypeID",
                table: "AccessTypes",
                column: "AccessTypeID",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Brands_BrandID",
                table: "Brands",
                column: "BrandID",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RoleTypes_BrandID",
                table: "RoleTypes",
                column: "BrandID");

            migrationBuilder.CreateIndex(
                name: "IX_RoleTypes_RoleTypeID",
                table: "RoleTypes",
                column: "RoleTypeID",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_UserRoleAccess_AccessRefID",
                table: "UserRoleAccess",
                column: "AccessRefID");

            migrationBuilder.CreateIndex(
                name: "IX_UserRoleAccess_AccessTypeID",
                table: "UserRoleAccess",
                column: "AccessTypeID");

            migrationBuilder.CreateIndex(
                name: "IX_UserRoleAccess_GrantedBy",
                table: "UserRoleAccess",
                column: "GrantedBy");

            migrationBuilder.CreateIndex(
                name: "IX_UserRoleAccess_UserRoleID",
                table: "UserRoleAccess",
                column: "UserRoleID");

            migrationBuilder.CreateIndex(
                name: "IX_UserRoles_RoleTypeID",
                table: "UserRoles",
                column: "RoleTypeID");

            migrationBuilder.CreateIndex(
                name: "IX_UserRoles_UserRoleID",
                table: "UserRoles",
                column: "UserRoleID",
                unique: true);

            #endregion
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Note: Due to foreign key constraints, must drop tables in this order
            migrationBuilder.DropTable(
                name: "UserRoleAccess");

            migrationBuilder.DropTable(
                name: "UserRoles");

            migrationBuilder.DropTable(
                name: "RoleTypes");

            migrationBuilder.DropTable(
                name: "Brands");

            migrationBuilder.DropTable(
                name: "AccessTypes");
        }
    }
}