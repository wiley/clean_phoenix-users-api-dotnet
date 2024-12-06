using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WLSUser.Domain.Models;
using WLSUser.Domain.Models.V4;
using WLSUser.Infrastructure.Contexts;

namespace WLSUser.Services.Interfaces
{
    public interface IUserMappingService
    {
        void AddNewUserMapping(UserDbContext passedContext, UserMapping userMappingData);
        UserMappingResponse CreateUserMapping(int userId, CreateUserMappingRequest userMappingData);
        UserMappingResponse UpdateUserMapping(int userId, int userMappingId, UpdateUserMappingRequest userMappingData);
        UserMappingResponse GetUserMapping(int userId, int userMappingId);
        void DeleteUserMapping(int userId, int userMappingId);
        UserMappingsResponse GetUserMappingsByUserId(int userId, string platformName, string platformCustomer, string platformRole);
    }
}
