using System.Collections.Generic;

namespace WLSUser.Domain.Models.Authentication
{
    public class LoginResponse
    {
        public string Status { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string UniqueID { get; set; }
        public string UserName { get; set; }
        public UserTypeEnum UserType { get; set; }
        public List<string> Roles { get; set; } = new List<string>();
    }
}
