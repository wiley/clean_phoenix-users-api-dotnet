using System;
using System.ComponentModel.DataAnnotations;

namespace WLSUser.Domain.Models.Authentication
{
    public class AuthResponse
    {
        [Required]
        public string AccessToken { get; set; }
        public DateTime Expires { get; set; }
        public string RefreshToken { get; set; }
        public DateTime RefreshExpires { get; set; }
    }
}
