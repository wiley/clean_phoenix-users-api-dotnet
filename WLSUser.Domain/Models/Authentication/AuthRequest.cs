using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

using System.ComponentModel.DataAnnotations;
using WLSUser.Domain.Attributes;

namespace WLSUser.Domain.Models.Authentication
{
    public class AuthRequest
    {
        [Required]
        [StringLength(100, MinimumLength = 8)]
        public string Username { get; set; }
        [Required]
        [Password(8, 50)]
        public string Password { get; set; }
        [Required]
        [JsonConverter(typeof(StringEnumConverter))]
        public UserTypeEnum UserType { get; set; }
        [Required]
        [JsonConverter(typeof(StringEnumConverter))]
        public SiteTypeEnum SiteType { get; set; }
        public bool PersistToken { get; set; }
    }
}

