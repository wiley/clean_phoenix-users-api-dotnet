using Microsoft.EntityFrameworkCore.Migrations;

namespace WLSUser.Migrations
{
    public partial class loginAttempts_Trigger : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql($"CREATE TRIGGER before_insert_loginattempts " +
                                 $"BEFORE INSERT ON LoginAttempts " +
                                 $"FOR EACH ROW " +
                                 $"BEGIN " +
                                 $"IF new.LoginAttemptID IS NULL THEN " +
                                 $"SET new.LoginAttemptID = uuid(); " +
                                 $"END IF; " +
                                 $"END " +
                                 $"; ; ");

        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("drop trigger wlsusers.before_insert_loginattempts;");
        }
    }
}
