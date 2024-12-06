using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace WLSUser.Domain.Models
{
    public class AddUserRoleAccessRequest
    {
        [Required]
        [StringLength(255, MinimumLength = 15)]
        public string UniqueID { get; set; }

        [Required]
        public int AccessTypeID { get; set; }

        [Required]
        public List<int> AccessRefIDs { get; set; }

        [Required]
        [StringLength(255, MinimumLength = 15)]
        public string GrantedBy { get; set; }
    }
}