using Amazon.Runtime.Internal;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using WLS.KafkaMessenger;
using WLSUser.Domain.Exceptions;
using WLSUser.Domain.Models;
using WLSUser.Domain.Models.Authentication;
using WLSUser.Domain.Models.V4;
using WLSUser.Infrastructure.Contexts;
using WLSUser.Services.Interfaces;

namespace WLSUser.Services
{
    public class UserService : IUserService
    {
        private readonly UserDbContext _userDbContext;
        private readonly ILearnerEmailAPI _learnerEmailAPI;
        private readonly IKafkaService _kafkaService;
        private readonly ILogger<UserService> _logger;
        private readonly IRedisService _redisService;
        private readonly IUserMappingService _userMappingService;
        private readonly string EMPTY_STRING = "---EMPTY---";
        private readonly IEmailAPIService _emailAPIService;
        private readonly IKeyCloakService _keycloakService;


        public UserService(UserDbContext userDbContext, ILearnerEmailAPI learnerEmailAPI, IKafkaService kafkaService, IRedisService redisService, ILogger<UserService> logger, IUserMappingService userMappingService, IEmailAPIService emailAPIService, IKeyCloakService keycloakService)
        {
            _userDbContext = userDbContext;
            _learnerEmailAPI = learnerEmailAPI;
            _kafkaService = kafkaService;
            _logger = logger;
            _redisService = redisService;
            _userMappingService = userMappingService;
            _emailAPIService = emailAPIService;
            _keycloakService = keycloakService;
        }

        /*
        //Temporary method to hash a password
        //Examples:
        //    object[] temp2 = UserService.GenerateHash("WorkplaceUser472");
        //    object[] temp3 = UserService.GenerateHash("WorkplaceLearner101", DateTime.Now);
        public static object[] GenerateHash(string password, DateTime? passwordDate = null)
        {
            object[] response = new object[3];

            response[0] = passwordDate ?? DateTime.Now;

            //Store strong password for next time
            byte[] passwordSalt = CreateSalt(16);
            byte[] passwordHash = GenerateSaltedHashSHA256(UnicodeEncoding.Unicode.GetBytes(password), passwordSalt, passwordDate);

            response[1] = Convert.ToBase64String(passwordSalt);
            response[2] = Convert.ToBase64String(passwordHash);

            return response;
        }
        */

        public LoginResponseModel Login(LoginRequestModel loginRequest, bool passwordRequired = true)
        {
            if (loginRequest == null || string.IsNullOrEmpty(loginRequest.Username) || (string.IsNullOrEmpty(loginRequest.Password) && passwordRequired))
                throw new ArgumentNullException();

            List<UserMapping> badMatches = null;
            UserModel user = null;
            List<UserMapping> matches = FindMatches(loginRequest.Username, loginRequest.Password, loginRequest.UserType, ref badMatches, ref user);

            bool anyMatches = (matches.Count > 0);

            var response = new LoginResponseModel
            {
                Status = (anyMatches) ? "Connected" : (badMatches.Count > 0) ? "Failed" : "Not Found",
                FirstName = (anyMatches) ? user.FirstName : null,
                LastName = (anyMatches) ? user.LastName : null,
                UniqueUserIDs = matches.Select(x => x.CreateUniqueID()).ToArray(),
            };

            if (matches.Count > 0)
            {
                UserMapping match = matches.FirstOrDefault(m => m.PlatformName == "epic" && m.PlatformRole == "learner");
                if (match != null)
                    LogLoginAttempt(user, match, true);
            }
            else if (badMatches.Count > 0)
            {
                UserMapping badMatch = badMatches.FirstOrDefault(m => m.PlatformName == "epic" && m.PlatformRole == "learner");
                if (badMatch != null)
                    LogLoginAttempt(user, badMatch, false);
            }
            response.UserName = loginRequest.Username;

            if (user != null)
                response.UserRoles = GetUserRoleResponses(user.UserID);
            else
                response.UserRoles = new List<UserRoleResponse>();

            return response;
        }

        public LoginResponse Login(string username, string password, SiteTypeEnum siteType, UserTypeEnum userType, bool passwordRequired = true)
        {
            if (string.IsNullOrEmpty(username))
                throw new ArgumentNullException("username");
            else if (string.IsNullOrEmpty(password) && passwordRequired)
                throw new ArgumentNullException("password");

            try
            {
                List<UserMapping> badMatches = null;
                UserModel user = null;
                List<UserMapping> matches = FindMatches(username, passwordRequired ? password : null, userType, ref badMatches, ref user, siteType);

                if (matches.Count > 0)
                {
                    UserMapping match = matches.FirstOrDefault(m => m.PlatformName == "epic" && m.PlatformRole == "learner");
                    if (match != null)
                        LogLoginAttempt(user, match, true);
                }
                else if (badMatches.Count > 0)
                {
                    UserMapping badMatch = badMatches.FirstOrDefault(m => m.PlatformName == "epic" && m.PlatformRole == "learner");
                    if (badMatch != null)
                        LogLoginAttempt(user, badMatch, false);
                }

                if (matches.Count == 0 || badMatches.Count > 0)
                    return new LoginResponse { Status = matches.Count == 0 ? "Not Found" : "Failed" };

                return CompleteLogin(user, matches.First(), true);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "UserService - Login Failed");
                throw;
            }
        }

        public LoginResponse LoginV2Refresh(string username, string uniqueID)
        {
            if (!IsValidUniqueID(uniqueID))
                throw new NotFoundException();

            UserModel user = _userDbContext.Users.FirstOrDefault(u => u.Username == username);

            if (user == null)
                throw new NotFoundException();

            UserUniqueID userUniqueID = new UserUniqueID(uniqueID);
            string uniqueIdAccountId = userUniqueID.AccountID.ToString();

            UserMapping userMapping = _userDbContext.UserMappings.FirstOrDefault(
                                                u => u.UserId == user.UserID &&
                                                u.PlatformName == userUniqueID.PlatformName &&
                                                u.PlatformCustomer == userUniqueID.Instance &&
                                                u.PlatformRole == userUniqueID.AccountType &&
                                                u.PlatformUserId == uniqueIdAccountId);

            if (userMapping == null)
                throw new NotFoundException();

            return CompleteLogin(user, userMapping, false);
        }

        private LoginResponse CompleteLogin(UserModel user, UserMapping userMapping, bool forceCacheUpdate = true)
        {
            var roleAccessReferences = GetRoleAccessReferences(user.UserID, forceCacheUpdate);
            var roles = roleAccessReferences.Select(rar => rar.RoleType.RoleName).Distinct().ToList();
            var response = new LoginResponse
            {
                Status = "Connected",
                FirstName = user.FirstName,
                LastName = user.LastName,
                UserName = user.Username,
                UniqueID = userMapping.CreateUniqueID(),
                UserType = userMapping.CreateUserType(),
                Roles = roles
            };

            return response;
        }

        public List<RoleAccessReference> GetRoleAccessReferences(int userID, bool forceUpdate = true, int secondsToExpire = 12 * 60 * 60)
        {
            string cacheKey = $"RoleAccessReferences_{userID}";
            if (_redisService.KeyExists(cacheKey) && !forceUpdate)
            {
                return _redisService.Get<List<RoleAccessReference>>(cacheKey);
            }
            else
            {
                var query = from ur in _userDbContext.UserRoles
                            join rt in _userDbContext.RoleTypes on ur.RoleTypeID equals rt.RoleTypeID
                            where ur.UserID == userID
                            select new { UserRoleID = ur.UserRoleID, RoleType = rt };

                var roles = query.ToList();

                var rarDict = new Dictionary<(RoleType, AccessType), List<UserRoleAccess>>();

                for (var i = 0; i < roles.Count; i++)
                {
                    var rarQuery = from ura in _userDbContext.UserRoleAccess
                                   join at in _userDbContext.AccessTypes on ura.AccessTypeID equals at.AccessTypeID
                                   where ura.UserRoleID == roles[i].UserRoleID
                                   select new { AccessType = at, UserRoleAccess = ura };

                    if (!rarQuery.Any())
                        rarDict.Add((roles[i].RoleType, new AccessType()), new List<UserRoleAccess>());
                    else
                    {
                        foreach (var rarItem in rarQuery.ToList())
                        {
                            var key = (roles[i].RoleType, rarItem.AccessType);
                            if (!rarDict.ContainsKey(key))
                                rarDict.Add(key, new List<UserRoleAccess>());
                            rarDict[key].Add(rarItem.UserRoleAccess);
                        }
                    }
                }

                var result = new List<RoleAccessReference>(rarDict.Count());

                foreach (var rarItem in rarDict)
                {
                    result.Add(new RoleAccessReference { RoleType = rarItem.Key.Item1, AccessType = rarItem.Key.Item2, UserRoleAccessList = rarItem.Value });
                }

                /*
                var query = from ur in _userDbContext.UserRoles
                            join rt in _userDbContext.RoleTypes on ur.RoleTypeID equals rt.RoleTypeID
                            join ura in _userDbContext.UserRoleAccess on ur.UserRoleID equals ura.UserRoleID
                            join at in _userDbContext.AccessTypes on ura.AccessTypeID equals at.AccessTypeID
                            where ur.UserID == userID
                            group ura by new { rt, at } into urGroup
                            from ura in urGroup.DefaultIfEmpty()
                            select new RoleAccessReference
                            {
                                RoleType = urGroup.Key.rt,
                                AccessType = urGroup.Key.at,
                                UserRoleAccessList = urGroup.ToList()
                            };
                var result = query.ToList();
                */
                _redisService.SetString(secondsToExpire, cacheKey, JsonConvert.SerializeObject(result));
                return result;
            }
        }

        public int CreateUser(string username)
        {
            //Find if the user is already there
            var user = _userDbContext.Users.AsNoTracking().FirstOrDefault(u => u.Username == username);
            if (user != null)
            {
                if (user.UserID == 0)
                    throw new Exception("Uninitialized UserID");
                return user.UserID;
            }

            user = new UserModel()
            {
                Username = username,
                FirstName = username,
                LastName = username,
                Email = username,
                UserType = UserTypeEnum.EPICLearner,
                Status = (int)UserStatus.Active
            };
            _userDbContext.Users.Add(user);
            _userDbContext.SaveChanges();

            //After SaveChanges, UserID should be set
            return user.UserID;
        }

        public IEnumerable<SearchResponseModel> Search(SearchRequestModel request)
        {
            List<UserMapping> badMatches = null;
            UserModel user = null;
            List<UserMapping> matches = FindMatches(request.Username, null, request.UserType, ref badMatches, ref user);
            return matches.Select(match => new SearchResponseModel
            {
                FirstName = user.FirstName,
                LastName = user.LastName,
                UniqueID = match.CreateUniqueID(),
                UserType = match.CreateUserType(),
                UserRoles = GetUserRoleResponses(user.UserID)
            });
        }

