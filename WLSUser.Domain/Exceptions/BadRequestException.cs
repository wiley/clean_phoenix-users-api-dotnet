using System;

namespace WLSUser.Domain.Exceptions
{
    [Serializable]
    public class BadRequestException : Exception
    {
        private static readonly string BADREQUEST_MESSAGE = "The request contains invalid data.";
        public string Message { get; set; }

        public BadRequestException()
        {
            Message = BADREQUEST_MESSAGE;
        }
        public BadRequestException(string message)
        {
            Message = message;
        }
    }
}
