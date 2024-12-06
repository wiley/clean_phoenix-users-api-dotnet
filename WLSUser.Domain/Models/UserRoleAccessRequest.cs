using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace WLSUser.Domain.Models
{
    public class UserRoleAccessRequest
    {
        [Required]
        public int UserID { get; set; }

        [Required]
        public int AccessTypeID { get; set; }

        [Required]
        public int RoleTypeID { get; set; }

        [Required]
        public List<int> AccessRefIDs { get; set; }

        [Required]
        public int GrantedBy { get; set; }
    }
}