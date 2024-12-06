using System;

namespace WLSUser.Domain.Models
{
    public class ValidateObject
    {
        public int UserId { get; set; }
        public string FunctionType { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? ProcessedOn { get; set; } = null;
    }
}