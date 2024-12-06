using System;

namespace WLSUser.Domain.Exceptions
{
    [Serializable]
    public class AuthenticationFailedException : Exception
    {
        public AuthenticationFailedException(string message = "") : base(message) { }
    }
}