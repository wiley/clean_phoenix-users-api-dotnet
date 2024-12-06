using System;

namespace WLSUser.Domain.Exceptions
{
    [Serializable]
    public class PasswordExpiredException : Exception
    {
        public PasswordExpiredException(string message = "") : base(message) { }
    }
}
