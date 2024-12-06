using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Reflection;

namespace WLSUser.Domain.Models
{
    public class LoginRequestModel
    {
        [Required]
        [StringLength(100, MinimumLength = 8)]
        public string Username { get; set; }

        [Required]
        [StringLength(50, MinimumLength = 8)]
        public string Password { get; set; }

        [Required]
        [JsonConverter(typeof(StringEnumConverter))]
        public UserTypeEnum UserType { get; set; }
    }

    public class LoginSsoRequestModel
    {
        [Required]
        [StringLength(50, MinimumLength = 3)]
        public string FederationName { get; set; }
        /// <summary>
        /// Code value is defined by individual OpenID Connect provider and may be any size, but is required
        /// </summary>
        [Required]
        [StringLength(int.MaxValue, MinimumLength = 1)]
        public string Code { get; set; }
        /// <summary>
        /// State identifier is defined by UsersAPI and at the moment the value is simply a lower-case Guid string
        /// </summary>
        [Required]
        [StringLength(100, MinimumLength = 36)]
        public string State { get; set; }
        /// <summary>
        /// Site identifier is defined by individual OpenID Connect provider and is required
        /// </summary>
        [Required]
        public int SiteId { get; set; } = (int)SiteTypeEnum.Catalyst;
    }
}
