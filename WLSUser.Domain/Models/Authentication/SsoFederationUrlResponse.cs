using System.ComponentModel.DataAnnotations;

namespace WLSUser.Domain.Models.Authentication
{
    public class SsoFederationUrlResponse
    {
        [Required]
        public string Url { get; set; }
    }
}
