using System;
using System.ComponentModel.DataAnnotations;

namespace WLSUser.Domain.Models
{
    public class SSOState
    {
        /// <summary>
        /// Database record identifier
        /// </summary>
        public int Id { get; set; }
        /// <summary>
        /// Key initially just storing a Guid
        /// </summary>
        [Required]
        [StringLength(100)]
        public string Key { get; set; }
        /// <summary>
        /// Data provided by the client
        /// </summary>
        [StringLength(255)]
        public string Data { get; set; }
        /// <summary>
        /// State will be valid for 30 minutes after Created timestamp
        /// </summary>
        [Required]
        public DateTime Created { get; set; }
    }
}
