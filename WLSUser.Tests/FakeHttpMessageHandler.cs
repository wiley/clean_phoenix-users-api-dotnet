using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace WLSUser.Tests
{
    public class FakeHttpMessageHandler : DelegatingHandler
    {
        private HttpResponseMessage _fakeResponse;
        private string _apiHeader;
        private string _apiToken;

        public FakeHttpMessageHandler(string apiHeader, string apiToken, HttpResponseMessage responseMessage)
        {
            _fakeResponse = responseMessage;
            _apiHeader = apiHeader;
            _apiToken = apiToken;
        }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            bool headerMatch = false;
            foreach (KeyValuePair<string, IEnumerable<string>> header in request.Headers)
            {
                if (header.Key == _apiHeader)
                {
                    List<string> values = new List<string>(header.Value);
                    if (values.Contains(_apiToken))
                        headerMatch = true;
                }
            }

            if (!string.IsNullOrEmpty(_apiHeader) && !headerMatch)
                return await Task.FromResult(new HttpResponseMessage(HttpStatusCode.Unauthorized));

            return await Task.FromResult(_fakeResponse);
        }
    }

    public class FakeHttpMessageHandlerSimple : DelegatingHandler
    {
        private HttpResponseMessage _fakeResponse;

        public FakeHttpMessageHandlerSimple(HttpResponseMessage responseMessage)
        {
            _fakeResponse = responseMessage;
        }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            return await Task.FromResult(_fakeResponse);
        }
    }
}