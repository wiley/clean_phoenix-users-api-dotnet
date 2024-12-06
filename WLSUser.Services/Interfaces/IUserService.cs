using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using WLSUser.Domain.Models;
using WLSUser.Domain.Models.Authentication;
using WLSUser.Domain.Models.V4;
using WLSUser.Infrastructure.Contexts;

namespace WLSUser.Services.Interfaces
{
    public interface IUserService
    {
        LoginResponseModel Login(LoginRequestModel loginRequest, bool passwordRequired = true);

        LoginResponse Login(string userName, string password, SiteTypeEnum siteType, UserTypeEnum userType, bool passwordRequired = true);

        LoginResponse LoginV2Refresh(string userName, string uniqueID);

        int CreateUser(string username);
        UserResponseModel CreateUserV4(CreateUserRequestV4Model model);

        IEnumerable<SearchResponseModel> Search(SearchRequestModel request);

        IEnumerable<UniqueIDSearchResponseModel> SearchByUniqueID(UniqueIDSearchRequestModel request);

        List<UserResponseModel> SearchUsersV4(SearchRequestV4Model request, string include, bool strict = true);

        ForgotPasswordResponseModel ForgotPassword(string apiToken, ForgotPasswordRequestModel forgotPassword);

        UpdateUserResponseModel UpdateUser(UpdateUserRequestModel updateUserRequest);
        UserResponseModel UpdateUserV4(int userID, UpdateUserRequestV4Model request);
        UserModel GetUserFromUsername(string Username);
        UserResponseModel GetUser(int userId);

        void DeleteUser(string uniqueID);

        void DeleteUser(int userId);

        UserRole AddUserRole(UserRoleRequest request);


        bool DeleteUserRole(UserRoleRequest request);

        List<RoleType> GetUserRoles(int userID);

        AccessType GetAccessTypeByName(string accessTypeName);

        List<UserRoleAccess> AddUserRoleAccess(UserRoleAccessRequest request);

        bool DeleteUserRoleAccess(UserRoleAccessRequest request);

        List<UserRoleAccess> GetUserRoleAccess(int userID);

        List<UserRoleResponse> GetUserRoleResponses(int userID);

        DateTime GetLastLoginDate(string uniqueID, int offset = 0);

        List<RoleAccessReference> GetRoleAccessReferences(int userID, bool forceUpdate = true, int secondsToExpire = 12 * 60 * 60);

        int LoginCount(string uniqueId);

        UserModel GetUserFromUniqueID(string uniqueID);

        Task<UserModel> Login(string userName, string password);

        UserModel GetUserModel(int userId);

        UserModel GetUserModelByUsername(string username);

        Task RecoverPassword(RecoverPasswordRequestV4 request);
        bool ValidateFunctionCode(string code);

        bool ChangePassword(UserChangePasswordRequest changePassword, int userId = 0);
        ValidateCodeResponse FunctionCode(string code);
        Task GenerateKafkaEvents(UserDbContext userDbContext);
    }
}