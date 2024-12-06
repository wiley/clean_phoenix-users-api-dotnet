using Newtonsoft.Json;

namespace WLSUser.Domain.Models
{
    public class UserEmailAddress
    {
        [JsonProperty(PropertyName = "userId")]
        public int UserId { get; set; }
        [JsonProperty(PropertyName = "emailName")]
        public string EmailName { get; set; }
        [JsonProperty(PropertyName = "emailAddress")]
        public string EmailAddress { get; set; }
    }
}