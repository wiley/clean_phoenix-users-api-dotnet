namespace WLSUser.Domain.Models
{
    public class RecoverPasswordObject
    {
        public string Key { get; set; }
        public ValidateObject Data { get; set; }
    }
}