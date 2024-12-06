using System.ComponentModel.DataAnnotations;

namespace WLSUser.Domain.Models.Authentication
{
    public class JwtRequestFromCode
    {
        [Required]
        public string Code { get; set; }
        [Required]
        public string State { get; set; }
    }
}
