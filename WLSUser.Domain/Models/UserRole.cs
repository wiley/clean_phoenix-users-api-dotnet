using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WLSUser.Domain.Models
{
    public class UserRole
    {
        [Required]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int UserRoleID { get; set; }

        [Required]
        public int UserID { get; set; }

        [Required]
        public int RoleTypeID { get; set; }

        [Required]
        public DateTime Created { get; set; }
    }
}