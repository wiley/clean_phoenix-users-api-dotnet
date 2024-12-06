using Microsoft.EntityFrameworkCore.Migrations;

namespace WLSUser.Migrations
{
    public partial class SqlInsertUserRoles : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // -- Have to add this SQL after the fact for automation environments that didn't have a chance to Seed between March 3rd 2021 and March 26th when this was originally deployed
            migrationBuilder.Sql("INSERT INTO Brands (BrandID, BrandName) SELECT 1, 'Everything DiSC' WHERE NOT EXISTS (SELECT 1 FROM Brands WHERE BrandID = 1);");
            migrationBuilder.Sql("INSERT INTO RoleTypes (RoleTypeID, BrandID, RoleName) SELECT 1,1,'Catalyst Learner' WHERE NOT EXISTS (SELECT 1 FROM RoleTypes WHERE RoleTypeID = 1);");
            //
            migrationBuilder.Sql("DELETE FROM UserRoles WHERE RoleTypeID = 1;");
            migrationBuilder.Sql($"INSERT INTO UserRoles (UserID, RoleTypeID) " +
                                 $"SELECT u.Id 'UserID', 1 'RoleTypeID' " +
                                 $"FROM Users u " +
                                 $"WHERE SUBSTRING_INDEX(u.UniqueID, ':', 3) = 'epic:singleton:learner' " +
                                 $"ORDER BY u.Id;");
        }

        protected override void Down(MigrationBuilder migrationBuilder) { }
    }
}