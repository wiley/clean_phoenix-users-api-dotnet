using System.ComponentModel.DataAnnotations;

namespace WLSUser.Domain.Models
{
    public class FindFederationRequest
    {
        [Required]
        [EmailAddress]
        public string Email { get; init; }
        public int SiteId { get; init; } = (int)SiteTypeEnum.Catalyst;
    }
}
