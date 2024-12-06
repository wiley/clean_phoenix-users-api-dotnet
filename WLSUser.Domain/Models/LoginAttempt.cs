using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;


namespace WLSUser.Domain.Models
{
    public class LoginAttempt
    {
        [Required]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public Guid LoginAttemptID { get; set; }

        [Required]
        public int UserID { get; set; }

        [Required]
        public DateTime Attempted { get; set; }

        [Required]
        public Boolean Success { get; set; }

    }
}
