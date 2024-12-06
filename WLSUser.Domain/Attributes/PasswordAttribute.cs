using System;
using System.ComponentModel.DataAnnotations;
using System.Linq;

namespace WLSUser.Domain.Attributes
{
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Parameter,
        AllowMultiple = false)]
    public sealed class PasswordAttribute : DataTypeAttribute
    {
        private const string ValidSymbols = "~!@#$%^&*()-_+{}|[]\\:;\"'?,./";
        private readonly int _minLength;
        private readonly int _maxLength;

        public PasswordAttribute(int minLength, int maxLength) : base(DataType.Password)
        {
            _minLength = minLength;
            _maxLength = maxLength;
        }

        public override bool IsValid(object value)
        {
            if (value == null)
            {
                return false;
            }

            if (!(value is string valueAsString)) return false;

            if (string.IsNullOrWhiteSpace(valueAsString)) return false;

            if (valueAsString.Length < _minLength || valueAsString.Length > _maxLength) return false;

            bool hasUpper = false, hasLower = false, hasDigit = false, hasSymbol = false;
            foreach (char c in valueAsString)
            {
                if (hasUpper && hasLower && hasDigit && hasSymbol)
                {
                    break;
                }

                if (Char.IsDigit(c)) hasDigit = true;
                else if (Char.IsUpper(c)) hasUpper = true;
                else if (Char.IsLower(c)) hasLower = true;
                else if (ValidSymbols.Contains(c)) hasSymbol = true;
                else return false; //any other character not allowed
            }

            if (CountTrue(hasUpper, hasLower, hasDigit, hasSymbol) < 3)
            {
                return false;
            }

            return true;
        }

        private int CountTrue(params bool[] args)
        {
            return args.Count(x => x);
        }
    }
}