using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System.Collections.Generic;

namespace WLSUser.Domain.Models
{
    public class UniqueIDSearchResponseModel
    {
        public string Username { get; set; }

        public string UniqueID { get; set; }

        [JsonConverter(typeof(StringEnumConverter))]
        public UserTypeEnum UserType { get; set; }

        public string FirstName { get; set; }

        public string LastName { get; set; }

        public List<UserRoleResponse> UserRoles { get; set; }
    }
}