using System;

namespace WLSUser.Domain.Exceptions
{
    public class UserRoleAccessExistsException : Exception
    {
        public UserRoleAccessExistsException(string message = "") : base(message) { }
    }
}