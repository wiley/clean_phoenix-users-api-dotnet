using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System.ComponentModel.DataAnnotations;

namespace WLSUser.Domain.Models
{
    public class RecoverPasswordRequestV4 
    {
        [Required(AllowEmptyStrings = false)]
        public string Username { get; set; }

        [JsonConverter(typeof(StringEnumConverter))]
        public SiteTypeEnum SiteType { get; set; } = SiteTypeEnum.Any;
    }
}