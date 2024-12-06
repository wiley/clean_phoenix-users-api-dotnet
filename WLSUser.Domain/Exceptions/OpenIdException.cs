using System;

namespace WLSUser.Domain.Exceptions
{
    [Serializable]
    public class OpenIdException : Exception
    {
        public OpenIdException(string message = "") : base(message) { }
    }
}
