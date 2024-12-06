using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using WLSUser.Domain.Exceptions;
using WLSUser.Domain.Models;
using WLSUser.Domain.Models.V4;
using WLSUser.Infrastructure.Contexts;
using WLSUser.Services.Interfaces;

namespace WLSUser.Services
{
    public class UserMappingService : IUserMappingService
    {
        private readonly UserDbContext _userDbContext;
        private readonly ILogger<UserMappingService> _logger;

        public UserMappingService(UserDbContext userDbContext, ILogger<UserMappingService> logger)
        {
            _userDbContext = userDbContext;
            _logger = logger;
        }

        public void AddNewUserMapping(UserDbContext passedContext, UserMapping userMappingData)
        {
            //This call is used to add a mapping when adding a new user. It takes in the DB context being used to add the user so that both records are added at the same time, or if there is an error, neither is added. 
            try
            {
               if (passedContext.UserMappings.AsNoTracking().Any(u => u.UserId == userMappingData.UserId && u.PlatformName == userMappingData.PlatformName && u.PlatformCustomer == userMappingData.PlatformCustomer && u.PlatformRole == userMappingData.PlatformRole && u.PlatformAccountId == userMappingData.PlatformAccountId))
                {
                    //should not be using this call if the mapping exists
                    _logger.LogWarning($"UserMappingService - AddNewUserMapping - Mapping Already exists for new user ID  - {userMappingData.UserId} - {userMappingData.PlatformName} - {userMappingData.PlatformCustomer} - {userMappingData.PlatformRole}", userMappingData.UserId, userMappingData.PlatformName, userMappingData.PlatformCustomer, userMappingData.PlatformRole);
                    throw new Exception("Mapping already Exists");
                }
                else
                {
                    userMappingData.Created = DateTime.Now;
                    userMappingData.Updated = DateTime.Now;

                    passedContext.UserMappings.Add(userMappingData);
                }
            }
            catch (Exception ex)
            {
                //throw any error that occured and let the controller log it.
                _logger.LogWarning(ex.Message, $"UserMappingService - AddNewUserMapping - error for user ID - {userMappingData.UserId} - {userMappingData.PlatformName} - {userMappingData.PlatformCustomer} - {userMappingData.PlatformRole}", userMappingData.UserId, userMappingData.PlatformName, userMappingData.PlatformCustomer, userMappingData.PlatformRole);
                throw new Exception(ex.Message);
            }
        }

