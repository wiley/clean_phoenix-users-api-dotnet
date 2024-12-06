using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System.ComponentModel.DataAnnotations;

namespace WLSUser.Domain.Models
{
    public class UpdateUserRequestModel
    {
        [Required]
        [StringLength(255)]
        public string UniqueID { get; set; }

        [StringLength(255)]
        public string Username { get; set; }

        [StringLength(50)]
        public string Hash { get; set; }

        [StringLength(50)]
        public string Salt { get; set; }

        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string Email { get; set; }

        [JsonConverter(typeof(StringEnumConverter))]
        public UserTypeEnum UserType { get; set; }

        public int Status { get; set; }
    }
}