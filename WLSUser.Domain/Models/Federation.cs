using System.ComponentModel.DataAnnotations;

namespace WLSUser.Domain.Models
{
    // This class have 2 Indexes: Name and SiteId
    public class Federation
    {
        public int Id { get; set; }
        public int OrganizationId { get; set; }
        [Required, MaxLength(25)]
        public string Name { get; set; }
        [Required, MaxLength(200)]
        public string OpenIdAuthInitUrl { get; set; }
        [Required, MaxLength(200)]
        public string OpenIdTokenUrl { get; set; }
        [Required, MaxLength(50)]
        public string OpenIdClientId { get; set; }
        [Required, MaxLength(768)]
        public string OpenIdClientSecret { get; set; }
        [Required, MaxLength(128)]
        public string RedirectUrl { get; set; }
        [MaxLength(25)]
        public string AlmFederationName { get; set; }
        [Required, MaxLength(128)]
        public string EmailDomain { get; set; }
        [Required]
        [StringLength(20)]
        public string Scope { get; set; } = FederationConstants.DefaultScope;
        [Required]
        [StringLength(20)]
        public string AuthMethod { get; set; } = FederationConstants.DefaultAuthMethod;
        public int SiteId { get; set; } = (int)SiteTypeEnum.Catalyst;
        [Required]
        [StringLength(512)]
        public string TestUsers { get; set; }
    }
}