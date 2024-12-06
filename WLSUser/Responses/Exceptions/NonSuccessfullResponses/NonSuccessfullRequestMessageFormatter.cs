using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace WLSUser.Responses.Exceptions.NonSuccessfullResponses
{
    public static class NonSuccessfullRequestMessageFormatter
    {
        private static readonly string REQUEST_CONTAINS_INVALID_DATA = "The request contains invalid data.";
        private static readonly string ERROR_ORIGIN_BODY = "body";
        private static readonly string ERROR_TYPE_INVALID_VALUE = "errors/invalid-value";

        public static BadRequestMessage FormatBadRequestResponse(ModelStateDictionary modelState)
        {
            BadRequestMessage badRequestMessage = new()
            {
                Message = REQUEST_CONTAINS_INVALID_DATA
            };

            foreach (string modelStateKey in modelState.Keys)
                foreach (var error in modelState[modelStateKey].Errors)
                    badRequestMessage.Errors.Add(new ValidationError(modelStateKey, ERROR_ORIGIN_BODY, ERROR_TYPE_INVALID_VALUE, error.ErrorMessage));

            return badRequestMessage;
        }

        public static ResourceNotFoundMessage FormatResourceNotFoundResponse()
        {
            return new ResourceNotFoundMessage();
        }
    }
}
