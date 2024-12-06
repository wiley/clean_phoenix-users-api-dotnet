using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WLSUser.Domain.Models.V4
{
    public class CreateUserMappingRequest
    {
        public string PlatformName { get; set; }
        public string PlatformCustomer { get; set; }
        public string PlatformRole { get; set; }
        public string PlatformUserId { get; set; }
        public string PlatformAccountId { get; set; }
        public string PlatformData { get; set; }
    }
}
