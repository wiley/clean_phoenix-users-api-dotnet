using Microsoft.EntityFrameworkCore.Migrations;

namespace WLSUser.Migrations
{
    public partial class FederationChangeSiteIdDefaultValue : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Federations_Name_SiteId",
                table: "Federations");

            migrationBuilder.DropColumn(
                name: "SiteId",
                table: "Federations");

            migrationBuilder.AddColumn<int>(
                name: "SiteId",
                table: "Federations",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.CreateIndex(
                name: "X_Federations_Name_SiteId",
                table: "Federations",
                columns: new[] { "Name", "SiteId" },
                unique: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "X_Federations_Name_SiteId",
                table: "Federations");

            migrationBuilder.DropColumn(
                name: "SiteId",
                table: "Federations");

            migrationBuilder.AddColumn<int>(
                name: "SiteId",
                table: "Federations",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_Federations_Name_SiteId",
                table: "Federations",
                columns: new[] { "Name", "SiteId" },
                unique: true);
        }
    }
}