        public IEnumerable<UniqueIDSearchResponseModel> SearchByUniqueID(UniqueIDSearchRequestModel request)
        {
            // This search is unique, it is not going to return a single match for the UniqueID,
            // but actually, return all the user entries for the user that possesses the input UniqueID.
            UserModel user = GetUserFromUniqueID(request.UniqueID);

            if (user == null)
                return new List<UniqueIDSearchResponseModel> { };

            List<UserMapping> badMatches = null;
            List<UserMapping> matches = FindMatches(user.Username, null, UserTypeEnum.Any, ref badMatches, ref user);

            return matches.Select(match => new UniqueIDSearchResponseModel
            {
                Username = user.Username,
                FirstName = user.FirstName,
                LastName = user.LastName,
                UniqueID = match.CreateUniqueID(),
                UserType = match.CreateUserType(),
                UserRoles = GetUserRoleResponses(user.UserID)
            });
        }

        public ForgotPasswordResponseModel ForgotPassword(string apiToken, ForgotPasswordRequestModel forgotPassword)
        {
            if (forgotPassword == null || string.IsNullOrEmpty(forgotPassword.Username))
                throw new ArgumentNullException();

            var response = new ForgotPasswordResponseModel();
            List<UserMapping> badMatches = null;
            UserModel user = null;
            List<UserMapping> matches = FindMatches(forgotPassword.Username, null, forgotPassword.UserType, ref badMatches, ref user);

            foreach (UserMapping match in matches)
            {
                if (match.PlatformName == "epic" && match.PlatformRole == "learner")
                {
                    if (_userDbContext.UserRoles.Any(ur => ur.UserID == user.UserID && ur.RoleTypeID == (int)RoleTypeEnum.CatalystDemo))
                        throw new Exception("Attempted Password Reset of Catalyst Demo User");

                    Task<bool> task = _learnerEmailAPI.RequestForgotPassword(apiToken, forgotPassword.Username);
                    task.Wait();
                    response.Status = (task.Result) ? "Requested" : "Failed";
                }
                else
                {
                    //No other systems currently use this method
                }
            }

            response.Status = "Requested";

            return response;
        }

