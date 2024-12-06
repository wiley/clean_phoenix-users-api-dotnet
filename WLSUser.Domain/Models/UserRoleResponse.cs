using System.Collections.Generic;

namespace WLSUser.Domain.Models
{
    public class UserRoleResponse
    {
        public UserRole UserRole { get; set; }
        public RoleType RoleType { get; set; }
        public List<UserRoleAccess> UserRoleAccess { get; set; }
    }
}