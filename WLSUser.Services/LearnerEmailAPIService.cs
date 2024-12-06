using System;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using WLSUser.Domain.Models;
using WLSUser.Services.Interfaces;

namespace WLSUser.Services
{
    public class LearnerEmailAPIService : ILearnerEmailAPI
    {
        private readonly HttpClient _client;

        public LearnerEmailAPIService(HttpClient client)
        {
            _client = client;
        }

        public async Task<bool> RequestForgotPassword(string apiToken, string emailAddress)
        {
            return false;            
        }

        public async Task<bool> RequestHealthCheck()
        {
            string learnerEmailAPIToken = APIToken.LearnerEmailAPIToken;
            try
            {
                var httpRequestMessage = new HttpRequestMessage
                {
                    Method = HttpMethod.Get,
                    RequestUri = new Uri(_client.BaseAddress + "Healthz"),
                    Headers =
                    {
                        { HttpRequestHeader.Accept.ToString(), "application/json" },
                        { "LearnerEmailAPIToken", learnerEmailAPIToken }
                    }
                };

                HttpResponseMessage message = await _client.SendAsync(httpRequestMessage);

                return (message.StatusCode == HttpStatusCode.OK);
            }
            catch
            {
                return false;
            }
        }
    }
}