        public UserMappingResponse CreateUserMapping(int userId, CreateUserMappingRequest userMappingData)
        {
            try
            {
                if (!_userDbContext.Users.Any(u => u.UserID == userId))
                {
                    throw new NotFoundException("User Not Found");
                }

                UserMapping userMapping = _userDbContext.UserMappings.FirstOrDefault(u => u.UserId == userId
                && u.PlatformName == userMappingData.PlatformName
                && u.PlatformCustomer == userMappingData.PlatformCustomer
                && u.PlatformRole == userMappingData.PlatformRole);

                if (userMapping != null)
                {
                    throw new ArgumentException("UserMapping already exists");
                }
                
                userMapping = new UserMapping()
                {
                    PlatformName = userMappingData.PlatformName,
                    PlatformCustomer = userMappingData.PlatformCustomer,
                    PlatformRole = userMappingData.PlatformRole,
                    PlatformUserId = userMappingData.PlatformUserId,
                    PlatformAccountId = userMappingData.PlatformAccountId,
                    PlatformData = userMappingData.PlatformData,
                    UserId = userId,
                    Updated = DateTime.Now,
                    Created = DateTime.Now,
                };
                _userDbContext.UserMappings.Add(userMapping);
                _userDbContext.SaveChanges();
                return new UserMappingResponse()
                {
                    Id = userMapping.Id,
                    CreatedAt = userMapping.Created,
                    CreatedBy = userMapping.CreatedBy,
                    UpdatedAt = userMapping.Updated,
                    UpdatedBy = userMapping.UpdatedBy,
                    UserId = userMapping.UserId,
                    PlatformName = userMapping.PlatformName,
                    PlatformCustomer = userMapping.PlatformCustomer,
                    PlatformRole = userMapping.PlatformRole,
                    PlatformUserId = userMapping.PlatformUserId,
                    PlatformAccountId = userMapping.PlatformAccountId,
                    PlatformData = userMapping.PlatformData,
                };
            }
            catch (NotFoundException ex)
            {
                //throw any error that occured and let the controller log it.
                _logger.LogWarning(ex.Message, "UserMappingService - CreateUserMapping - user not found - {userId} - {userMappingData.PlatformName} - {userMappingData.PlatformCustomer} - {userMappingData.PlatformRole}", userId, userMappingData.PlatformName, userMappingData.PlatformCustomer, userMappingData.PlatformRole);
                throw;
            }
            catch (ArgumentException ex)
            {
                //throw any error that occured and let the controller log it.
                _logger.LogWarning(ex.Message, "UserMappingService - CreateUserMapping - already exists - {userId} - {userMappingData.PlatformName} - {userMappingData.PlatformCustomer} - {userMappingData.PlatformRole}", userId, userMappingData.PlatformName, userMappingData.PlatformCustomer, userMappingData.PlatformRole);
                throw;
            }
            catch (Exception ex)
            {
                //throw any error that occured and let the controller log it.
                _logger.LogWarning(ex.Message, $"UserMappingService - CreateUserMapping - error for user ID - {userId} - {userMappingData.PlatformName} - {userMappingData.PlatformCustomer} - {userMappingData.PlatformRole}", userId, userMappingData.PlatformName, userMappingData.PlatformCustomer, userMappingData.PlatformRole);
                throw new Exception(ex.Message);
            }
        }
        
        public UserMappingResponse UpdateUserMapping(int userId, int userMappingId, UpdateUserMappingRequest userMappingData)
        {
            try
            {
                UserMapping userMapping = _userDbContext.UserMappings.FirstOrDefault(u => u.UserId == userId
                && u.Id == userMappingId);

                if (userMapping == null)
                {
                    throw new NotFoundException("UserMapping Not Found");
                }
                else
                {
                    //update mapping
                    if (!string.IsNullOrEmpty(userMappingData.PlatformUserId) && userMapping.PlatformUserId != userMappingData.PlatformUserId)
                    {
                        userMapping.PlatformUserId = userMappingData.PlatformUserId;
                    }

                    if (!string.IsNullOrEmpty(userMappingData.PlatformAccountId) && userMapping.PlatformAccountId != userMappingData.PlatformAccountId)
                    {
                        userMapping.PlatformAccountId = userMappingData.PlatformAccountId;
                    }
                    
                    if (!string.IsNullOrEmpty(userMappingData.PlatformData) && userMapping.PlatformData != userMappingData.PlatformData)
                    {
                        userMapping.PlatformData = userMappingData.PlatformData;
                    }


                    if (_userDbContext.Entry(userMapping).State == EntityState.Modified)
                    {
                        userMapping.Updated = DateTime.Now;
                        _userDbContext.SaveChanges();
                    }
                }
                return new UserMappingResponse()
                {
                    Id = userMapping.Id,
                    CreatedAt = userMapping.Created,
                    CreatedBy = userMapping.CreatedBy,
                    UpdatedAt = userMapping.Updated,
                    UpdatedBy = userMapping.UpdatedBy,
                    UserId = userMapping.UserId,
                    PlatformName = userMapping.PlatformName,
                    PlatformCustomer = userMapping.PlatformCustomer,
                    PlatformRole = userMapping.PlatformRole,
                    PlatformUserId = userMapping.PlatformUserId,
                    PlatformAccountId = userMapping.PlatformAccountId,
                    PlatformData = userMapping.PlatformData,
                };
            }
            catch (NotFoundException ex)
            {
                //throw any error that occured and let the controller log it.
                _logger.LogWarning(ex.Message, "UserMappingService - UpdateUserMapping - Not found - userId: {userId}, userMappingId: {userMappingId}", userId, userMappingId);
                throw new NotFoundException(ex.Message);
            }
            catch (Exception ex)
            {
                //throw any error that occured and let the controller log it.
                _logger.LogWarning(ex.Message, $"UserMappingService - UpdateUserMapping - error for user ID: {userId}, userMappingId: {userMappingId}", userId, userMappingId);
                throw new Exception(ex.Message);
            }
        }

