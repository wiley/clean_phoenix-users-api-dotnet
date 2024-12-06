using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using WLSUser.Domain.Models;
using WLSUser.Services.Interfaces;

namespace WLSUser.Services
{
    public class EmailAPIService : IEmailAPIService
    {
        private readonly HttpClient _client;
        private readonly ILogger<EmailAPIService> _logger;

        public EmailAPIService(ILogger<EmailAPIService> logger, HttpClient client)
        {
            _client = client;
            _logger = logger;

        }
        public async Task<bool> RequestRecoverPassword(UserModel user, string code, SiteTypeEnum siteType)
        {
            try
            {
                _client.DefaultRequestHeaders.Add("x-api-key", Environment.GetEnvironmentVariable("EMAILER_API_TOKEN").ToString());
                _client.DefaultRequestHeaders.Add("EmailerAPIToken", Environment.GetEnvironmentVariable("EMAILER_API_TOKEN").ToString());
                _client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("*/*"));

                var body = new SendEmailRequest
                {
                    SiteType = siteType,
                    Template = "reset-password",
                    SendTo = new List<SendEmailBlock> { 
                        new SendEmailBlock
                        {
                            To = new List<UserEmailAddress>
                            {
                                new UserEmailAddress {
                                    UserId = user.UserID
                                }
                            },
                            TemplateData = new Dictionary<string, string>
                            {
                                ["functionCode"]=code,
                                ["greeting"]=user.FirstName
                            }
                        }
                    }
                };

                var jsonBody = JsonConvert.SerializeObject(body);
                byte[] messageBytes = System.Text.Encoding.UTF8.GetBytes(jsonBody);
                var content = new ByteArrayContent(messageBytes);
                content.Headers.ContentType = new MediaTypeHeaderValue("application/json");
                var response = await _client.PostAsync("api/v4/emails", content);

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogError("RequestRecoverPassword - Email Failed - {siteType}, {statusCode}, {userId}", siteType, response.StatusCode, user.UserID);
                    return false;
                }

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "RequestRecoverPassword - Failed - {siteType}, {userId}", siteType, user.UserID);
                return false;
            }
        }
    }
}
