using Newtonsoft.Json;
using System.ComponentModel.DataAnnotations;

namespace WLSUser.Domain.Models
{
    public class LoginRequestV4
    {
        [Required(AllowEmptyStrings = false)]
        public string Username { get; set; }

        [Required(AllowEmptyStrings = false)]
        public string Password { get; set; }
    }
    public class LoginAPITokenRequestV4
    {
        [Required(AllowEmptyStrings = false)]
        public string Username { get; set; }

    }

}