        public UpdateUserResponseModel UpdateUser(UpdateUserRequestModel request)
        {
            //A default Status of Complete will be sent for any response that isn't handled
            UpdateUserResponseModel response = new UpdateUserResponseModel() { Status = "Complete" };
            try
            {
                if (request == null)
                    throw new Exception($"UpdateUser - missing request object");
                if (string.IsNullOrEmpty(request.UniqueID) || !IsValidUniqueID(request.UniqueID))
                    throw new Exception($"UpdateUser - invalid UniqueID ({request.UniqueID})");

                UserUniqueID userUniqueID = new UserUniqueID(request.UniqueID); //For now, the uniqueID class is still ok, but not all UniqueID.AccountIDs are UserIDs
                UserModel user = _userDbContext.Users.FirstOrDefault(u => u.UniqueID == request.UniqueID);
                if (user == null)
                {
                    //The only Site's using UpdateUserRequestModel are Eclipse UserUpdateService and LPI Online
                    if (userUniqueID.SiteType == SiteTypeEnum.Epic && userUniqueID.AccountType == "learner")
                    {
                        //Eclipse UserUpdateService could send a request.Username update request, so we can't guarantee it's our key
                        //But LearnerID == UserID
                        user = _userDbContext.Users.FirstOrDefault(u => u.UserID == userUniqueID.AccountID);
                        if (user == null)
                        {
                            //AccountID not found - Not allowing Learners to be created by this method
                            throw new Exception($"AddingUser - invalid UniqueID ({request.UniqueID})");
                        }
                    }
                    else if (!string.IsNullOrEmpty(request.Username))
                    {
                        //LPI or other does not have another field in this UpdateUserRequestModel than username
                        user = _userDbContext.Users.FirstOrDefault(u => u.Username == request.Username);
                    }
                }

                if (user == null) // Adding new user
                {
                    //Still allowing the creation of users - to support LPI user creation

                    //Validate all parameters
                    if (string.IsNullOrEmpty(request.Username) || request.Username.Length < 8 || request.Username.Length > 255 || !IsValidUsername(request.Username))
                        throw new Exception($"AddingUser - invalid Username ({request.Username})");
                    if (string.IsNullOrEmpty(request.FirstName) || request.FirstName.Length > 100)
                        throw new Exception($"AddingUser - invalid FirstName ({request.FirstName})");
                    if (string.IsNullOrEmpty(request.LastName) || request.LastName.Length > 100)
                        throw new Exception($"AddingUser - invalid LastName ({request.LastName})");
                    if (string.IsNullOrEmpty(request.Email) || request.Email.Length < 8 || request.Email.Length > 255 || (request.Email != EMPTY_STRING && !IsValidEmailAddress(request.Email)))
                        throw new Exception($"AddingUser - invalid Email ({request.Email})");
                    if (request.Salt != "" && (request.Salt == null || request.Salt.Length < 8 || request.Salt.Length > 50))
                        throw new Exception($"AddingUser - invalid Salt ({request.Salt})");
                    if (string.IsNullOrEmpty(request.Hash) || request.Hash.Length < 8 || request.Hash.Length > 50)
                        throw new Exception($"AddingUser - invalid Hash ({request.Hash})");
                    if (request.UserType == UserTypeEnum.Any) //Cannot add a user without a type
                        throw new Exception($"AddingUser - invalid UserType ({request.UserType})");

                    DateTime timestamp = DateTime.Now;

                    user = new UserModel
                    {
                        Username = request.Username,
                        Email = request.Email.Replace(EMPTY_STRING, ""),
                        FirstName = request.FirstName.Replace(EMPTY_STRING, ""),
                        LastName = request.LastName.Replace(EMPTY_STRING, ""),
                        OrigPasswordSalt = "",
                        OrigPasswordHash = "",
                        UserType = request.UserType,
                        Status = request.Status,
                        LastUpdated = timestamp
                    };

                    //user.UserID will be auto-created when SaveChanges is called
                    _userDbContext.Users.Add(user);
                    _userDbContext.SaveChanges(); //Commit change to get UserID

                    if (user.UserID <= 0)
                        throw new Exception($"AddingUser - UserID not created ({request.UniqueID})");

                    UserMapping userMapping = new UserMapping
                    {
                        UserId = user.UserID,
                        PlatformName = userUniqueID.PlatformName,
                        PlatformCustomer = userUniqueID.Instance,
                        PlatformRole = userUniqueID.AccountType,
                        PlatformUserId = userUniqueID.PlatformUserID,
                        PlatformPasswordHash = request.Hash,
                        PlatformPasswordSalt = request.Salt,
                        PlatformPassowrdMethod = userUniqueID.PlatformPasswordMethod(),
                        Created = timestamp,
                        Updated = timestamp
                    };
                    //Add the User Mapping record
                    _userDbContext.UserMappings.Add(userMapping);
                    _userDbContext.SaveChanges();

                    // Add UserRole
                    int roleTypeID = 0;

                    switch (userUniqueID.SiteType)
                    {
                        case SiteTypeEnum.Epic:
                            if (userUniqueID.AccountType == "learner")
                                roleTypeID = 1; // Catalyst Learner
                            break;

                        case SiteTypeEnum.Catalyst:
                            if (userUniqueID.AccountType == "facilitator")
                                roleTypeID = 2; // Catalyst Facilitator
                            break;

                        default: // No RoleType defined
                            roleTypeID = 0;
                            break;
                    }

                    if (roleTypeID > 0)
                    {
                        _userDbContext.UserRoles.Add(new UserRole
                        {
                            UserID = (int)user.UserID,
                            RoleTypeID = roleTypeID
                        });
                        _userDbContext.SaveChanges();
                    }

                    response.Status = "Added";
                }
                else // Updating existing user
                {
                    //Validate each potential change as a separate change, but then later apply only the changes
                    if (!string.IsNullOrEmpty(request.Username))
                    {
                        if (request.Username.Length < 8 || request.Username.Length > 100 || !IsValidUsername(request.Username))
                            throw new Exception($"UpdateUser - invalid Username ({request.Username})");
                    }

                    if (!string.IsNullOrEmpty(request.Email))
                    {
                        if (request.Email.Length < 8 || request.Email.Length > 255 ||
                            (request.Email != EMPTY_STRING && !IsValidEmailAddress(request.Email)))
                            throw new Exception($"UpdateUser - invalid Email ({request.Email})");
                    }

                    if (!string.IsNullOrEmpty(request.FirstName))
                    {
                        if (request.FirstName.Length > 100)
                            throw new Exception($"UpdateUser - invalid FirstName ({request.FirstName})");
                    }

                    if (!string.IsNullOrEmpty(request.LastName))
                    {
                        if (request.LastName.Length > 100)
                            throw new Exception($"UpdateUser - invalid LastName ({request.LastName})");
                    }

                    if (!string.IsNullOrEmpty(request.Hash))
                    {
                        if (request.Salt != "" && (request.Salt.Length < 8 || request.Salt.Length > 50))
                            throw new Exception($"UpdateUser - invalid Salt ({request.Salt})");
                        if (request.Hash.Length < 8 || request.Hash.Length > 50)
                            throw new Exception($"UpdateUser - invalid Hash ({request.Hash})");
                    }
                    else
                    {
                        //If Salt provided without Hash, then not valid
                        if (!string.IsNullOrEmpty(request.Salt))
                            throw new Exception($"UpdateUser - invalid Salt ({request.Salt})");
                    }

                    //Make the changes to the entity
                    if (!string.IsNullOrEmpty(request.Username) && request.Username != user.Username)
                        user.Username = request.Username;
                    if (!string.IsNullOrEmpty(request.Email) && request.Email != user.Email)
                        user.Email = request.Email.Replace(EMPTY_STRING, "");
                    if (!string.IsNullOrEmpty(request.FirstName) && request.FirstName != user.FirstName)
                        user.FirstName = request.FirstName.Replace(EMPTY_STRING, "");
                    if (!string.IsNullOrEmpty(request.LastName) && request.LastName != user.LastName)
                        user.LastName = request.LastName.Replace(EMPTY_STRING, "");
                    if (!string.IsNullOrEmpty(request.Hash))
                    {
                        if (!string.IsNullOrEmpty(user.StrongPasswordHash))
                        {
                            user.OrigPasswordSalt = "";//clear obsolete variable
                            user.OrigPasswordHash = "";
                        }
                    }

                    if (user.Status != request.Status)
                        user.Status = request.Status;

                    if (_userDbContext.Entry(user).State == EntityState.Modified)
                    {
                        user.LastUpdated = DateTime.Now;
                    }

                    UserMapping userMapping = _userDbContext.UserMappings
                        .FirstOrDefault(u => u.UserId == user.UserID &&
                                                u.PlatformName == userUniqueID.PlatformName &&
                                                u.PlatformCustomer == userUniqueID.Instance &&
                                                u.PlatformRole == userUniqueID.AccountType);

                    if (userMapping == null)
                    {
                        //Likely first population of the User data
                        userMapping = new UserMapping()
                        {
                            UserId = user.UserID,
                            PlatformName = userUniqueID.PlatformName,
                            PlatformCustomer = userUniqueID.Instance,
                            PlatformRole = userUniqueID.AccountType,
                            PlatformUserId = userUniqueID.PlatformUserID,
                            Created = DateTime.Now,
                            Updated = DateTime.Now
                        };
                        if (string.IsNullOrEmpty(user.StrongPasswordHash))
                        {
                            userMapping.PlatformPasswordHash = request.Hash;
                            userMapping.PlatformPasswordSalt = request.Salt;
                            userMapping.PlatformPassowrdMethod = userUniqueID.PlatformPasswordMethod();
                        };
                        _userDbContext.UserMappings.Add(userMapping);
                    }
                    else
                    {
                        //update mapping with new Password
                        if (!string.IsNullOrEmpty(request.Hash) && string.IsNullOrEmpty(user.StrongPasswordHash))
                        {
                            userMapping.PlatformPasswordHash = request.Hash;
                            userMapping.PlatformPasswordSalt = request.Salt;
                            userMapping.PlatformPassowrdMethod = userUniqueID.PlatformPasswordMethod();
                            userMapping.Updated = DateTime.Now;
                        }
                    }

                    int roleTypeID = 0;
                    switch (userUniqueID.SiteType)
                    {
                        case SiteTypeEnum.Epic:
                            if (userUniqueID.AccountType == "learner")
                                roleTypeID = 1; // Catalyst Learner
                            break;

                        case SiteTypeEnum.Catalyst:
                            if (userUniqueID.AccountType == "facilitator")
                                roleTypeID = 2; // Catalyst Facilitator
                            break;

                        default: // No RoleType defined
                            roleTypeID = 0;
                            break;
                    }

                    if (roleTypeID > 0)
                    {
                        UserRole userRole = _userDbContext.UserRoles
                                                .FirstOrDefault(ur => ur.UserID == user.UserID &&
                                                                      ur.RoleTypeID == roleTypeID);
                        if (userRole == null)
                        {
                            _userDbContext.UserRoles.Add(new UserRole
                            {
                                UserID = (int)user.UserID,
                                RoleTypeID = roleTypeID
                            });
                        }
                    }

                    _userDbContext.SaveChanges();

                    response.Status = "Updated";
                }

                return response;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex.Message, $"UserService - UpdateUser - error ");
                //throw any error that occured and let the controller will log it.
                throw new Exception(ex.Message);
            }
        }

        #region V4
        public UserResponseModel CreateUserV4(CreateUserRequestV4Model model)
        {
            UserResponseModel response = new UserResponseModel();

            model.Username = model.Username.ToLower();
            //Find if the user is already there
            var user = _userDbContext.Users.AsNoTracking().FirstOrDefault(u => u.Username == model.Username);
            if (user != null)
            {
                throw new ArgumentException($"User already exists - {model.Username}");
            }

            user = new UserModel();

            //Validate each potential change as a separate change, but then later apply only the changes
            ValidateCreateUserFields(model);

            //Make the changes to the entity
            PopulateCreateUserFields(model, user);

            _userDbContext.Users.Add(user);
            _userDbContext.SaveChanges();

            UserUniqueID userUniqueID = new UserUniqueID(string.Format("epic:singleton:learner:{0}", user.UserID));
            List<UserMappingResponse> userMappingResponses = new List<UserMappingResponse>();
            if (model.UserMappings != null && model.UserMappings.Any())
            {
                foreach (var userMappingModel in model.UserMappings)
                {
                    UserMapping userMapping = new UserMapping()
                    {
                        UserId = user.UserID,
                        PlatformName = userMappingModel.PlatformName,
                        PlatformCustomer = userMappingModel.PlatformCustomer,
                        PlatformRole = userMappingModel.PlatformRole,
                        PlatformUserId = userMappingModel.PlatformUserId,
                        PlatformAccountId = userMappingModel.PlatformAccountId,
                        PlatformData = userMappingModel.PlatformData,
                        Created = DateTime.Now,
                        Updated = DateTime.Now
                    };
                    _userDbContext.UserMappings.Add(userMapping);
                    _userDbContext.SaveChanges();
                    userMappingResponses.Add(
                        new UserMappingResponse
                        {
                            Id = userMapping.Id,
                            UserId = userMapping.UserId,
                            PlatformName = userMapping.PlatformName,
                            PlatformCustomer = userMapping.PlatformCustomer,
                            PlatformRole = userMapping.PlatformRole,
                            PlatformUserId = userMapping.PlatformUserId,
                            CreatedAt = userMapping.Created,
                            CreatedBy = userMapping.CreatedBy,
                            PlatformAccountId = userMapping.PlatformAccountId,
                            PlatformData = userMapping.PlatformData,
                            UpdatedAt = userMapping.Updated,
                            UpdatedBy = userMapping.UpdatedBy,
                        }
                    );
                }
            }

            _userDbContext.SaveChanges();

            if (user.UserID == 0)
                throw new Exception($"User has not been created - {model.Username}");

            _kafkaService.SendUserKafkaMessage(user, "UserUpdated");

            response.Id = user.UserID;
            response.UserId = user.UserID;
            response.CreatedAt = user.CreatedAt;
            response.CreatedBy = user.CreatedBy;
            response.UpdatedAt = user.LastUpdated;
            response.UpdatedBy = user.UpdatedBy;
            response.Username = user.Username;
            response.FirstName = user.FirstName;
            response.LastName = user.LastName;
            response.Email = user.Email;
            response.Status = (UserStatus)user.Status;
            response.AlmId = user.AlmId;
            response.DataConsentDate = user.DataConsentDate;
            response.PrivacyAcceptDate = user.PrivacyAcceptDate;
            response.RecoveryEmail = user.RecoveryEmail;
            response.Language = user.Language;
            response.PhoneNumber = user.PhoneNumber;
            response.AddressLine1 = user.AddressLine1;
            response.AddressLine2 = user.AddressLine2;
            response.City = user.City;
            response.Region = user.Region;
            response.Country = user.Country;
            response.PostalCode = user.PostalCode;
            response.AvatarUrl = user.AvatarUrl;
            response.UserMappings = userMappingResponses;

            return response;
        }

        public UserModel GetUserFromUsername(string Username)
        {

            UserModel user = null;
            try
            {
                user = _userDbContext.Users.FirstOrDefault(u => u.Username == Username);
            }
            catch (NotFoundException ex)
            {
                _logger.LogWarning(ex.Message, $"UserService - GetUser - User NotFound - {Username}");
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex.Message, $"UserService - GetUser - error for user ID - {Username}");
                throw;
            }

            return user;
        }
        public UserResponseModel GetUser(int userId)
        {
            UserResponseModel response = new UserResponseModel();
            try
            {
                UserModel user = null;
                user = _userDbContext.Users.FirstOrDefault(u => u.UserID == userId);
                if (user == null)
                    throw new NotFoundException();

                response.Id = user.UserID;
                response.UserId = user.UserID;
                response.CreatedAt = user.CreatedAt;
                response.CreatedBy = user.CreatedBy;
                response.UpdatedAt = user.LastUpdated;
                response.UpdatedBy = user.UpdatedBy;
                response.Username = user.Username;
                response.FirstName = user.FirstName;
                response.LastName = user.LastName;
                response.Email = user.Email;
                response.Status = (UserStatus)user.Status;
                response.AlmId = user.AlmId;
                response.DataConsentDate = user.DataConsentDate;
                response.PrivacyAcceptDate = user.PrivacyAcceptDate;
                response.RecoveryEmail = user.RecoveryEmail;
                response.Language = user.Language;
                response.PhoneNumber = user.PhoneNumber;
                response.AddressLine1 = user.AddressLine1;
                response.AddressLine2 = user.AddressLine2;
                response.City = user.City;
                response.Region = user.Region;
                response.Country = user.Country;
                response.PostalCode = user.PostalCode;
                response.AvatarUrl = user.AvatarUrl;

                IQueryable<UserMapping> userMappings = _userDbContext.UserMappings.Where(u => u.UserId == user.UserID);
                if (userMappings != null && userMappings.Any())
                {
                    response.UserMappings = new List<UserMappingResponse>();

                    foreach (var userMapping in userMappings)
                    {
                        response.UserMappings.Add(new UserMappingResponse()
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
                }

                return response;
            }
            catch (NotFoundException ex)
            {
                _logger.LogWarning(ex.Message, $"UserService - GetUser - User NotFound - {userId}");
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex.Message, $"UserService - GetUser - error for user ID - {userId}");
                throw;
            }

        }

        public UserResponseModel UpdateUserV4(int userID, UpdateUserRequestV4Model request)
        {
            //A default Status of Complete will be sent for any response that isn't handled
            UserResponseModel response = new UserResponseModel();
            try
            {
                if (request == null)
                    throw new Exception($"UpdateUser - missing request object");

                UserModel user = _userDbContext.Users.FirstOrDefault(u => u.UserID == userID);
                if (user == null) throw new Exception($"AddingUser - invalid userID ({userID})");

                //Validate each potential change as a separate change, but then later apply only the changes
                ValidateUpdateUserFields(request);

                //Make the changes to the entity
                request.Username = request.Username.ToLower();
                PopulateUpdateUserFields(request, user);

                if (_userDbContext.Entry(user).State == EntityState.Modified)
                {
                    user.LastUpdated = DateTime.Now;
                }

                _userDbContext.SaveChanges();
                response.Id = user.UserID;
                response.UserId = user.UserID;
                response.CreatedAt = user.CreatedAt;
                response.CreatedBy = user.CreatedBy;
                response.UpdatedAt = user.LastUpdated;
                response.UpdatedBy = user.UpdatedBy;
                response.Username = user.Username;
                response.FirstName = user.FirstName;
                response.LastName = user.LastName;
                response.Email = user.Email;
                response.Status = (UserStatus)user.Status;
                response.AlmId = user.AlmId;
                response.DataConsentDate = user.DataConsentDate;
                response.PrivacyAcceptDate = user.PrivacyAcceptDate;
                response.RecoveryEmail = user.RecoveryEmail;
                response.Language = user.Language;
                response.PhoneNumber = user.PhoneNumber;
                response.AddressLine1 = user.AddressLine1;
                response.AddressLine2 = user.AddressLine2;
                response.City = user.City;
                response.Region = user.Region;
                response.Country = user.Country;
                response.PostalCode = user.PostalCode;
                response.AvatarUrl = user.AvatarUrl;
                IQueryable<UserMapping> userMappings = _userDbContext.UserMappings.Where(u => u.UserId == user.UserID);
                if (userMappings != null && userMappings.Any())
                {
                    response.UserMappings = new List<UserMappingResponse>();

                    foreach (var userMapping in userMappings)
                    {
                        response.UserMappings.Add(new UserMappingResponse()
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
                }
                _kafkaService.SendUserKafkaMessage(user, "UserUpdated");

                return response;
            }
            catch (FieldValidationException) { throw; }
            catch (Exception ex)
            {
                _logger.LogWarning(ex.Message, $"UserService - UpdateUser - error ");
                //throw any error that occured and let the controller will log it.
                throw new Exception(ex.Message);
            }
        }
        public void DeleteUser(int userId)
        {
            if (userId == 0)
                throw new NotFoundException();

            UserModel user = _userDbContext.Users.FirstOrDefault(u => u.UserID == userId);
            if (user == null)
                throw new NotFoundException();

            //Delete user by emptying PII
            //There are no more User Mapping records, so it's ok to delete the user as well by emptying PII
            bool hasAnyMapping = true;
            try
            {
                hasAnyMapping = _userMappingService.GetUserMappingsByUserId(userId, null, null, null).Items.Any();
            }
            catch (NotFoundException)
            {
                hasAnyMapping = false;
            }

            if (!hasAnyMapping)
            {

                _logger.LogInformation("Soft delete of the user and removing it from keycloack");
                // Remove the user from keycloack, allows to reuse the username for new accounts
                _keycloakService.DeleteUser(user.Username);

                user.Username = "";
                user.Email = "";
                user.FirstName = "";
                user.LastName = "";
                user.OrigPasswordSalt = "";
                user.OrigPasswordHash = "";
                user.StrongPasswordSalt = "";
                user.StrongPasswordHash = "";
                user.Status = (int)UserStatus.Inactive;
                user.LastUpdated = DateTime.Now;

                _userDbContext.SaveChanges();
                _redisService.ClearKey($"RoleAccessReferences_{user.UserID}");

                _kafkaService.SendUserKafkaMessage(user, "UserRemoved");
            }
            else
            {
                throw new InvalidOperationException("User Has active Mappings");
            }
        }

        public List<UserResponseModel> SearchUsersV4(SearchRequestV4Model request, string include, bool strict = true)
        {
            //Validate Search Request
            if (string.IsNullOrEmpty(request.Username) && string.IsNullOrEmpty(request.Email) && string.IsNullOrEmpty(request.Login))
                throw new NotFoundException();

            // Start with the base query
            var query = _userDbContext.Users.AsNoTracking().AsQueryable();

            // Conditionally add filters
            if (!string.IsNullOrEmpty(request.Email))
            {
                query = strict
                    ? query.Where(u => u.Email == request.Email)
                    : query.Where(u => EF.Functions.Like(u.Email, $"%{request.Email}%"));
            }
            if (!string.IsNullOrEmpty(request.Login))
            {
                query = strict
                    ? from user in query
                      join userMapping in _userDbContext.UserMappings on user.UserID equals userMapping.UserId
                      where userMapping.PlatformAccountId == request.Login
                      select user
                    : from user in query
                      join userMapping in _userDbContext.UserMappings on user.UserID equals userMapping.UserId
                      where EF.Functions.Like(userMapping.PlatformAccountId, $"%{request.Login}%")
                      select user;
            }
            if (!string.IsNullOrEmpty(request.Username))
            {
                query = strict
                    ? query.Where(u => u.Username == request.Username)
                    : query.Where(u => EF.Functions.Like(u.Username, $"%{request.Username}%"));
            }

            List<UserModel> users = query.ToList();

            if (users == null || users.Count == 0)
                throw new NotFoundException();

            include ??= "";
            bool includeBasic = include.Contains("basic");
            bool includeDetails = include.Contains("details");
            bool includeMapping = include.Contains("mapping");

            List<UserResponseModel> response = new List<UserResponseModel>(users.Count);
            foreach (UserModel user in users)
            {
                UserResponseModel userResponse = new UserResponseModel();
                userResponse.UserId = user.UserID;
                if (includeBasic || includeDetails)
                {
                    userResponse.Id = user.UserID;
                    userResponse.CreatedAt = user.CreatedAt;
                    userResponse.UpdatedAt = user.LastUpdated;
                    userResponse.Username = user.Username;
                    userResponse.FirstName = user.FirstName;
                    userResponse.LastName = user.LastName;
                    userResponse.Status = (UserStatus)user.Status;
                    userResponse.Language = user.Language;
                    userResponse.AvatarUrl = user.AvatarUrl;
                }
                if (includeDetails)
                {
                    userResponse.CreatedBy = user.CreatedBy;
                    userResponse.UpdatedBy = user.UpdatedBy;
                    userResponse.Email = user.Email;
                    userResponse.AlmId = user.AlmId;
                    userResponse.DataConsentDate = user.DataConsentDate;
                    userResponse.PrivacyAcceptDate = user.PrivacyAcceptDate;
                    userResponse.RecoveryEmail = user.RecoveryEmail;
                    userResponse.PhoneNumber = user.PhoneNumber;
                    userResponse.AddressLine1 = user.AddressLine1;
                    userResponse.AddressLine2 = user.AddressLine2;
                    userResponse.City = user.City;
                    userResponse.Region = user.Region;
                    userResponse.Country = user.Country;
                    userResponse.PostalCode = user.PostalCode;
                }
                if (includeMapping)
                {
                    IQueryable<UserMapping> userMappings = _userDbContext.UserMappings.Where(u => u.UserId == user.UserID);
                    if (userMappings != null && userMappings.Any())
                    {
                        userResponse.UserMappings = new List<UserMappingResponse>();

                        foreach (var userMapping in userMappings)
                        {
                            userResponse.UserMappings.Add(new UserMappingResponse()
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
                    }
                }
                response.Add(userResponse);
            }

            return response;
        }

        #endregion

        private const string BASE_UNIQUEID_EXPRESSION = @"^[\w]+:[\w]+:[\w]+:[\d]+$";
        private static Regex validUniqueIDRegEx = new Regex(BASE_UNIQUEID_EXPRESSION);

        private const string BASE_USERNAME_EXPRESSION = @"^[0-9a-zA-Z][0-9a-zA-Z_@\-\+\.]{5,100}$";

        //private const string BASE_EMAIL_AS_USERNAME_EXPRESSION = @"^[\w][\w\.\-\'\+]*@[\w\.-]+\.[\w][\w][\w]*$";
        private static Regex validUsernameRegEx = new Regex(BASE_USERNAME_EXPRESSION);

        private static Regex validEmailAsUsernameRegEx = new Regex(BASE_EMAIL_VALIDATION_EXPRESSION);

        private const string BASE_EMAIL_VALIDATION_EXPRESSION = @"^[\#\$\%\&\'\*\+\/\=\?\^\`\{\|\}\~\!A-Za-z0-9_\.-]+@[A-Za-z0-9_\.-]+\.[A-Za-z0-9_][A-Za-z0-9_][A-Za-z0-9_]*$";
        private static Regex validEmailRegEx = new Regex(BASE_EMAIL_VALIDATION_EXPRESSION);

        /// <summary>
        /// Check for Valid Unique ID
        /// </summary>
        /// <remarks>
        /// A valid Unique ID is any string that is four parts separated by colons "source:instance:type:value"
        /// Where source is a company or app like "EPIC", "PAC", "WLS", "CK", "CKLS"
        /// And instance is a "singleton" for one main app or a client name like "CocaCola" or "Loreal"
        /// And type is user type like "learner", "admin", or "facilitator"
        /// And value is a localized app representation that is unique with that source:instance:type
        /// </remarks>
        /// <param name="uniqueID"></param>
        /// <returns></returns>
        public static bool IsValidUniqueID(string uniqueID)
        {
            bool result = false;

            //Simple test if the Unique ID is in the right format
            result = validUniqueIDRegEx.IsMatch(uniqueID);
            if (!result)
                return result;

            return result;
        }

        public static bool IsValidUsername(string username)
        {
            bool result = false;

            if (username.Contains("@"))
                result = validEmailAsUsernameRegEx.IsMatch(username);
            else
                result = validUsernameRegEx.IsMatch(username);

            return result;
        }

        public static bool IsValidEmailAddress(string emailAddress)
        {
            bool result = false;

            //We were already testing for 2, 3, and 4 character top-level domains.
            //But there are two additional top-level domains named .travel and .museum that we need to accomodate
            //This next code merely replaces them to allow for the match to suceed.
            //It won't matter if .travel was elsewhere in the email address as we're not changing the character types, but just the number of characters
            //We're also lowercasing the email address which will also not cause problems

            if (emailAddress.IndexOf("..") >= 0 || //The regular expression can't pickup on this typo..., so just return false
                emailAddress.IndexOf(".@") >= 0 ||
                emailAddress.IndexOf("@.") >= 0 ||
                emailAddress.IndexOf(".") == 0 ||
                emailAddress.LastIndexOf(".") == emailAddress.Length - 1)
            {
                return result;
            }

            result = validEmailRegEx.IsMatch(emailAddress);

            return result;
        }

        public void DeleteUser(string uniqueID)
        {
            if (!IsValidUniqueID(uniqueID))
                throw new NotFoundException();

            UserUniqueID userUniqueID = new UserUniqueID(uniqueID); //For now, the uniqueID class is still ok, but not all UniqueID.AccountIDs are UserIDs
            UserModel user = _userDbContext.Users.FirstOrDefault(u => u.UniqueID == uniqueID);
            if (user == null)
            {
                //The user record could have been created from another site first.  The only system calling DeleteUser by UniqueID is
                // UserUpdateService which is doing so because the EPIC Learner was deleted
                if (userUniqueID.SiteType == SiteTypeEnum.Epic && userUniqueID.AccountType == "learner")
                {
                    //Eclipse UserUpdateService could send a request.Username update request, so we can't guarantee it's our key
                    //But LearnerID == UserID
                    user = _userDbContext.Users.FirstOrDefault(u => u.UserID == userUniqueID.AccountID);
                }
            }
            if (user == null)
                throw new NotFoundException();

            List<UserMapping> userMappings = _userDbContext.UserMappings.Where(u => u.UserId == user.UserID).ToList();
            if (userMappings != null)
            {
                UserMapping userMapping = userMappings.FirstOrDefault(u =>
                                            u.PlatformName == userUniqueID.PlatformName &&
                                            u.PlatformCustomer == userUniqueID.Instance &&
                                            u.PlatformRole == userUniqueID.AccountType);
                if (userMapping != null)
                {
                    userMappings.Remove(userMapping);
                    _userDbContext.UserMappings.Remove(userMapping);
                }
            }

            if (userMappings == null || userMappings.Count == 0)
            {
                //There are no more User Mapping records, so it's ok to delete the user as well by emptying PII
                user.Username = "";
                user.Email = "";
                user.FirstName = "";
                user.LastName = "";
                user.OrigPasswordSalt = "";
                user.OrigPasswordHash = "";
                user.Status = (int)UserStatus.Inactive;
                user.LastUpdated = DateTime.Now;
            }

            _userDbContext.SaveChanges();
            _redisService.ClearKey($"RoleAccessReferences_{user.UserID}");
        }

        public UserRole AddUserRole(UserRoleRequest request)
        {
            var userRole = new UserRole
            {
                UserID = request.UserID,
                RoleTypeID = request.RoleTypeID
            };

            if (!_userDbContext.Users.Any(x => x.UserID == request.UserID) || !_userDbContext.RoleTypes.Any(x => x.RoleTypeID == request.RoleTypeID))
                throw new NotFoundException();
            else if (_userDbContext.UserRoles.Any(x => x.UserID == userRole.UserID && x.RoleTypeID == userRole.RoleTypeID))
                throw new UserRoleExistsException();

            _userDbContext.UserRoles.Add(userRole);
            _userDbContext.SaveChanges();
            _redisService.ClearKey($"RoleAccessReferences_{request.UserID}");
            return userRole;
        }

        public bool DeleteUserRole(UserRoleRequest request)
        {
            var userRole = _userDbContext.UserRoles.FirstOrDefault(x => x.UserID == request.UserID && x.RoleTypeID == request.RoleTypeID);
            if (userRole == null)
                throw new NotFoundException();

            _userDbContext.UserRoles.Remove(userRole);
            _userDbContext.SaveChanges();
            _redisService.ClearKey($"RoleAccessReferences_{request.UserID}");
            return true;
        }

        public List<RoleType> GetUserRoles(int userID)
        {
            var user = _userDbContext.Users.FirstOrDefault(x => x.UserID == userID);
            if (user == null)
                throw new NotFoundException();

            //var roles = _userDbContext.UserRoles.Where(ur => ur.UserID == userID).ToList();
            //var roleTypes = _userDbContext.RoleTypes.Where(rt => roles.Select(r => r.RoleTypeID).Contains(rt.RoleTypeID)).ToList();
            var result = _userDbContext.UserRoles
                .AsNoTracking()
                .Where(ur => ur.UserID == userID)
                .Join(_userDbContext.RoleTypes,
                    roles => roles.RoleTypeID, roleTypes => roleTypes.RoleTypeID,
                    (roles, roleTypes) => new { roles, roleTypes })
                .Select(x => x.roleTypes).ToList();

            //return (from ur in _userDbContext.UserRoles
            //        join rt in _userDbContext.RoleTypes on ur.RoleTypeID equals rt.RoleTypeID
            //        where ur.UserID == userID
            //        select rt).ToList();

            return result;
        }

        public AccessType GetAccessTypeByName(string accessTypeName)
        {
            var accessType = _userDbContext.AccessTypes.FirstOrDefault(x => x.AccessTypeName == accessTypeName);
            if (accessType == null)
                throw new NotFoundException();
            else
                return accessType;
        }

        public List<UserRoleAccess> AddUserRoleAccess(UserRoleAccessRequest request)
        {
            if (!_userDbContext.Users.Any(x => x.UserID == request.UserID))
                throw new NotFoundException(string.Format($"UserID '{0}' not found.", request.UserID));
            else if (!_userDbContext.Users.Any(x => x.UserID == request.GrantedBy))
                throw new NotFoundException(string.Format($"GrantedBy '{0}' not found.", request.GrantedBy));
            else if (!_userDbContext.AccessTypes.Any(x => x.AccessTypeID == request.AccessTypeID))
                throw new NotFoundException(string.Format($"AccessType '{0}' not found.", request.AccessTypeID));

            var userRole = _userDbContext.UserRoles.FirstOrDefault(x => x.UserID == request.UserID && x.RoleTypeID == request.RoleTypeID);
            if (userRole == null)
            {
                throw new NotFoundException(string.Format("UserRole for UserID '{0}' and RoleTypeID '{1}' not found.",
                    request.UserID, request.RoleTypeID));
            }
            else
            {
                var list = new List<UserRoleAccess>();

                // Remove any duplicate AccessRefIDs from list
                request.AccessRefIDs.RemoveAll(delegate (int val)
                {
                    return _userDbContext.UserRoleAccess.Any(x => x.UserRoleID == userRole.UserRoleID && x.AccessTypeID == request.AccessTypeID &&
                                                                  request.AccessRefIDs.Contains(x.AccessRefID) && x.AccessRefID == val);
                });

                foreach (int accessRefID in request.AccessRefIDs)
                {
                    list.Add(new UserRoleAccess
                    {
                        UserRoleID = userRole.UserRoleID,
                        AccessTypeID = request.AccessTypeID,
                        AccessRefID = accessRefID,
                        GrantedBy = request.GrantedBy
                    });
                }

                _userDbContext.UserRoleAccess.AddRange(list);
                _userDbContext.SaveChanges();
                _redisService.ClearKey($"RoleAccessReferences_{request.UserID}");
                return list;
            }
        }

        public bool DeleteUserRoleAccess(UserRoleAccessRequest request)
        {
            var userRole = _userDbContext.UserRoles.FirstOrDefault(x => x.UserID == request.UserID && x.RoleTypeID == request.RoleTypeID);
            if (userRole == null)
            {
                throw new NotFoundException(string.Format("UserRole for UserID '{0}' and RoleTypeID '{1}' not found.",
                    request.UserID, request.RoleTypeID));
            }

            var userRoleAccess = _userDbContext.UserRoleAccess.FirstOrDefault(x => x.AccessTypeID == request.AccessTypeID &&
                                                                                   x.UserRoleID == userRole.UserRoleID && request.AccessRefIDs.Contains(x.AccessRefID));
            if (userRoleAccess == null)
                throw new NotFoundException(string.Format($"UserRoleAccess with UserRoleID '{0}' AccessTypeID '{1}' and AccessRefIDs '{2}' not found.",
                    userRole.UserRoleID, request.AccessTypeID, string.Join(",", request.AccessRefIDs)));

            var list = _userDbContext.UserRoleAccess.Where(x => x.UserRoleID == userRole.UserRoleID && x.AccessTypeID == request.AccessTypeID
                                                                                                    && request.AccessRefIDs.Contains(x.AccessRefID)).ToList();
            if (list != null)
            {
                _userDbContext.UserRoleAccess.RemoveRange(list);
                _userDbContext.SaveChanges();
                _redisService.ClearKey($"RoleAccessReferences_{request.UserID}");
            }

            return true;
        }

        public List<UserRoleAccess> GetUserRoleAccess(int userID)
        {
            var user = _userDbContext.Users.FirstOrDefault(x => x.UserID == userID);
            if (user == null)
                throw new NotFoundException();

            return (from u in _userDbContext.Users
                    join ur in _userDbContext.UserRoles on u.UserID equals ur.UserID
                    join ura in _userDbContext.UserRoleAccess on ur.UserRoleID equals ura.UserRoleID
                    where u.UserID == userID
                    select ura).ToList();
        }

        public List<UserRoleResponse> GetUserRoleResponses(int userID)
        {
            var userRoleResponses = new List<UserRoleResponse>();
            var user = _userDbContext.Users.FirstOrDefault(x => x.UserID == userID);
            if (user == null)
                throw new NotFoundException();

            var query = from ur in _userDbContext.UserRoles
                        join rt in _userDbContext.RoleTypes on ur.RoleTypeID equals rt.RoleTypeID
                        where ur.UserID == userID
                        select new UserRoleResponse
                        {
                            UserRole = ur,
                            RoleType = rt,
                            UserRoleAccess = _userDbContext.UserRoleAccess.Where(ura => ur.UserRoleID == ura.UserRoleID).ToList()
                        };

            if (query.Count() > 0)
            {
                // TODO: Is there a way to simplify this using Distinct, Aggregate, etc?
                foreach (RoleTypeEnum roleType in Enum.GetValues(typeof(RoleTypeEnum)))
                {
                    var urr = query.ToList().FirstOrDefault(x => x.RoleType.RoleTypeID == (int)roleType);
                    if (urr != null) userRoleResponses.Add(urr);
                }
            }

            return userRoleResponses;
        }

        public DateTime GetLastLoginDate(string uniqueID, int offset = 0)
        {
            UserModel user = GetUserFromUniqueID(uniqueID);
            if (user == null)
            {
                throw new NotFoundException($"GetLastLoginDate - UniqueID does not exist {uniqueID}");
            }

            LoginAttempt login = _userDbContext.LoginAttempts.OrderByDescending(x => x.Attempted).Where(u => u.UserID == user.UserID && u.Success == true).Skip(offset).FirstOrDefault();
            if (login == null)
            {
                throw new NotFoundException($"GetLastLoginDate - No record in position {offset} for user {user.UserID} having logged in.");
            }


            return login.Attempted;
        }

        public int LoginCount(string uniqueId)
        {
            var user = GetUserFromUniqueID(uniqueId);
            if (user == null)
                return 0; //If there is no user the count would be zero.  (This will handle a unique situation after CreateAccount where the data may be propogated yet.)

            var attempts = _userDbContext.LoginAttempts.Count(q => q.UserID == user.UserID && q.Success);
            return attempts;
        }

        public UserModel GetUserFromUniqueID(string uniqueID)
        {
            UserModel user = _userDbContext.Users.FirstOrDefault(u => u.UniqueID == uniqueID);

            if (user == null)
            {
                UserUniqueID userUniqueID = new UserUniqueID(uniqueID);
                if ((userUniqueID.SiteType == SiteTypeEnum.Epic && userUniqueID.AccountType == "learner"))
                {
                    user = _userDbContext.Users.FirstOrDefault(u => u.UserID == userUniqueID.AccountID);
                }
                else if (userUniqueID.SiteType == SiteTypeEnum.LPI && userUniqueID.AccountType == "facilitator")
                {
                    //User was not an LPI Facilitator first because UniqueID does not match.  The values here are only available in UserMapping table

                    var userMapping = _userDbContext.UserMappings.AsNoTracking().FirstOrDefault(u =>
                        u.PlatformName == userUniqueID.PlatformName &&
                        u.PlatformRole == userUniqueID.AccountType &&
                        u.PlatformUserId == userUniqueID.PlatformUserID);
                    if (userMapping != null)
                    {
                        user = _userDbContext.Users.FirstOrDefault(u => u.UserID == userMapping.UserId);
                    }
                }
            }

            return user;
        }

        //login assumes unique usernames
        public async Task<UserModel> Login(string userName, string password)
        {
            if (string.IsNullOrEmpty(userName))
                throw new ArgumentNullException("userName");
            else if (string.IsNullOrEmpty(password))
                throw new ArgumentNullException("password");

            try
            {
                List<UserModel> matches;
                List<UserModel> badMatches = null;
                (matches, badMatches) = await FindMatches(userName, password);

                if (matches.Count > 0)
                {
                    LogLoginAttempt(matches.First(), null, true);
                }
                else if (badMatches.Count > 0)
                {
                    LogLoginAttempt(badMatches.First(), null, false);
                }

                if (matches.Count == 0 || badMatches.Count > 0)
                {
                    _logger.LogWarning($"UserService - {0}", matches.Count == 0 ? "Not Found" : "Failed");
                    throw new AuthenticationFailedException();
                }

                UserModel user = matches[0];

                if (user.StrongPasswordGoodUntil == null || user.StrongPasswordGoodUntil < DateTime.Today)
                {
                    throw new PasswordExpiredException("Your password has expired. Please choose a new password.");
                }

                return user;
            }
            catch (PasswordExpiredException ex)
            {
                _logger.LogWarning($"Failed to log in user: {userName} password expired.");
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "UserService - Login Failed");
                throw new AuthenticationFailedException();
            }
        }

        public UserModel GetUserModelByUsername(string username)
        {
            try
            {
                username = username.ToLower();
                UserModel user = _userDbContext.Users.FirstOrDefault(u => u.Username == username);

                if (user is null)
                    throw new NotFoundException($"UserService - GetUserByUsername - User NotFound - {username}");

                return user;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex.Message, $"UserService - GetUserByUsername - error for user ID - {username}");
                throw;
            }
        }

        public UserModel GetUserModel(int userId)
        {
            try
            {
                UserModel user = _userDbContext.Users.FirstOrDefault(u => u.UserID == userId);

                if (user is null)
                    throw new NotFoundException($"UserService - GetUserByUsername - User NotFound - {userId}");

                return user;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex.Message, $"UserService - GetUser - error for user ID - {userId}");
                throw;
            }
        }

        public async Task RecoverPassword(RecoverPasswordRequestV4 request)
        {
            var user = GetUserModelByUsername(request.Username);

            if (user is null) throw new NotFoundException($"User does not exist. {request.Username}");

            var functionCode = Guid.NewGuid().ToString();
            var validateCodeObject = new RecoverPasswordObject { Key = functionCode, Data = new ValidateObject { UserId = user.UserID, FunctionType = FunctionType.ResetPassword.ToString(), CreatedAt = DateTime.Now } };
            var value = JsonConvert.SerializeObject(validateCodeObject);

            var emailResult = await _emailAPIService.RequestRecoverPassword(user, validateCodeObject.Key, request.SiteType);
            if (!emailResult)
            {
                throw new EmailCallException();
            }

            var key = $"{FunctionType.ResetPassword}:{functionCode}";
            _redisService.SetString("RecoverPassword", key, value);
        }

        public bool ValidateFunctionCode(string code)
        {
            try
            {
                string functionCode = _redisService.GetString($"{FunctionType.ResetPassword}:{code}");
                if (string.IsNullOrEmpty(functionCode))
                {
                    return false;
                }

                var validateCodeObject = JsonConvert.DeserializeObject<RecoverPasswordObject>(functionCode);
                var user = _userDbContext.Users.FirstOrDefault(x => x.UserID == validateCodeObject.Data.UserId);

                return ValidateRecoverPassword(validateCodeObject.Data.ProcessedOn, user);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error on Validate Function Code: {ex.Message}");
                throw;
            }
        }

        public bool ChangePassword(UserChangePasswordRequest changePassword, int userId = 0)
        {
            try
            {
                var redisKey = $"{FunctionType.ResetPassword}:{changePassword.Code}";
                RecoverPasswordObject recoverObject = null;
                UserModel user = null;

                if (userId == 0)
                {
                    try
                    {
                        recoverObject = _redisService.Get<RecoverPasswordObject>(redisKey);
                    }
                    catch
                    {
                        _logger.LogInformation($"Function code does not exist");
                        return false;
                    }

                    user = _userDbContext.Users.FirstOrDefault(x => x.UserID == recoverObject.Data.UserId);

                    if (!ValidateRecoverPassword(recoverObject.Data.ProcessedOn, user))
                    {
                        _logger.LogInformation($"Invalid function code");
                        return false;
                    }
                }
                else
                {
                    user = _userDbContext.Users.FirstOrDefault(x => x.UserID == userId);
                    if (user is null)
                    {
                        throw new NotFoundException();
                    }
                }

                SetPasswordSalt(user, changePassword.NewPassword);

                _userDbContext.Users.Update(user);
                _userDbContext.SaveChanges();

                if (userId == 0)
                {
                    recoverObject.Data.ProcessedOn = DateTime.UtcNow;
                    var validateCodeObject = new RecoverPasswordObject { Key = recoverObject.Key, Data = recoverObject.Data };
                    var value = JsonConvert.SerializeObject(validateCodeObject);
                    _redisService.SetString("RecoverPassword", redisKey, value);
                }

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Change-Password - Error on update password - {ex.Message}");
                throw;
            }
        }

        public ValidateCodeResponse FunctionCode(string code)
        {
            try
            {
                string functionCode = _redisService.GetString($"{FunctionType.ResetPassword}:{code}");
                if (string.IsNullOrEmpty(functionCode))
                {
                    throw new NotFoundException();
                }

                var validateCodeObject = JsonConvert.DeserializeObject<RecoverPasswordObject>(functionCode);
                var user = _userDbContext.Users.FirstOrDefault(x => x.UserID == validateCodeObject.Data.UserId);

                if (!ValidateRecoverPassword(validateCodeObject.Data.ProcessedOn, user))
                {
                    throw new BadRequestException("Code already used. Please ask new code.");
                }
                var response = new ValidateCodeResponse
                {
                    UserId = user.UserID,
                    Permission = true
                };
                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "FunctionCode - Error - {code}", code);
                throw;
            }
        }
        public async Task GenerateKafkaEvents(UserDbContext dbContext)
        {
            var recordsProcessedCount = 0;
            var totalRecordsCount = 0;
            var lastId = -1;
            using (dbContext)
                try
                {
                    var firstRecord = dbContext.Users
                        .OrderByDescending(x => x.UserID)
                        .AsNoTracking() // do not keep track of records in unit of work to prevent OOM exception
                        .Take(1)
                        .First();

                    if (null == firstRecord)
                    {
                        _logger.LogInformation("GenerateKafkaEvents - Skipping, no records found");
                        return;
                    }

                    totalRecordsCount = dbContext.Users.Count();
                    _logger.LogInformation($"GenerateKafkaEvents - Start, first record: {firstRecord.UserID}, total records count: {totalRecordsCount}");
                    await _kafkaService.SendUserKafkaMessage(firstRecord, "UserUpdated");
                    recordsProcessedCount++;
                    lastId = firstRecord.UserID;
                    List<UserModel> users;
                    do
                    {
                        users = dbContext.Users
                            .Include(u => u.UserMappings) // eager-load relationships
                            .Where(x => x.UserID < lastId) // use keyset pagination
                            .AsNoTracking() // do not keep track of records in unit of work to prevent OOM exception
                            .OrderByDescending(x => x.UserID)
                            .Take(100)
                            .ToList();
                        foreach (var user in users)
                        {
                            await _kafkaService.SendUserKafkaMessage(user, "UserUpdated");
                            lastId = user.UserID;
                            recordsProcessedCount++;
                        }
                        _logger.LogDebug($"GenerateKafkaEvents - Generate {users.Count} events, {recordsProcessedCount} out of {totalRecordsCount}, ids: {users.FirstOrDefault()?.UserID} - {users.LastOrDefault()?.UserID}");
                    }
                    while (users.Count > 0);
                    _logger.LogInformation($"GenerateKafkaEvents - End, last record: {lastId}, records processed: {recordsProcessedCount}, expected number of records: {totalRecordsCount}");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"GenerateKafkaEvents - Error, last record: {lastId}, records processed: {recordsProcessedCount}, expected number of records: {totalRecordsCount}");
                }
        }

        #region Private Helpers
        private bool ValidateRecoverPassword(DateTime? processedOn, UserModel user)
        {
            if (processedOn != null)
            {
                _logger.LogInformation("Code already used. Please ask new code.");
                return false;
            }

            if (user == null)
            {
                _logger.LogInformation($"User does not exist.");
                return false;
            }

            return true;
        }

        private void ConvertAvatarImage(ImageAPISaveImageRequest file, UserModel user)
        {
            //TODO: Create the logic to work with images

            //ImageAPI requires the image data to only be the base 64 string, no metadata at the beginning
            // Ex: data:image/png;base64,iVBORw0KGgoAAA... needs to remove everything before the comma

            //var splitImageData = file.ImageData.Split(',');
            //file.ImageData = splitImageData.Last();
            //ImageAPISaveImageResponse response = await _imageService.CreateAvatar(request);
            //user.AvatarUrl = response.path;

            //Placeholder
            user.AvatarUrl = "/" + user.Username + "/";
        }

        private void PopulateUpdateUserFields(UpdateUserRequestV4Model request, UserModel user)
        {
            if (!string.IsNullOrEmpty(request.Username) && request.Username != user.Username)
                user.Username = request.Username;
            if (request.FirstName != null && request.FirstName != user.FirstName)
                user.FirstName = request.FirstName.Replace(EMPTY_STRING, "");
            if (request.LastName != null && request.LastName != user.LastName)
                user.LastName = request.LastName.Replace(EMPTY_STRING, "");

            if (user.Status != (int)request.Status)
                user.Status = (int)request.Status;

            if (request.Email != null && request.Email != user.Email)
                user.Email = request.Email;

            if (!string.IsNullOrEmpty(request.AlmId) && request.AlmId != user.AlmId)
                user.AlmId = request.AlmId.Replace(EMPTY_STRING, "");

            if (request.DataConsentDate.HasValue && request.DataConsentDate != user.DataConsentDate)
                user.DataConsentDate = request.DataConsentDate;

            if (request.PrivacyAcceptDate.HasValue && request.PrivacyAcceptDate != user.PrivacyAcceptDate)
                user.PrivacyAcceptDate = request.PrivacyAcceptDate;

            if (!string.IsNullOrEmpty(request.RecoveryEmail) && request.RecoveryEmail != user.RecoveryEmail)
                user.RecoveryEmail = request.RecoveryEmail.Replace(EMPTY_STRING, "");

            if (!string.IsNullOrEmpty(request.Language) && request.Language != user.Language)
                user.Language = request.Language.Replace(EMPTY_STRING, "");

            if (!string.IsNullOrEmpty(request.PhoneNumber) && request.PhoneNumber != user.PhoneNumber)
                user.PhoneNumber = request.PhoneNumber.Replace(EMPTY_STRING, "");

            if (!string.IsNullOrEmpty(request.AddressLine1) && request.AddressLine1 != user.AddressLine1)
                user.AddressLine1 = request.AddressLine1.Replace(EMPTY_STRING, "");

            if (!string.IsNullOrEmpty(request.AddressLine2) && request.AddressLine2 != user.AddressLine2)
                user.AddressLine2 = request.AddressLine2.Replace(EMPTY_STRING, "");

            if (!string.IsNullOrEmpty(request.City) && request.City != user.City)
                user.City = request.City.Replace(EMPTY_STRING, "");

            if (!string.IsNullOrEmpty(request.Region) && request.Region != user.Region)
                user.Region = request.Region.Replace(EMPTY_STRING, "");

            if (!string.IsNullOrEmpty(request.Country) && request.Country != user.Country)
                user.Country = request.Country.Replace(EMPTY_STRING, "");

            if (!string.IsNullOrEmpty(request.PostalCode) && request.PostalCode != user.PostalCode)
                user.PostalCode = request.PostalCode.Replace(EMPTY_STRING, "");

            if (request.AvatarImage != null && request.AvatarImage.ImageData.Length > 0 && request.AvatarImage.ImageType.Length > 0)
                ConvertAvatarImage(request.AvatarImage, user);

            if (!string.IsNullOrEmpty(request.Password))
                SetPasswordSalt(user, request.Password);
        }

        private void ValidateUpdateUserFields(UpdateUserRequestV4Model request)
        {
            if (!string.IsNullOrEmpty(request.Username))
            {
                if (request.Username.Length < 8 || request.Username.Length > 100)
                    throw new FieldValidationException($"UpdateUser - invalid Username ({request.Username})");
            }

            if (!string.IsNullOrEmpty(request.FirstName))
            {
                if (request.FirstName.Length > 100)
                    throw new FieldValidationException($"UpdateUser - invalid FirstName ({request.FirstName})");
            }

            if (!string.IsNullOrEmpty(request.LastName))
            {
                if (request.LastName.Length > 100)
                    throw new FieldValidationException($"UpdateUser - invalid LastName ({request.LastName})");
            }

            if (!string.IsNullOrEmpty(request.Email))
            {
                if (request.Email.Length > 255 || !IsValidUsername(request.Email))
                    throw new FieldValidationException($"CreateUser - invalid Email ({request.Email})");
            }
            if (!string.IsNullOrEmpty(request.Password) && (request.Password.Length < 8 || request.Password.Length > 50))
                throw new FieldValidationException($"UpdateUser - invalid Password ({request.Password})");

            if (!string.IsNullOrEmpty(request.AlmId) && request.AlmId.Length > 255)
                throw new FieldValidationException($"UpdateUser - invalid AlmId ({request.AlmId})");

            if (!string.IsNullOrEmpty(request.RecoveryEmail) && request.RecoveryEmail.Length > 255)
                throw new FieldValidationException($"UpdateUser - invalid RecoveryEmail ({request.RecoveryEmail})");
            else if (!string.IsNullOrEmpty(request.RecoveryEmail) && !IsValidEmailAddress(request.RecoveryEmail))
                throw new FieldValidationException($"UpdateUser - invalid RecoveryEmail ({request.RecoveryEmail})");

            if (!string.IsNullOrEmpty(request.Language) && request.Language.Length > 50)
                throw new FieldValidationException($"UpdateUser - invalid Language ({request.Language})");

            if (!string.IsNullOrEmpty(request.PhoneNumber) && request.PhoneNumber.Length > 50)
                throw new FieldValidationException($"UpdateUser - invalid PhoneNumber ({request.PhoneNumber})");

            if (!string.IsNullOrEmpty(request.AddressLine1) && request.AddressLine1.Length > 120)
                throw new FieldValidationException($"UpdateUser - invalid AddressLine1 ({request.AddressLine1})");

            if (!string.IsNullOrEmpty(request.AddressLine2) && request.AddressLine2.Length > 100)
                throw new FieldValidationException($"UpdateUser - invalid AddressLine2 ({request.AddressLine2})");

            if (!string.IsNullOrEmpty(request.City) && request.City.Length > 50)
                throw new FieldValidationException($"UpdateUser - invalid City ({request.City})");

            if (!string.IsNullOrEmpty(request.Region) && request.Region.Length > 50)
                throw new FieldValidationException($"UpdateUser - invalid Region ({request.Region})");

            if (!string.IsNullOrEmpty(request.Country) && request.Country.Length > 50)
                throw new FieldValidationException($"UpdateUser - invalid Country ({request.Country})");

            if (!string.IsNullOrEmpty(request.PostalCode) && request.PostalCode.Length > 50)
                throw new FieldValidationException($"UpdateUser - invalid PostalCode ({request.PostalCode})");

        }

        private void PopulateCreateUserFields(CreateUserRequestV4Model request, UserModel user)
        {
            user.Username = request.Username;
            user.FirstName = request.FirstName;
            user.LastName = request.LastName;

            if (request.AlmId != null)
                user.AlmId = request.AlmId;

            if (request.Email != null)
                user.Email = request.Email;

            if (request.DataConsentDate.HasValue)
                user.DataConsentDate = request.DataConsentDate;

            if (request.PrivacyAcceptDate.HasValue)
                user.PrivacyAcceptDate = request.PrivacyAcceptDate;

            if (request.RecoveryEmail != null)
                user.RecoveryEmail = request.RecoveryEmail;

            if (request.Language != null)
                user.Language = request.Language;

            if (request.PhoneNumber != null)
                user.PhoneNumber = request.PhoneNumber;

            if (request.AddressLine1 != null)
                user.AddressLine1 = request.AddressLine1;

            if (request.AddressLine2 != null)
                user.AddressLine2 = request.AddressLine2;

            if (request.City != null)
                user.City = request.City;

            if (request.Region != null)
                user.Region = request.Region;

            if (request.Country != null)
                user.Country = request.Country;

            if (request.PostalCode != null)
                user.PostalCode = request.PostalCode;

            if (request.AvatarImage != null && request.AvatarImage.ImageData.Length > 0)
                ConvertAvatarImage(request.AvatarImage, user);

            if (request.Password != null)
                SetPasswordSalt(user, request.Password);

            user.CreatedAt = DateTime.Now;
            user.LastUpdated = DateTime.Now;
            user.Status = (int)UserStatus.Active;
        }

        private void ValidateCreateUserFields(CreateUserRequestV4Model request)
        {
            if (!string.IsNullOrEmpty(request.Username))
            {
                if (request.Username.Length < 8 || request.Username.Length > 100)
                    throw new FieldValidationException($"CreateUser - invalid Username ({request.Username})");
            }
            else
                throw new FieldValidationException($"CreateUser - invalid Username ({request.Username})");

            if (!string.IsNullOrEmpty(request.FirstName))
            {
                if (request.FirstName.Length > 100)
                    throw new FieldValidationException($"CreateUser - invalid FirstName ({request.FirstName})");
            }

            if (!string.IsNullOrEmpty(request.LastName))
            {
                if (request.LastName.Length > 100)
                    throw new FieldValidationException($"CreateUser - invalid LastName ({request.LastName})");
            }

            if (!string.IsNullOrEmpty(request.Password))
            {
                if (request.Password.Length < 8 || request.Password.Length > 50)
                    throw new FieldValidationException($"CreateUser - invalid Password ({request.Password})");
            }
            else
                throw new FieldValidationException($"CreateUser - invalid Password ({request.Password})");

            if (!string.IsNullOrEmpty(request.AlmId) && request.AlmId.Length > 255)
                throw new FieldValidationException($"CreateUser - invalid AlmId ({request.AlmId})");


            if (!string.IsNullOrEmpty(request.Email))
            {
                if (request.Email.Length > 255 || !IsValidUsername(request.Email))
                    throw new FieldValidationException($"CreateUser - invalid Email ({request.Email})");
            }

            if (!string.IsNullOrEmpty(request.RecoveryEmail))
            {
                if (request.RecoveryEmail.Length > 255)
                    throw new FieldValidationException($"CreateUser - invalid RecoveryEmail ({request.RecoveryEmail})");
                else if (!IsValidEmailAddress(request.RecoveryEmail))
                    throw new FieldValidationException($"CreateUser - invalid RecoveryEmail ({request.RecoveryEmail})");
            }

            if (!string.IsNullOrEmpty(request.Language) && request.Language.Length > 50)
                throw new FieldValidationException($"CreateUser - invalid Language ({request.Language})");

            if (!string.IsNullOrEmpty(request.PhoneNumber) && request.PhoneNumber.Length > 50)
                throw new FieldValidationException($"CreateUser - invalid PhoneNumber ({request.PhoneNumber})");

            if (!string.IsNullOrEmpty(request.AddressLine1) && request.AddressLine1.Length > 120)
                throw new FieldValidationException($"CreateUser - invalid AddressLine1 ({request.AddressLine1})");

            if (!string.IsNullOrEmpty(request.AddressLine2) && request.AddressLine2.Length > 100)
                throw new FieldValidationException($"CreateUser - invalid AddressLine2 ({request.AddressLine2})");

            if (!string.IsNullOrEmpty(request.City) && request.City.Length > 50)
                throw new FieldValidationException($"CreateUser - invalid City ({request.City})");

            if (!string.IsNullOrEmpty(request.Region) && request.Region.Length > 50)
                throw new FieldValidationException($"CreateUser - invalid Region ({request.Region})");

            if (!string.IsNullOrEmpty(request.Country) && request.Country.Length > 50)
                throw new FieldValidationException($"CreateUser - invalid Country ({request.Country})");

            if (!string.IsNullOrEmpty(request.PostalCode) && request.PostalCode.Length > 50)
                throw new FieldValidationException($"CreateUser - invalid PostalCode ({request.PostalCode})");

            if (request.UserMappings != null && request.UserMappings.Count > 0)
            {
                ValidateUserMappings(request);
            }
        }

        private void ValidateUserMappings(CreateUserRequestV4Model request)
        {
            foreach (var um in request.UserMappings)
            {
                bool isDuplicated = request.UserMappings.Count(x => x.PlatformName == um.PlatformName
                && x.PlatformCustomer == um.PlatformCustomer && x.PlatformRole == um.PlatformRole) > 1;
                if (isDuplicated)
                    throw new FieldValidationException($"CreateUser - invalid UserMappings - Duplicated");
            }
        }

        private void SetPasswordSalt(UserModel user, string password)
        {
            user.StrongPasswordSet = DateTime.Now;
            user.StrongPasswordGoodUntil = new DateTime(2080, 1, 31);
            //Store strong password for next time
            byte[] passwordSalt = CreateSalt(16);
            byte[] passwordHash = GenerateSaltedHashSHA256(UnicodeEncoding.Unicode.GetBytes(password), passwordSalt, user.StrongPasswordSet);

            user.StrongPasswordHash = Convert.ToBase64String(passwordHash);
            user.StrongPasswordSalt = Convert.ToBase64String(passwordSalt);
        }

        private List<UserMapping> FindMatches(string username, string password, UserTypeEnum userType, ref List<UserMapping> badMatches, ref UserModel user, SiteTypeEnum site = SiteTypeEnum.Any)
        {
            badMatches = new List<UserMapping>();
            var result = new List<UserMapping>();
            bool anyType = userType == UserTypeEnum.Any;
            string siteName = Enum.GetName(typeof(SiteTypeEnum), site).ToLower();

            username = username.ToLower();

            //Only a single user for a username
            user = _userDbContext.Users.FirstOrDefault(u => u.Username == username);
            if (user == null)
            {
                return result;
            }
            int userID = user.UserID;

            var shouldSave = false;

            List<UserMapping> userMappings;
            if (site == SiteTypeEnum.Any && userType == UserTypeEnum.Any)
            {
                userMappings = _userDbContext.UserMappings.Where(u => u.UserId == userID).ToList();
            }
            else if (site == SiteTypeEnum.Any)
            {
                string platformName = "epic";
                string platformRole = "learner";
                switch (userType)
                {
                    case UserTypeEnum.LPIFacilitator:
                        platformName = "lpi";
                        platformRole = "facilitator";
                        break;
                    default:
                        break;
                }
                userMappings = _userDbContext.UserMappings.Where(
                                                                u => u.UserId == userID &&
                                                                u.PlatformName == platformName &&
                                                                u.PlatformRole == platformRole).ToList();
            }
            else
            {
                string platformName = "epic";
                string platformRole = "learner";
                if (site == SiteTypeEnum.LPI)
                {
                    platformName = "lpi";
                }
                else if (site == SiteTypeEnum.Catalyst)
                {
                    platformName = "catalyst";
                }

                switch (userType)
                {
                    case UserTypeEnum.LPIFacilitator:
                        platformName = "lpi";
                        platformRole = "facilitator";
                        break;
                    default:
                        break;
                }

                userMappings = _userDbContext.UserMappings.Where(
                                                                u => u.UserId == userID &&
                                                                u.PlatformName == platformName &&
                                                                u.PlatformRole == platformRole).ToList();
            }

            foreach (UserMapping match in userMappings)
            {
                if (site != SiteTypeEnum.Any && match.PlatformName != siteName)
                    continue;

                if (password == null)
                {
                    //Just finding User matches
                    result.Add(match);
                    continue;
                }

                bool passwordMatch = false;
                byte[] saltedHash = null;

                if (!string.IsNullOrEmpty(user.StrongPasswordSalt) && match.PlatformName != "lpi")
                {
                    //Using new strong password for epic and catalyst learners, so compare that
                    saltedHash = GenerateSaltedHashSHA256(UnicodeEncoding.Unicode.GetBytes(password), Convert.FromBase64String(user.StrongPasswordSalt), user.StrongPasswordSet);
                    passwordMatch = Convert.ToBase64String(saltedHash) == user.StrongPasswordHash;
                }
                else
                {
                    switch (match.PlatformPassowrdMethod)
                    {
                        case "SHA1":
                            if (match.PlatformName == "epic" || match.PlatformName == "catalyst")
                            {
                                saltedHash = GenerateEPICLearnerSaltedHashSHA1(match.PlatformPasswordSalt + password);
                                passwordMatch = String.Equals(Convert.ToBase64String(saltedHash), match.PlatformPasswordHash, StringComparison.OrdinalIgnoreCase);
                            }
                            break;
                        case "SHA256":
                            if (match.PlatformName == "pac")
                            {
                                if (match.PlatformRole == "admin")
                                {
                                    //Password was stored in .NET 1.1 era Forms Authentication database
                                    saltedHash = GenerateSaltedHashSHA1(UnicodeEncoding.Unicode.GetBytes(password), Convert.FromBase64String(match.PlatformPasswordSalt));
                                    passwordMatch = Convert.ToBase64String(saltedHash) == match.PlatformPasswordHash;
                                }
                                else
                                {
                                    saltedHash = GenerateSaltedHashSHA256(UnicodeEncoding.Unicode.GetBytes(password), Convert.FromBase64String(match.PlatformPasswordSalt));
                                    passwordMatch = Convert.ToBase64String(saltedHash) == match.PlatformPasswordHash;
                                }
                            }
                            else
                            {
                                saltedHash = GenerateSaltedHashSHA256(UnicodeEncoding.Unicode.GetBytes(password), Convert.FromBase64String(match.PlatformPasswordSalt));
                                passwordMatch = Convert.ToBase64String(saltedHash) == match.PlatformPasswordHash;
                            }
                            break;
                        default:
                            if (match.PlatformPasswordSalt != null)
                            {
                                saltedHash = GenerateSaltedHashSHA256(UnicodeEncoding.Unicode.GetBytes(password), Convert.FromBase64String(match.PlatformPasswordSalt));
                                passwordMatch = Convert.ToBase64String(saltedHash) == match.PlatformPasswordHash;
                            }
                            else
                            {
                                passwordMatch = false;
                            }
                            break;
                    }
                }

                if (passwordMatch && string.IsNullOrEmpty(user.StrongPasswordSalt))
                {
                    user.StrongPasswordSet = DateTime.Now;
                    user.StrongPasswordGoodUntil = new DateTime(2080, 1, 31);
                    //Store strong password for next time
                    byte[] passwordSalt = CreateSalt(16);
                    byte[] passwordHash = GenerateSaltedHashSHA256(UnicodeEncoding.Unicode.GetBytes(password), passwordSalt, user.StrongPasswordSet);

                    user.StrongPasswordHash = Convert.ToBase64String(passwordHash);
                    user.StrongPasswordSalt = Convert.ToBase64String(passwordSalt);

                    shouldSave = true;
                }

                if (passwordMatch)
                {
                    result.Add(match);
                }
                else
                {
                    badMatches.Add(match);
                }
            }

            if (shouldSave)
            {
                _userDbContext.SaveChanges();
            }

            return result;
        }

        private async Task<(List<UserModel>, List<UserModel>)> FindMatches(string userName, string password)
        {
            List<UserModel> badMatches = new List<UserModel>();
            var result = new List<UserModel>();
            List<UserModel> userMatches;

            userName = userName.ToLower();

            userMatches = await _userDbContext.Users.Where(u => u.Username == userName).ToListAsync();

            foreach (UserModel match in userMatches)
            {
                bool passwordMatch = false;
                byte[] saltedHash = null;

                //Using new strong password, so compare that
                saltedHash = GenerateSaltedHashSHA256(UnicodeEncoding.Unicode.GetBytes(password), Convert.FromBase64String(match.StrongPasswordSalt), match.StrongPasswordSet);
                passwordMatch = Convert.ToBase64String(saltedHash) == match.StrongPasswordHash;

                if (passwordMatch)
                {
                    result.Add(match);
                }
                else
                {
                    badMatches.Add(match);
                }
            }

            return (result, badMatches);
        }

        /// <summary>
        /// Create random salt
        /// </summary>
        /// <param name="size">length of salt array</param>
        /// <returns>salt array</returns>
        private static byte[] CreateSalt(int size)
        {
            //Generate a cryptographic random number.
            RNGCryptoServiceProvider rng = new RNGCryptoServiceProvider();
            byte[] buff = new byte[size];
            rng.GetBytes(buff);

            return buff;
        }

        /// <summary>
        /// Generate a salted hash using SHA256
        /// </summary>
        /// <param name="plainText">password plaintext as array</param>
        /// <param name="salt">salt array</param>
        /// <returns>hash array</returns>
        private static byte[] GenerateSaltedHashSHA256(byte[] plainText, byte[] salt, DateTime? passwordDate = null)
        {
            HashAlgorithm algorithm = new SHA256Managed();

            byte[] plainTextWithSaltBytes =
                new byte[plainText.Length + salt.Length];

            for (int i = 0; i < plainText.Length; i++)
            {
                plainTextWithSaltBytes[i] = plainText[i];
            }

            for (int i = 0; i < salt.Length; i++)
            {
                plainTextWithSaltBytes[plainText.Length + i] = salt[i];
            }

            if (passwordDate != null)
            {
                TimeSpan span = passwordDate.Value.Subtract(new DateTime(2006, 5, 27));
                int count = (span.Days / 30) + (span.Days % 30) * 40 + 1;
                while (count > 0)
                {
                    plainTextWithSaltBytes = algorithm.ComputeHash(plainTextWithSaltBytes);
                    count--;
                }

                return plainTextWithSaltBytes;
            }
            else
            {
                return algorithm.ComputeHash(plainTextWithSaltBytes);
            }
        }

        /// <summary>
        /// Generate a salted hash using SHA1
        /// </summary>
        /// <param name="plainText">password plaintext as array</param>
        /// <param name="salt">salt array</param>
        /// <returns>hash array</returns>
        private static byte[] GenerateSaltedHashSHA1(byte[] plainText, byte[] salt)
        {
            HashAlgorithm algorithm = new SHA1Managed();

            byte[] plainTextWithSaltBytes =
                new byte[plainText.Length + salt.Length];

            for (int i = 0; i < plainText.Length; i++)
            {
                plainTextWithSaltBytes[i] = plainText[i];
            }

            for (int i = 0; i < salt.Length; i++)
            {
                plainTextWithSaltBytes[plainText.Length + i] = salt[i];
            }

            return algorithm.ComputeHash(plainTextWithSaltBytes);
        }

        /// <summary>
        /// Generate a salted hash for EPIC Learners using SHA1
        /// </summary>
        /// <param name="saltedPasword">Salt + password</param>
        /// <returns>hash array</returns>
        private static byte[] GenerateEPICLearnerSaltedHashSHA1(string saltedPasword)
        {
            HashAlgorithm algorithm = new SHA1Managed();
            byte[] arrData = algorithm.ComputeHash(Encoding.UTF8.GetBytes(saltedPasword));
            StringBuilder sBuilder = new StringBuilder();
            for (int i = 0; i < arrData.Length; i++)
            {
                sBuilder.Append(arrData[i].ToString("x2"));
            }

            return Convert.FromBase64String(sBuilder.ToString());
        }

        /// <summary>
        /// Compare hash arrays
        /// </summary>
        /// <param name="array1">hash array</param>
        /// <param name="array2">hash array</param>
        /// <returns>boolean</returns>
        private static bool CompareHash(byte[] array1, byte[] array2)
        {
            if (array1.Length != array2.Length)
            {
                return false;
            }

            for (int i = 0; i < array1.Length; i++)
            {
                if (array1[i] != array2[i])
                {
                    return false;
                }
            }

            return true;
        }

        private async Task LogLoginAttempt(UserModel user, UserMapping userMapping, bool success)
        {
            try
            {
                var now = DateTime.Now;
                var iD = Guid.NewGuid();
                LoginAttempt request = new LoginAttempt { LoginAttemptID = iD, UserID = user.UserID, Success = success, Attempted = now };
                _userDbContext.LoginAttempts.Add(request);
                _userDbContext.SaveChanges();

                if (success)
                {
                    var result = await _kafkaService.SendKafkaLoginMessage(user, userMapping, now);
                    var failures = result.Where(x => x.Status != ReturnStatus.Success);
                    if (failures.Any())
                        _logger.LogError("LogLoginAttempt - SendKafkaLoginMessage errors occurred\n{0}", string.Join("\n", failures.Select(x => x.Exception)));
                    else
                        _logger.LogInformation("LogLoginAttempt - SendKafkaLoginMessage successful");
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, $"LogLoginAttempt - Error - {user.UserID}, {userMapping?.PlatformName}, {userMapping?.PlatformRole}", user.UniqueID, userMapping.PlatformName, userMapping.PlatformRole);
            }
        }

        #endregion Private Helpers
    }
}

