using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System.ComponentModel.DataAnnotations;

namespace WLSUser.Domain.Models
{
    public class ForgotPasswordRequestModel
    {
        [Required]
        [StringLength(100, MinimumLength = 8)]
        public string Username { get; set; }

        [Required]
        [JsonConverter(typeof(StringEnumConverter))]
        public UserTypeEnum UserType { get; set; }
    }
}