using Newtonsoft.Json;
using System.ComponentModel.DataAnnotations;

namespace WLSUser.Domain.Models.V4
{
    public class UnauthorizedResponseV4
    {
        [Required(AllowEmptyStrings = false)]
        public string Message { get; set; }
    }

}