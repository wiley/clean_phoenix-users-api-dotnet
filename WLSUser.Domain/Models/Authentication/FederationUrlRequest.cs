using Newtonsoft.Json;
using System.ComponentModel.DataAnnotations;

namespace WLSUser.Domain.Models.Authentication
{
    public class FederationUrlRequest
    {
        [Required]
        public string AuthUrl { get; set; }
        [Required]
        public string RedirectUrl { get; set; }
        public int SiteId { get; set; } = (int)SiteTypeEnum.Catalyst;
    }
}
