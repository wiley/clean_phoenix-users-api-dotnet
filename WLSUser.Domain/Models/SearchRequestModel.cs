using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System.ComponentModel.DataAnnotations;

namespace WLSUser.Domain.Models
{
    public class SearchRequestModel
    {
        [Required]
        public string Username { get; set; }

        [JsonConverter(typeof(StringEnumConverter))]
        public UserTypeEnum UserType { get; set; } = UserTypeEnum.Any;
    }
}