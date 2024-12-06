using System.ComponentModel.DataAnnotations;

namespace WLSUser.Domain.Models.V4
{
    public class SearchRequestV4Model
    {
        [StringLength(100, MinimumLength = 8)]
        public string Username { get; set; }
        public string Email { get; set; }
        public string Login { get; set; }
    }
}
