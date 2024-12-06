using System.ComponentModel.DataAnnotations;

namespace WLSUser.Domain.Models
{
    public class UserRoleRequest
    {
        [Required]
        public int UserID { get; set; }

        [Required]
        public int RoleTypeID { get; set; }
    }
}