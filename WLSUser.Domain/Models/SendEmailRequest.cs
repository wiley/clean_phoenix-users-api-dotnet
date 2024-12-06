using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System.Collections.Generic;

namespace WLSUser.Domain.Models
{
    public class SendEmailRequest
    {
        [JsonProperty(PropertyName = "siteType")]
        [JsonConverter(typeof(StringEnumConverter))]
        public SiteTypeEnum SiteType { get; set; }
        [JsonProperty(PropertyName = "reSend")]
        public bool ReSend { get; set; }
        [JsonProperty(PropertyName = "template")]
        public string Template { get; set; }

        [JsonProperty(PropertyName = "sendTo")]
        public List<SendEmailBlock> SendTo { get; set; }
    }
}