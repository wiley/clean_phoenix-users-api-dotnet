using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System;
using System.ComponentModel.DataAnnotations;

namespace WLSUser.Domain.Models
{
    public class UserMapping
    {
        public int Id { get; set; }

        //UserID from UserModel
        
        public int UserId { get; set; }

        //epic/lpi/ck/pac
        [Required]
        [StringLength(200)]
        public string PlatformName { get; set; }

        //singleton/ckCustomer
        [Required]
        [StringLength(200)]
        public string PlatformCustomer { get; set; }

        //learner/admin/user
        [Required]
        [StringLength(200)]
        public string PlatformRole { get; set; }

        //User related value in the platform. example would be the UserId value from the EPIC Users table
        [StringLength(100)]
        [Required]
        public string PlatformUserId { get; set; }

        //Account User is connected to in the platform. EPIC example is the Accounts or MYEd_Accounts Account_ID value
        
        [StringLength(100)]
        public string PlatformAccountId { get; set; }

        //json object - account access roles for lpi
        [StringLength(255)]
        public string PlatformData { get; set; }

        //platforms hashed password
        [StringLength(50)]
        public string PlatformPasswordHash { get; set; }

        //platforms password salt
        [StringLength(50)]
        public string PlatformPasswordSalt { get; set; }

        //SHA1, SHA256, 
        [StringLength(20)]
        public string PlatformPassowrdMethod { get; set; }

        public DateTime Created { get; set; } = DateTime.Now;
        public int CreatedBy { get; set; }

        public DateTime Updated { get; set; } = DateTime.Now;
        public int UpdatedBy { get; set; }

        public string CreateUniqueID()
        {
            return $"{PlatformName}:{PlatformCustomer}:{PlatformRole.ToLower()}:{UserId}";
        }
        public UserTypeEnum CreateUserType()
        {
            string platformRole = PlatformRole.ToLower();
            switch (PlatformName)
            {
                case "epic":
                    switch (platformRole)
                    {
                        case "learner":
                            return UserTypeEnum.EPICLearner;
                        case "admin":
                            return UserTypeEnum.EPICAdmin;
                        default:
                            throw new NotImplementedException();
                    }
                case "lpi":
                    switch (platformRole)
                    {
                        case "facilitator":
                            return UserTypeEnum.LPIFacilitator;
                        default:
                            throw new NotImplementedException();
                    }
                default:
                    throw new NotImplementedException();
            }
        }
    }
}
