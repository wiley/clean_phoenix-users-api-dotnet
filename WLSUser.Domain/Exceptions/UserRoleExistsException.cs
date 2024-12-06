using System;

namespace WLSUser.Domain.Exceptions
{
    [Serializable]
    public class UserRoleExistsException : Exception
    {
        public UserRoleExistsException() { }
    }
}