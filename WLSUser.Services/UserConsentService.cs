using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WLSUser.Domain.Exceptions;
using WLSUser.Domain.Models.V4;
using WLSUser.Infrastructure.Contexts;
using WLSUser.Services.Interfaces;

namespace WLSUser.Services
{
    public class UserConsentService : IUserConsentService
    {
        private readonly UserDbContext _userDbContext;
        private readonly ILogger<UserConsentService> _logger;

        public UserConsentService(UserDbContext userDbContext, ILogger<UserConsentService> logger)
        {
            _userDbContext = userDbContext;
            _logger = logger;
        }

        public async Task<IEnumerable<UserConsent>> SearchUserConsents(int userId, string policyType, bool latestVersion)
        {
            var query = _userDbContext.UserConsent.Where(x => x.UserId == userId).AsNoTracking();

            if (!string.IsNullOrWhiteSpace(policyType))
                query = query.Where(x => x.PolicyType == policyType);

            if (latestVersion)
            {
                // Bringing to memory because Mysql Distinct works differently from SQL Server and 
                // Linq cannot translate properly.
                var policies = await query.ToListAsync();
                return policies.OrderByDescending(x => x.PolicyType).ThenByDescending(x => x.Version).DistinctBy(d => d.PolicyType).ToList();
            }

            return await query.OrderByDescending(x => x.PolicyType).ThenByDescending(x => x.Version).ToListAsync();
        }

        public async Task<UserConsent> GetUserConsentById(int userId, int userConsentId)
        {
            return await _userDbContext.UserConsent.FirstOrDefaultAsync(x => x.UserId == userId && x.Id == userConsentId);
        }

        public async Task<UserConsent> CreateUserConsent(int userId, CreateUserConsentRequest request)
        {
            var userConsentDb = await _userDbContext.UserConsent.AnyAsync(x => x.UserId == userId
            && x.PolicyType == request.PolicyType && x.Version == request.Version);
            if (userConsentDb)
                throw new ConflictException("User-Consent already exists.");

            UserConsent consent = new UserConsent
            {
                UserId = userId,
                AcknowledgedAt = DateTime.UtcNow,
                CreatedAt = DateTime.UtcNow,
                CreatedBy = userId,
                PolicyType = request.PolicyType,
                Status = request.Status,
                UpdatedAt = DateTime.UtcNow,
                UpdatedBy = userId,
                Version = request.Version
            };
            _userDbContext.UserConsent.Add(consent);
            await _userDbContext.SaveChangesAsync();
            return consent;
        }

        public async Task DeleteUserConsent(int userId, int userConsentId)
        {
            UserConsent consent = await _userDbContext.UserConsent.FirstOrDefaultAsync(x => x.UserId == userId && x.Id == userConsentId) 
                ?? throw new NotFoundException();

            _userDbContext.UserConsent.Remove(consent);
            await _userDbContext.SaveChangesAsync();
        }

    }
}
