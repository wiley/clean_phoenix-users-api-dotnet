using System.ComponentModel.DataAnnotations;

namespace WLSUser.Domain.Models
{
    public class Brand
    {
        [Required]
        public int BrandID { get; set; }

        [Required]
        [MaxLength(245)]
        public string BrandName { get; set; }
    }
}