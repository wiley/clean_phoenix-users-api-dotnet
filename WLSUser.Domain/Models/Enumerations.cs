namespace WLSUser.Domain.Models
{
    public enum SiteTypeEnum
    {
        Any = 0,
        Catalyst = 1,
        Epic = 2,
        LPI = 3,
        WLS = 4, // All of the Unit Test data and the seeded data in the database has this "wls:singleton:facilitator:93xxxx" for example, and the code in UserUniqueID needs a value to support this even if it never is a real site again "Workplace 3.0=WLS originally"
        LPISelfStudent = 5,
        CK = 6,
        Wiley = 7
    }

    public enum UserStatus
    {
        Active = 0,
        Inactive = 1
    }

    public enum UserTypeEnum
    {
        Any = 0,
        EPICAdmin = 1,
        EPICLearner = 2,
        PACAdmin = 3,
        PACLearner = 4,
        XYZFacilitator = 5,
        //CatalystDemo = 6, //CatalystDemo is just a role -- deleted the single "catalyst:singleton:demo:" mateo record
        LPIFacilitator = 7,
        LPILearner = 8
    }

    public enum PlatformRole
    {
        Learner = 0,
        Admin = 1,
        Facilitator = 2
    }

    public enum BrandTypeEnum
    {
        EverythingDisc = 1,
        PXTSelect = 2,
        TheLeadershipChallenge = 3
    }

    public enum RoleTypeEnum
    {
        CatalystLearner = 1,
        CatalystFacilitator = 2,
        CatalystDemo = 3,
        LPILeader = 4,
        LPIObserver = 5,
        LPIFacilitator = 6
    }

    public enum AccessTypeEnum
    {
        Organization = 1,
        Account = 2
    }

    public class DependenciesTypes
    {
        public const string MySql = "MySql";
        public const string LearnerEmailAPI = "LearnerEmailAPI";
        public const string RedisConnection = "RedisConnection";
        public const string RedisReadWrite = "RedisReadWrite";
    }

    public class HealthResults
    {
        public const string OK = "OK";
        public const string Unavailable = "Unavailable";
        public const string Fail = "Fail";
    }

    public class APIToken
    {
        public const string UsersAPIToken = "";
        public const string RootBeerUsersAPIToken = "";
        public const string LearnerEmailAPIToken = "";
        public const string RootBeerLearnerEmailAPIToken = "";
        public const string CSCToken = "342091BP-82YF-XX4R-LWVT-IO6K7DHILN1T";
    }

    public enum TokenTypeEnum
    {
        AccessToken = 0,
        RefreshToken = 1,
        ExchangeToken = 2
    }

    public class FederationConstants
    {
        public const string ScopeOpenID = "openid";
        public const string ScopeEmail = "email";
        public const string ScopeProfile = "profile";
        public const string DefaultScope = ScopeOpenID;

        public const string AuthMethodClientSecretBasic = "client_secret_basic";
        public const string AuthMethodClientSecretPost = "client_secret_post";
        public const string DefaultAuthMethod = AuthMethodClientSecretPost;
    }

    public class RedisTestConstants
    {
        public const int ExpirySeconds = 1;
        public const string Key = "ping";
        public const string Value = "pong";
    }

    public enum FunctionType
    {
        Default = 0,
        ResetPassword = 1
    }
}