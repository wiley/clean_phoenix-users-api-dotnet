using Microsoft.AspNetCore.WebUtilities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Mail;
using System.Threading.Tasks;
using WLSUser.Domain.Models;
using WLSUser.Domain.Models.Authentication;
using WLSUser.Infrastructure.Contexts;
using WLSUser.Services.Interfaces;

namespace WLSUser.Services
{
    public class FederationService : IFederationService
    {
        private readonly UserDbContext _context;
        private readonly ILogger<FederationService> _logger;

        public FederationService(UserDbContext context, ILogger<FederationService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<Federation> GetFederationByEmail(string email, int siteId)
        {
            MailAddress mailAddress = null;
            if (!MailAddress.TryCreate(email, out mailAddress))
            {
                _logger.LogWarning("GetFederationByEmail unable to convert email address, {email}.", email);
                return null;
            }
            string domain = mailAddress.Host;

            //First search for test users and return any federations with LIKE partial matches
            var federations = await _context.Federations
                                                .AsNoTracking()
                                                .Where(f => EF.Functions.Like(f.TestUsers, $"%{email}%") && f.SiteId == siteId)
                                                .ToListAsync();

            //TestUsers contains a comma delimited list "my.test@domain.com,a.test@domain.com"
            // and we passed "test@domain.com" which returned the record, but we need to search for whole word adding commas to beginning and end
            var federation = federations.FirstOrDefault(f => ("," + f.TestUsers + ",").Contains("," + email + ",", StringComparison.OrdinalIgnoreCase));
            
            if (federation != null)
                return federation;

            //Otherwise use the default mechanism to search for a domain
            federations = await _context.Federations
                                               .AsNoTracking()
                                               .Where(f => EF.Functions.Like(f.EmailDomain, $"%{domain}%") && f.SiteId == siteId)
                                               .ToListAsync();

            // Used "," to force the text be exacly, EF don't allow to use split and this core version don't has EF.Functions.StringSplit
            federation = federations.FirstOrDefault(f => ("," + f.EmailDomain + ",").Contains("," + domain + ",", StringComparison.OrdinalIgnoreCase));

            return federation;
        }

        public async Task<Federation> GetFederationByName(string name, int siteId)
        {
            var federation = await _context.Federations.FirstOrDefaultAsync(f => f.Name == name && f.SiteId == siteId);
            if (federation == null)
            {
                _logger.LogWarning($"Federation {name} was not found in the database.");
                return null;
            }
            return federation;
        }

        public SsoFederationUrlResponse GetFederationUrl(Federation federation, string state)
        {
            var queryString = new Dictionary<string, string>
            {
                { "client_id", federation.OpenIdClientId },
                { "response_type", "code" },
                { "state", state.ToString() },
                { "redirect_uri", federation.RedirectUrl }
            };

            if (!string.IsNullOrWhiteSpace(federation.AlmFederationName))
            {
                queryString.Add("kc_idp_hint", federation.AlmFederationName);
            }

            var federationUrl = QueryHelpers.AddQueryString(federation.OpenIdAuthInitUrl, queryString);

            return new SsoFederationUrlResponse { Url = federationUrl };
        }

        public async Task<string> StoreStateInformation(string stateInformation)
        {
            string key = Guid.NewGuid().ToString();

            SSOState stateObject = new SSOState()
            {
                Key = key,
                Data = stateInformation,
                Created = DateTime.Now
            };

            _context.SSOStates.Add(stateObject);
            await _context.SaveChangesAsync();

            return key;
        }

        public async Task<string> GetStateInformation(string stateKey)
        {
            SSOState ssoState = await _context.SSOStates.FirstOrDefaultAsync(u => u.Key == stateKey);
            if (ssoState == null)
                return null;

            TimeSpan timeElapsed = DateTime.Now.Subtract(ssoState.Created);

            string data = ssoState.Data;

            //Remove the state from the database
            _context.SSOStates.Remove(ssoState);
            await _context.SaveChangesAsync();

            if (timeElapsed.Minutes < 0 || timeElapsed.Minutes > 30)
                return null;

            return data ?? "";
        }
    }
}
