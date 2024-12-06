using System;
using System.Collections.Generic;

namespace WLSUser.Domain.Models
{
    public class LoginResponseModel
    {
        public string Status { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string[] UniqueUserIDs { get; set; }

        public List<UserRoleResponse> UserRoles { get; set; }

        public string State { get; set; }
        public string UserName { get; set; }
    }
}