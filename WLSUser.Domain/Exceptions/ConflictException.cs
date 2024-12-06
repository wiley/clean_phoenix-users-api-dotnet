using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WLSUser.Domain.Exceptions
{
    [Serializable]
    public class ConflictException : Exception
    {
        private static readonly string CONFLICTREQUEST_MESSAGE = "The request contains conflict.";
        public string Message { get; set; }

        public ConflictException()
        {
            Message = CONFLICTREQUEST_MESSAGE;
        }
        public ConflictException(string message)
        {
            Message = message;
        }
    }
}
