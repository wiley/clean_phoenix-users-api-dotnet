using System.ComponentModel.DataAnnotations;

namespace WLSUser.Domain.Models
{
    public class AccessType
    {
        [Required]
        public int AccessTypeID { get; set; }

        [Required]
        [MaxLength(245)]
        public string AccessTypeName { get; set; }
    }
}