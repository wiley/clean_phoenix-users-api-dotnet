using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System;
using System.ComponentModel.DataAnnotations;

namespace WLSUser.Domain.Models
{
    public class UserMappingKafka
    {
        public int Id { get; set; }
        
        public int UserId { get; set; }

        [Required]
        [StringLength(200)]
        public string PlatformName { get; set; }

        [Required]
        [StringLength(200)]
        public string PlatformCustomer { get; set; }

        [Required]
        [StringLength(200)]
        public string PlatformRole { get; set; }

        [StringLength(100)]
        [Required]
        public string PlatformUserId { get; set; }
        
        [StringLength(100)]
        public string PlatformAccountId { get; set; }

        [StringLength(255)]
        public string PlatformData { get; set; }

        public DateTime Created { get; set; } = DateTime.Now;
        
        public int CreatedBy { get; set; }

        public DateTime Updated { get; set; } = DateTime.Now;

        public int UpdatedBy { get; set; }
    }
}
