using System;
using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;

namespace WLSUser.Domain.Attributes
{
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Parameter,
        AllowMultiple = false)]
    public sealed class EmailAttribute : ValidationAttribute
    {
        private const string BASE_EMAIL_VALIDATION_EXPRESSION = @"^[\#\$\%\&\'\*\+\/\=\?\^\`\{\|\}\~\!A-Za-z0-9_\.-]+@[A-Za-z0-9_\.-]+\.[A-Za-z0-9_][A-Za-z0-9_][A-Za-z0-9_]*$";
        private static Regex validEmailRegEx = new Regex(BASE_EMAIL_VALIDATION_EXPRESSION);

        public override bool IsValid(object value)
        {
            if (value == null)
            {
                return false;
            }
            else if (value.GetType() != typeof(string))
                return false;
            else
            {
                string emailAddress = value.ToString();

                if (emailAddress.Contains("..") || //The regular expression can't pickup on this typo..., so just return false
                    emailAddress.Contains(".@") ||
                    emailAddress.Contains("@.")  ||
                    emailAddress.StartsWith(".") ||
                    emailAddress.EndsWith("."))
                {
                    return false;
                }

                return validEmailRegEx.IsMatch(emailAddress);
            }
        }
    }
}
