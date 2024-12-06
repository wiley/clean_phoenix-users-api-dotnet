using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using WLSUser.Domain.Models.V4;

namespace WLSUser.Domain.Models
{
    public class UserModel
    {
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int UserID { get; set; }

        [Required]
        [StringLength(255)]
        public string Username { get; set; }

        [StringLength(100)]
        public string FirstName { get; set; }

        [StringLength(100)]
        public string LastName { get; set; }

        [StringLength(255)]
        public string Email { get; set; } = "";

        [Required]
        [JsonConverter(typeof(StringEnumConverter))]
        [Obsolete("Should use UserMapping CreateUserType() instead", false)]
        public UserTypeEnum UserType { get; set; }

        [Obsolete("Should use UserMapping PlatformPasswordSalt instead", false)]
        [StringLength(50)]
        public string OrigPasswordSalt { get; set; }

        [Obsolete("Should use UserMapping PlatformPasswordHash instead", false)]
        [StringLength(50)]
        public string OrigPasswordHash { get; set; }

        [StringLength(50)]
        public string StrongPasswordSalt { get; set; }

        [StringLength(50)]
        public string StrongPasswordHash { get; set; }

        public DateTime? StrongPasswordSet { get; set; }

        public DateTime? StrongPasswordGoodUntil { get; set; }

        public int Status { get; set; } = 0;

        public DateTime LastUpdated { get; set; } = DateTime.Now;

        [StringLength(255)]
        public string AlmId { get; set; }

        public DateTime? DataConsentDate { get; set; }

        public DateTime? PrivacyAcceptDate { get; set; }

        [StringLength(255)]
        public string RecoveryEmail { get; set; } = "";

        [StringLength(50)]
        public string Language { get; set; }

        [StringLength(50)]
        public string PhoneNumber { get; set; }

        [StringLength(120)]
        public string AddressLine1 { get; set; }

        [StringLength(100)]
        public string AddressLine2 { get; set; }

        [StringLength(50)]
        public string City { get; set; }

        [StringLength(50)]
        public string Region { get; set; }

        [StringLength(50)]
        public string Country { get; set; }

        [StringLength(50)]
        public string PostalCode { get; set; }
        [StringLength(2048)]
        public string AvatarUrl { get; set; }
        public DateTime CreatedAt { get; set; }
        public int CreatedBy { get; set; }
        public int UpdatedBy { get; set; }

        public List<UserMapping> UserMappings { get; set; } = new List<UserMapping>();
        public List<UserConsent> UserConsents { get; set; } = new List<UserConsent>();

        public string UniqueID 
        {
            get {
                return string.Format("epic:singleton:learner:{0}", UserID);
            }
        }
    }
}