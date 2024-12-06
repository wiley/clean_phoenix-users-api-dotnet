using System.ComponentModel.DataAnnotations;

namespace WLSUser.Domain.Models.V4
{
    public class UpdateUserMappingRequest
    {
        [StringLength(100)]
        public string PlatformUserId { get; set; }
        [StringLength(100)]
        public string PlatformAccountId { get; set; }
        [StringLength(255)]
        public string PlatformData { get; set; }
    }
}
