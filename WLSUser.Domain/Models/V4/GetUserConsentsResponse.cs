using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WLSUser.Domain.Models.V4
{
    public class GetUserConsentsResponse
    {
        public int Count { get; set; }
        public IEnumerable<UserConsent> Items { get; set; }
    }
}
