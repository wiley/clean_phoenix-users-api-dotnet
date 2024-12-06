using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WLSUser.Domain.Models.V4
{
    public class UserConsent
    {
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }
        [Required]
        public int UserId { get; set; }
        [Required]
        public DateTime CreatedAt { get; set; }
        [Required]
        public int CreatedBy { get; set; }
        [Required]
        public DateTime UpdatedAt { get; set; }
        [Required]
        public int UpdatedBy { get; set; }
        [Required, StringLength(50)]
        public string PolicyType { get; set; }
        [Required]
        public double Version { get; set; }
        public DateTime? AcknowledgedAt { get; set; }
        [Required, StringLength(20)]
        public string Status { get; set; }
    }
}
