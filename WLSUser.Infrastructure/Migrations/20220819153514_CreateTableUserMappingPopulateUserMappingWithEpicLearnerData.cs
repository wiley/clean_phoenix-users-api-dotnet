using Microsoft.EntityFrameworkCore.Migrations;

namespace WLSUser.Migrations
{
    public partial class CreateTableUserMappingPopulateUserMappingWithEpicLearnerData : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql($"insert into UserMappings(UserId, PlatformName, PlatformCustomer, PlatformRole, PlatformUserId, PlatformPasswordHash, PlatformPasswordSalt, PlatformPassowrdMethod, Created, Updated) " +
                                 $"select u.UserID as 'UserId' " +
                                 $", REPLACE(SUBSTRING(SUBSTRING_INDEX(u.UniqueID, ':', 1), LENGTH(SUBSTRING_INDEX(u.UniqueID, ':', 1 -1)) + 1), ':', '') 'PlatformName' " +
                                 $", REPLACE(SUBSTRING(SUBSTRING_INDEX(u.UniqueID, ':', 2), LENGTH(SUBSTRING_INDEX(u.UniqueID, ':', 2 -1)) + 1), ':', '') 'PlatformCustomer' " +
                                 $", REPLACE(SUBSTRING(SUBSTRING_INDEX(u.UniqueID, ':', 3), LENGTH(SUBSTRING_INDEX(u.UniqueID, ':', 3 -1)) + 1), ':', '') 'PlatformRole' " +
                                 $", REPLACE(SUBSTRING(SUBSTRING_INDEX(u.UniqueID, ':', 4), LENGTH(SUBSTRING_INDEX(u.UniqueID, ':', 4 -1)) + 1), ':', '') as 'PlatformUserID' " +
                                 $", u.OrigPasswordHash as 'PlatformPasswordHash' " +
                                 $", u.OrigPasswordSalt as 'PlatformPasswordSalt' " +
                                 $", 'SHA1'  as 'PlatformPassowrdMethod' " +
                                 $", u.LastUpdated " +
                                 $", u.LastUpdated " +
                                 $"from Users u " +
                                 $"where u.UserType = 2 " +
                                 $"and u.UserID is not null " +
                                 $"and u.Status = 0; ");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("set foreign_key_checks = 0;");
            migrationBuilder.Sql("truncate table UserMappings;");
            migrationBuilder.Sql("set foreign_key_checks = 1;");
        }
    }
}
