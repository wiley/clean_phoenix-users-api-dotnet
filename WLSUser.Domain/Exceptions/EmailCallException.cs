using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WLSUser.Domain.Exceptions
{
    [Serializable]
    public class EmailCallException : Exception
    {
        public EmailCallException(string message = "") : base(message) { }
    }
}
