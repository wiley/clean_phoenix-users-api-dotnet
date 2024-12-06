using System;

namespace WLSUser.Domain.Models
{
    public class UserUniqueID
    {
        public UserUniqueID(string uniqueID)
        {
            var parts = uniqueID?.Split(":");

            if (parts == null ||
                parts.Length != 4 ||
                !Enum.TryParse<SiteTypeEnum>(parts[0], true, out SiteTypeEnum siteType) ||
                String.IsNullOrEmpty(parts[1]) ||
                String.IsNullOrEmpty(parts[2]) ||
                !int.TryParse(parts[3], out int accountID))
                throw new ArgumentException();

            SiteType = siteType;
            Instance = parts[1];
            AccountType = parts[2].ToLower();
            AccountID = accountID;
        }
        /// <summary>
        /// A company or app like "EPIC", "PAC", "WLS", "CK", "CKLS"
        /// </summary>
        public SiteTypeEnum SiteType { get; set; }

        /// <summary>
        /// "singleton" for one main app or a client name like "CocaCola" or "Loreal"
        /// </summary>
        public string Instance { get; set; } = "singleton";
        /// <summary>
        /// A "learner", "admin", or "facilitator"
        /// </summary>
        public string AccountType { get; set; }
        /// <summary>
        /// a localized app id that is unique to the site:instance:type
        /// </summary>
        public int AccountID { get; set; }

        public override string ToString()
        {
            var siteName = Enum.GetName(typeof(SiteTypeEnum), SiteType).ToLower();

            return $"{siteName}:{Instance}:{AccountType.ToLower()}:{AccountID}";
        }

        public  string PlatformName => SiteType.ToString().ToLower();

        public string PlatformUserID => AccountID.ToString();


        public string PlatformPasswordMethod()
        {
            try
            {
                Enum.TryParse(AccountType, true, out PlatformRole role);
                switch (SiteType)
                {
                    case SiteTypeEnum.Epic:
                        switch (role)
                        {
                            case PlatformRole.Admin:
                                return "SHA256";
                            case PlatformRole.Learner:
                                return "SHA1";
                            default:
                                return "";
                        }
                    default:
                        return "";
                }
            }
            catch
            {
                return "";
            }
        }
    }
}
