using Microsoft.EntityFrameworkCore.Migrations;

namespace WLSUser.Migrations
{
    public partial class UsersPopulateUserID : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql($"update Users as t1, " +
                                   $"(select id, REPLACE(SUBSTRING(SUBSTRING_INDEX(UniqueID, ':', 4), LENGTH(SUBSTRING_INDEX(UniqueID, ':', 4 - 1)) + 1), ':', '') 'NewUserID' " +
                                   $"from Users where UserType = 2 and id > 0 and userID is null) as t2 " +
                                   $"set t1.userID = t2.NewUserID " +
                                   $"where t1.id = t2.id; ");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("update Users set UserID = null where UserID is not null;");
        }
    }
}