        public UserMappingResponse GetUserMapping(int userId, int userMappingId)
        {
            try
            {
                UserMapping userMapping = _userDbContext.UserMappings.FirstOrDefault(u => u.Id == userMappingId && u.UserId == userId);
                if (userMapping == null)
                    throw new NotFoundException();
                return new UserMappingResponse()
                {
                    Id = userMapping.Id,
                    CreatedAt = userMapping.Created,
                    CreatedBy = userMapping.CreatedBy,
                    UpdatedAt = userMapping.Updated,
                    UpdatedBy = userMapping.UpdatedBy,
                    UserId = userMapping.UserId,
                    PlatformName = userMapping.PlatformName,
                    PlatformCustomer = userMapping.PlatformCustomer,
                    PlatformRole = userMapping.PlatformRole,
                    PlatformUserId = userMapping.PlatformUserId,
                    PlatformAccountId = userMapping.PlatformAccountId,
                    PlatformData = userMapping.PlatformData,
                };
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex.Message, $"UserMappingService - GetUserMapping - error for user ID - {userId}, userMappingId - {userMappingId}");
                throw;
            }
        }

        public UserMappingsResponse GetUserMappingsByUserId(int userId, string platformName, string platformCustomer, string platformRole)
        {
            try
            {
                platformName ??= "";
                platformCustomer ??= "";
                platformRole ??= "";

                UserMappingsResponse response = new UserMappingsResponse() { Items = new List<UserMappingResponse>()};
                IQueryable<UserMapping> userMappings = _userDbContext.UserMappings.Where(
                    u => u.UserId == userId
                        && (platformName == "" || u.PlatformName == platformName)
                        && (platformCustomer == "" || u.PlatformCustomer == platformCustomer)
                        && (platformRole == "" || u.PlatformRole == platformRole)
                        );
                if (userMappings == null || !userMappings.Any())
                    throw new NotFoundException();

                foreach (var userMapping in userMappings)
                {
                    response.Items.Add(new UserMappingResponse()
                    {
                        Id = userMapping.Id,
                        CreatedAt = userMapping.Created,
                        CreatedBy = userMapping.CreatedBy,
                        UpdatedAt = userMapping.Updated,
                        UpdatedBy = userMapping.UpdatedBy,
                        UserId = userMapping.UserId,
                        PlatformName = userMapping.PlatformName,
                        PlatformCustomer = userMapping.PlatformCustomer,
                        PlatformRole = userMapping.PlatformRole,
                        PlatformUserId = userMapping.PlatformUserId,
                        PlatformAccountId = userMapping.PlatformAccountId,
                        PlatformData = userMapping.PlatformData,
                    });
                }
                return response;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex.Message, $"UserMappingService - GetUserMappingsByUserId - error for user ID - {userId}");
                throw;
            }
        }

        public void DeleteUserMapping(int userId, int userMappingId)
        {
            try
            {
                UserMapping userMapping = _userDbContext.UserMappings.FirstOrDefault(u => u.Id == userMappingId && u.UserId == userId);
                if (userMapping == null)
                    throw new NotFoundException();
                _userDbContext.Remove(userMapping);
                _userDbContext.SaveChanges();
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex.Message, $"UserMappingService - GetUserMapping - error for user ID - {userMappingId}");
                throw;
            }
        } 
    }


}
