namespace WLSUser.Domain.Constants
{
    public static class JwtClaimIdentifiers
    {
        public const string
            Rol = "rol",
            Id = "id",
            SiteType = "site",
            UserName = "user_name",
            UserType = "user_type",
            RoleAccessRefIDs = "rar";
    }

    public static class JwtClaims
    {
        public const string ApiAccess = "api_access";
    }
}