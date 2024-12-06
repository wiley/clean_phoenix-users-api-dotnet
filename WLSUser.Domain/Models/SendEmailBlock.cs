using Newtonsoft.Json;
using System.Collections.Generic;

namespace WLSUser.Domain.Models
{
    public class SendEmailBlock
    {
        [JsonProperty(PropertyName = "to")]
        public List<UserEmailAddress> To { get; set; }

        [JsonProperty(PropertyName = "cc")]
        public List<UserEmailAddress> Cc { get; set; }

        [JsonProperty(PropertyName = "bcc")]
        public List<UserEmailAddress> Bcc { get; set; }

        [JsonProperty(PropertyName = "templateData")]
        public Dictionary<string, string> TemplateData { get; set; }
    }
}
