using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

using System.ComponentModel.DataAnnotations;

namespace WLSUser.Domain.Models
{
    public class AddUserRoleAccessRequestV3
    {
        [Required]
        [StringLength(255, MinimumLength = 15)]
        public string UniqueID { get; set; }

        [Required]
        [JsonConverter(typeof(StringEnumConverter))]
        public RoleTypeEnum RoleType { get; set; }

        [Required]
        [JsonConverter(typeof(StringEnumConverter))]
        public AccessTypeEnum AccessType { get; set; }

        [Required]
        public int[] AccessRefIDs { get; set; }

        [Required]
        [StringLength(255, MinimumLength = 15)]
        public string GrantedBy { get; set; }
    }
}
