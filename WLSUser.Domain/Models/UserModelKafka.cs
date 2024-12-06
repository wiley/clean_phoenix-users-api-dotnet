using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using WLSUser.Domain.Models.V4;

namespace WLSUser.Domain.Models
{
    public class UserModelKafka
    {
        public int UserId { get; set; }

        [Required]
        [StringLength(255)]
        public string Username { get; set; }

        [Required]
        [StringLength(100)]
        public string FirstName { get; set; }

        [Required]
        [StringLength(100)]
        public string LastName { get; set; }

        [StringLength(255)]
        public string Email { get; set; } = "";

        public int Status { get; set; } = 0;

        public DateTime LastUpdated { get; set; } = DateTime.Now;

        [StringLength(50)]
        public string Language { get; set; }

        [StringLength(50)]
        public string Region { get; set; }

        public DateTime CreatedAt { get; set; }

        public int CreatedBy { get; set; }

        public int UpdatedBy { get; set; }

        public List<UserMappingKafka> UserMappings { get; set; }
    }
}