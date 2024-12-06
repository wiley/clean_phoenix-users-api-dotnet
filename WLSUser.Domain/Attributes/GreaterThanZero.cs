using System.ComponentModel.DataAnnotations;

namespace WLSUser.Domain.Attributes
{
    public class GreaterThanZero : ValidationAttribute
    {
        private const string DefaultErrorMessage = "Field must be greater than zero";

        protected override ValidationResult IsValid(object value, ValidationContext validationContext)
        {
            if (value != null && int.TryParse(value.ToString(), out int i) && i > 0)
            {
                return ValidationResult.Success;
            }
            else
            {
                return new ValidationResult(ErrorMessage ?? DefaultErrorMessage);
            }
        }
    }
}