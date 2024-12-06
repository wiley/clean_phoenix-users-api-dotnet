using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

namespace WLSUser.Migrations
{
    public partial class AddFederation : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Federations",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    OrganizationId = table.Column<int>(nullable: false),
                    Name = table.Column<string>(nullable: false),
                    OpenIdAuthInitUrl = table.Column<string>(nullable: false),
                    OpenIdTokenUrl = table.Column<string>(nullable: false),
                    OpenIdClientId = table.Column<string>(nullable: false),
                    OpenIdClientSecret = table.Column<string>(nullable: false),
                    RedirectUrl = table.Column<string>(nullable: false),
                    AlmFederationName = table.Column<string>(nullable: true),
                    EmailDomain = table.Column<string>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Federations", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Federations_Name",
                table: "Federations",
                column: "Name",
                unique: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Federations");
        }
    }
}
