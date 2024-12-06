using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

namespace WLSUser.Migrations
{
    public partial class CleanupUserIdAndUserMapping : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql($"UPDATE Users u " +
                                    $"INNER JOIN( " +
                                    $"SELECT Id, ((ROW_NUMBER() OVER(ORDER BY ID)) + 1) + 5200000 as 'RowNumber' " +
                                    $"FROM Users " +
                                    $"where userID is null " +
                                    $") b " +
                                    $"ON u.Id = b.Id " +
                                    $"SET u.UserID = b.RowNumber; ");
            migrationBuilder.Sql($"insert into UserMappings(UserId, PlatformName, PlatformCustomer, PlatformRole, PlatformUserId, PlatformPasswordHash, PlatformPasswordSalt, PlatformPassowrdMethod, Created, Updated) " +
                                    $"select u.UserID as 'UserId' " +
                                    $", REPLACE(SUBSTRING(SUBSTRING_INDEX(u.UniqueID, ':', 1), LENGTH(SUBSTRING_INDEX(u.UniqueID, ':', 1 - 1)) + 1), ':', '') 'PlatformName' " +
                                    $", REPLACE(SUBSTRING(SUBSTRING_INDEX(u.UniqueID, ':', 2), LENGTH(SUBSTRING_INDEX(u.UniqueID, ':', 2 - 1)) + 1), ':', '') 'PlatformCustomer' " +
                                    $", REPLACE(SUBSTRING(SUBSTRING_INDEX(u.UniqueID, ':', 3), LENGTH(SUBSTRING_INDEX(u.UniqueID, ':', 3 - 1)) + 1), ':', '') 'PlatformRole' " +
                                    $", REPLACE(SUBSTRING(SUBSTRING_INDEX(u.UniqueID, ':', 4), LENGTH(SUBSTRING_INDEX(u.UniqueID, ':', 4 - 1)) + 1), ':', '') as 'PlatformUserID' " +
                                    $", u.OrigPasswordHash as 'PlatformPasswordHash' " +
                                    $", u.OrigPasswordSalt as 'PlatformPasswordSalt' " +
                                    $", 'SHA1' as 'PlatformPassowrdMethod' " +
                                    $", u.LastUpdated " +
                                    $", u.LastUpdated " +
                                    $"from Users u " +
                                    $"left outer join UserMappings um on um.UserID = u.UserId " +
                                    $"where um.UserId is null " +
                                    $"and u.UserID > 5200000 " +
                                    $"and u.Status = 0; ");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
        }
    }
}
