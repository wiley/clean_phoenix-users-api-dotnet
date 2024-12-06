using System.ComponentModel.DataAnnotations;

namespace WLSUser.Domain.Models
{
    public class RoleType
    {
        [Required]
        public int RoleTypeID { get; set; }

        [Required]
        public int BrandID { get; set; }

        [Required]
        [MaxLength(245)]
        public string RoleName { get; set; }
    }
}