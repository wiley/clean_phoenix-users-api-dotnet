using System.ComponentModel.DataAnnotations;

namespace WLSUser.Domain.Models
{
    public class UniqueIDSearchRequestModel
    {
        [Required]
        public string UniqueID { get; set; }
    }
}