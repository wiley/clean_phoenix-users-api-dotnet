using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WLSUser.Migrations
{
    public partial class FixUsersTable : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("ALTER TABLE Users MODIFY COLUMN Id int NULL DEFAULT NULL");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // No need
        }
    }
}
