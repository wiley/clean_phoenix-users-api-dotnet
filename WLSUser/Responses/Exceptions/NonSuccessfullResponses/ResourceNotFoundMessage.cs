namespace WLSUser.Responses.Exceptions.NonSuccessfullResponses
{
    public class ResourceNotFoundMessage
    {
        private static readonly string RESOURCE_NOT_FOUND = "The requested resource does not exist.";
        public string Message { get; set; }

        public ResourceNotFoundMessage()
        {
            Message = RESOURCE_NOT_FOUND;
        }

        public ResourceNotFoundMessage(string message)
        {
            Message = message;
        }
    }
}
