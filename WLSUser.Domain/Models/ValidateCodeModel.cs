using System.ComponentModel.DataAnnotations;
﻿using Newtonsoft.Json;

namespace WLSUser.Domain.Models
{
    public class ValidateCode
    {
        public int UserId { get; set; } = 0;
        public string Code { get; set; }
    }
    public class ValidateCodeResponse
    {
        public int? UserId { get; set; }
        [JsonIgnore]
        public bool Permission { get; set; } = false;
    }

    public class UserChangePasswordRequest
    {
        public string Code { get; set; }
        public string NewPassword { get; set; }
    }

    public class UserChangePasswordOnlyRequest
    {

        [MinLength(8)]
        public string Password { get; set; }
    }
}
