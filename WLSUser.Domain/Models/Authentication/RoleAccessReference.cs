using System.Collections.Generic;

namespace WLSUser.Domain.Models.Authentication
{
    public class RoleAccessReference
    {
        public RoleType RoleType { get; set; }

        public AccessType AccessType { get; set; }

        public List<UserRoleAccess> UserRoleAccessList { get; set; }
    }
}
