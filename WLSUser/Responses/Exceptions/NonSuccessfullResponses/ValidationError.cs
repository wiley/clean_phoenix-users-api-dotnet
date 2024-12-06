namespace WLSUser.Responses.Exceptions.NonSuccessfullResponses
{
    public class ValidationError
    {
        public string Name { get; set; }

        public string Origin { get; set; }

        public string Type { get; set; }

        public string Detail { get; set; }

        public ValidationError(string name, string origin, string type, string detail)
        {
            Name = name;
            Origin = origin;
            Type = type;
            Detail = detail;
        }
    }
}
