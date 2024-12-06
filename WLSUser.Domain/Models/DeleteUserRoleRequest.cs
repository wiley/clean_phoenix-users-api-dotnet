using System.ComponentModel.DataAnnotations;

namespace WLSUser.Domain.Models
{
    public class DeleteUserRoleRequest
    {
        [Required]
        [StringLength(255, MinimumLength = 15)]
        public string UniqueID { get; set; }

        [Required]
        public int RoleTypeID { get; set; }
    }
}