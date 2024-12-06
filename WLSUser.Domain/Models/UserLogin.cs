using System;
using System.ComponentModel.DataAnnotations;

namespace WLSUser.Domain.Models
{
    public class UserLogin
    {
        public int Id { get; set; }

        [Required]
        [StringLength(255)]
        public string UniqueID { get; set; }

        [Required]
        [StringLength(255)]
        public string Username { get; set; }

        [Required]
        public DateTime LastLogin { get; set; }
    }
}
