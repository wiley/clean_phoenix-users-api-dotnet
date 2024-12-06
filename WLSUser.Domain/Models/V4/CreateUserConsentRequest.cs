using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WLSUser.Domain.Models.V4
{
    public class CreateUserConsentRequest
    {
        [Required, StringLength(50,MinimumLength =5)]
        public string PolicyType { get; set; }
        [Required, Range(0.1, double.MaxValue, ErrorMessage = "Version must be up to 0.1 .")]
        public double Version { get; set; }
        [Required, StringLength(20, MinimumLength = 1)]
        public string Status { get; set; }
    }
}
