using Microsoft.AspNetCore.Authorization;

using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Net;
using System.Threading.Tasks;

using WLS.Log.LoggerTransactionPattern;

using WLSUser.Domain.Exceptions;
using WLSUser.Domain.Models;
using WLSUser.Domain.Models.Authentication;
using WLSUser.Domain.Models.V4;
using WLSUser.Services;
using WLSUser.Services.Interfaces;
using DarwinAuthorization.Models;
using WLSUser.Responses.Exceptions.NonSuccessfullResponses;
using System.Linq;
using WLSUser.Infrastructure.Contexts;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace WLSUser.Controllers
{
    [Route("api/v{version:apiVersion}/Users")]
    [Consumes("application/json")]
    [Produces("application/json")]
    [ApiVersion("1.0")]
    [ApiController]
    public class UsersController : ControllerBase
    {
        private readonly IUserService _userService = null;
        private readonly IUserMappingService _userMappingService = null;
        private readonly IUserConsentService _userConsentService = null;
        private readonly IFederationService _federationService;
        private readonly ISsoService _ssoService;
        private readonly ILogger<UsersController> _logger;
        private readonly ILoggerStateFactory _loggerStateFactory;
        private readonly IKeyCloakService _keycloakService;
        private readonly DarwinAuthorizationContext _darwinAuthorizationContext;

        private readonly IConfiguration _configuration;

        public UsersController(IUserService userService, IUserMappingService userMappingService, IUserConsentService userConsentService, IFederationService federationService,
                ISsoService ssoService, ILogger<UsersController> logger, ILoggerStateFactory loggerStateFactory, IKeyCloakService keycloakService, DarwinAuthorizationContext darwinAuthorizationContext, IConfiguration configuration)
        {
            _userService = userService;
            _userMappingService = userMappingService;
            _userConsentService = userConsentService;
            _federationService = federationService;
            _ssoService = ssoService;
            _logger = logger;
            _loggerStateFactory = loggerStateFactory;
            _keycloakService = keycloakService;
            _darwinAuthorizationContext = darwinAuthorizationContext;
            _configuration = configuration;
        }

        #region V4 End-points
        [HttpPut("generate-kafka-events")]
        [ProducesResponseType(202)]
        [ApiVersion("4.0")]
        [Authorize]
        public IActionResult GenerateKafkaEvents()
        {
            using (_logger.BeginScope(_loggerStateFactory.Create(Request.Headers["Transaction-ID"])))
            {
                try
                {
                    var connectionString = Environment.GetEnvironmentVariable("USERSAPI_CONNECTION_STRING") ?? _configuration.GetConnectionString("UserDbContext");
                    var optionsBuilder = new DbContextOptionsBuilder<UserDbContext>();
                    optionsBuilder.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString));
                    _ = _userService.GenerateKafkaEvents(new UserDbContext(optionsBuilder.Options));

                    return Accepted();
                }
                catch (SystemException exception)
                {
                    _logger.LogError(exception, $"GenerateKafkaEvents - Unhandled Exception");
                    return StatusCode(500);
                }
            }
        }

        [HttpGet("{userID}")]
        [ProducesResponseType(typeof(UserResponseModel), 200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(401)]
        [ProducesResponseType(404)]
        [ProducesResponseType(typeof(ForbiddenResponseV4), 403)]
        [ApiVersion("4.0")]
        [Authorize]
        public IActionResult GetUser(int userID)
        {
            using (_logger.BeginScope(_loggerStateFactory.Create(Request.Headers["Transaction-ID"])))
            {
                try
                {
                    if (userID == 0)
                    {
                        return BadRequest();
                    }

                    UserResponseModel user = _userService.GetUser(userID);

                    return Ok(user);
                }
                catch (NotFoundException)
                {
                    _logger.LogWarning("GetUser - NotFound - {userID}", userID);
                    return NotFound();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "GetUser - Failed - {userID}", userID);

                    return StatusCode(500);
                }
            }
        }

        [HttpPut("{userID}")]
        [ProducesResponseType(typeof(UserResponseModel), 200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(401)]
        [ProducesResponseType(404)]
        [ApiVersion("4.0")]
        [Authorize]
        public IActionResult UpdateUser(int userID, [FromBody] UpdateUserRequestV4Model updateUserRequestModel)
        {
            using (_logger.BeginScope(_loggerStateFactory.Create(Request.Headers["Transaction-ID"])))
            {
                try
                {
                    if (userID == 0)
                    {
                        _logger.LogWarning("UpdateUser - Bad Request UserId");
                        return BadRequest();
                    }
                    else if (!ModelState.IsValid)
                    {
                        _logger.LogWarning("UpdateUser - Invalid Model State");
                        return BadRequest(ModelState);
                    }
                    else if (updateUserRequestModel == null)
                    {
                        _logger.LogWarning("UpdateUser - Model is null");
                        return BadRequest();
                    }

                    UserResponseModel user = _userService.UpdateUserV4(userID, updateUserRequestModel);

                    return Ok(user);
                }
                catch (NotFoundException)
                {
                    _logger.LogWarning("UpdateUser - NotFound - {userID}", userID);
                    return NotFound();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "UpdateUser - Failed - {userID}", userID);
                    return StatusCode(500);
                }
            }
        }

        [HttpPost]
        [ProducesResponseType(typeof(UserResponseModel), 201)]
        [ProducesResponseType(400)]
        [ProducesResponseType(401)]
        [ProducesResponseType(404)]
        [ApiVersion("4.0")]
        [Authorize]
        public IActionResult CreateUser([FromBody] CreateUserRequestV4Model createUserRequestModel)
        {
            using (_logger.BeginScope(_loggerStateFactory.Create(Request.Headers["Transaction-ID"])))
            {
                try
                {
                    if (!ModelState.IsValid)
                    {
                        _logger.LogWarning("CreateUser - Invalid Model State");
                        return BadRequest(ModelState);
                    }
                    else if (createUserRequestModel == null)
                    {
                        _logger.LogWarning("CreateUser - Model is null");
                        return BadRequest();
                    }

                    UserResponseModel user = _userService.CreateUserV4(createUserRequestModel);

                    string detailUrlFormat = string.Concat(
                        HttpContext.Request.Scheme,
                        "://",
                        HttpContext.Request.Host.ToUriComponent(),
                        HttpContext.Request.Path,
                        $"/{user.UserId}");

                    return Created(detailUrlFormat, user);
                }
                catch (FieldValidationException ex)
                {
                    _logger.LogWarning(ex, "CreateUser - BadRequest - {username}", createUserRequestModel.Username);
                    return BadRequest();
                }
                catch (ArgumentException ex)
                {
                    _logger.LogError(ex, "CreateUser - User already exists - {Username}", createUserRequestModel.Username);
                    return Conflict();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "CreateUser - Failed - {Username}", createUserRequestModel.Username);
                    return StatusCode(500);
                }
            }
        }

        [HttpDelete("{userID}")]
        [ProducesResponseType(204)]
        [ProducesResponseType(400)]
        [ProducesResponseType(401)]
        [ProducesResponseType(404)]
        [ApiVersion("4.0")]
        [Authorize]
        public IActionResult DeleteUser(int userID)
        {
            using (_logger.BeginScope(_loggerStateFactory.Create(Request.Headers["Transaction-ID"])))
            {
                try
                {
                    if (userID == 0)
                    {
                        _logger.LogWarning("DeleteUser - Bad Request UserId");
                        return BadRequest();
                    }
                    else if (!ModelState.IsValid)
                    {
                        _logger.LogWarning("DeleteUser - Invalid Model State");
                        return BadRequest(ModelState);
                    }

                    _userService.DeleteUser(userID);
                    return NoContent();
                }
                catch (InvalidOperationException)
                {
                    _logger.LogWarning("DeleteUser - User Has active Mappings - {userID}", userID);
                    return Conflict();
                }
                catch (NotFoundException)
                {
                    _logger.LogWarning("DeleteUser - NotFound - {userID}", userID);
                    return NotFound();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "DeleteUser - Failed - {userID}", userID);
                    return StatusCode(500);
                }
            }
        }

        [HttpPost("search")]
        [ProducesResponseType(typeof(List<UserResponseModel>), 200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(401)]
        [ProducesResponseType(404)]
        [ApiVersion("4.0")]
        [Authorize]
        public IActionResult SearchUsersV4([FromBody] SearchRequestV4Model request, [FromQuery] string include, [FromQuery] bool strict = true)
        {
            using (_logger.BeginScope(_loggerStateFactory.Create(Request.Headers["Transaction-ID"])))
            {
                try
                {
                    if (request == null)
                    {
                        _logger.LogWarning("SearchUsersV4 - Bad Request UserId");
                        return BadRequest();
                    }
                    else if (!ModelState.IsValid)
                    {
                        _logger.LogWarning("SearchUsersV4 - Invalid Model State");
                        return BadRequest(ModelState);
                    }

                    SearchResponseV4Model response = new SearchResponseV4Model();
                    response.Items = _userService.SearchUsersV4(request, include, strict);

                    return Ok(response);
                }
                catch (NotFoundException)
                {
                    return NotFound();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "SearchUsersV4 - Failed - {Username}", request.Username);
                    return StatusCode(500);
                }
            }
        }

        #region UserMappings

        [HttpGet("{userID}/user-mappings/")]
        [ProducesResponseType(typeof(UserMappingsResponse), 200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(401)]
        [ProducesResponseType(404)]
        [ApiVersion("4.0")]
        [Authorize]
        public IActionResult GetUserMappings([FromRoute] int userID, [FromQuery] string platformName, [FromQuery] string platformCustomer, [FromQuery] string platformRole)
        {
            using (_logger.BeginScope(_loggerStateFactory.Create(Request.Headers["Transaction-ID"])))
            {
                try
                {
                    if (userID == 0)
                    {
                        _logger.LogWarning("GetUserMappings - Bad Request userMappingId");
                        return BadRequest();
                    }
                    else if (!ModelState.IsValid)
                    {
                        _logger.LogWarning("GetUserMappings - Invalid Model State");
                        return BadRequest(ModelState);
                    }

                    UserMappingsResponse mapping = _userMappingService.GetUserMappingsByUserId(userID, platformName, platformCustomer, platformRole);
                    return Ok(mapping);
                }
                catch (NotFoundException)
                {
                    _logger.LogWarning("GetUserMappings - NotFound - {userID}", userID);
                    return NotFound();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "GetUserMappings - Failed - {userID}", userID);
                    return BadRequest();
                }
            }
        }

        [HttpGet("{userID}/user-mappings/{userMappingId}")]
        [ProducesResponseType(typeof(UserMappingResponse), 200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(401)]
        [ProducesResponseType(404)]
        [ApiVersion("4.0")]
        [Authorize]
        public IActionResult GetUserMapping(int userID, int userMappingId)
        {
            using (_logger.BeginScope(_loggerStateFactory.Create(Request.Headers["Transaction-ID"])))
            {
                try
                {
                    if (userMappingId == 0)
                    {
                        _logger.LogWarning("GetUserMapping - Bad Request userMappingId");
                        return BadRequest();
                    }
                    UserMappingResponse mapping = _userMappingService.GetUserMapping(userID, userMappingId);
                    return Ok(mapping);
                }
                catch (NotFoundException)
                {
                    _logger.LogWarning("GetUserMapping - NotFound - {userID}, {userMappingId}", userID, userMappingId);
                    return NotFound();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "GetUserMapping - Failed - {userID}, {userMappingId}", userID, userMappingId);
                    return BadRequest();
                }
            }
        }

        [HttpPost("{userID}/user-mappings")]
        [ProducesResponseType(typeof(UserMappingResponse), 200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(401)]
        [ProducesResponseType(404)]
        [ApiVersion("4.0")]
        [Authorize]
        public IActionResult CreateUserMapping(int userID, [FromBody] CreateUserMappingRequest userMapping)
        {
            using (_logger.BeginScope(_loggerStateFactory.Create(Request.Headers["Transaction-ID"])))
            {
                try
                {
                    if (userMapping == null)
                    {
                        _logger.LogWarning("CreateUserMapping - Bad Request userMapping");
                        return BadRequest();
                    }
                    else if (!ModelState.IsValid)
                    {
                        _logger.LogWarning("CreateUserMapping - Invalid Model State");
                        return BadRequest(ModelState);
                    }

                    UserMappingResponse mapping = _userMappingService.CreateUserMapping(userID, userMapping);
                    return Ok(mapping);
                }
                catch (NotFoundException)
                {
                    return NotFound();
                }
                catch (ArgumentException)
                {
                    return Conflict();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "CreateUserMapping - Failed - {userID}, {PlatformName}", userID, userMapping.PlatformName);
                    return StatusCode(500);
                }
            }
        }

        [HttpPut("{userID}/user-mappings/{userMappingId}")]
        [ProducesResponseType(typeof(UserMappingResponse), 200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(401)]
        [ProducesResponseType(404)]
        [ApiVersion("4.0")]
        [Authorize]
        public IActionResult UpdateUserMapping(int userID, int userMappingId, [FromBody] UpdateUserMappingRequest userMapping)
        {
            using (_logger.BeginScope(_loggerStateFactory.Create(Request.Headers["Transaction-ID"])))
            {
                try
                {
                    UserMappingResponse mapping = _userMappingService.UpdateUserMapping(userID, userMappingId, userMapping);

                    return Ok(mapping);
                }
                catch (NotFoundException)
                {
                    _logger.LogWarning("UpdateUserMapping - NotFound - {userID}, {userMappingId}", userID, userMappingId);
                    return NotFound();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "UpdateUserMapping - Failed - {userID}, {userMappingId}", userID, userMappingId);
                    return BadRequest();
                }
            }
        }

        [HttpDelete("{userID}/user-mappings/{userMappingId}")]
        [ProducesResponseType(204)]
        [ProducesResponseType(400)]
        [ProducesResponseType(401)]
        [ProducesResponseType(404)]
        [ApiVersion("4.0")]
        [Authorize]
        public IActionResult DeleteUserMapping(int userID, int userMappingId)
        {
            using (_logger.BeginScope(_loggerStateFactory.Create(Request.Headers["Transaction-ID"])))
            {
                try
                {
                    if (userID == 0)
                    {
                        _logger.LogWarning("DeleteUserMapping - Bad Request UserId");
                        return BadRequest();
                    }
                    else if (!ModelState.IsValid)
                    {
                        _logger.LogWarning("DeleteUserMapping - Invalid Model State");
                        return BadRequest(ModelState);
                    }

                    _userMappingService.DeleteUserMapping(userID, userMappingId);
                    return NoContent();
                }
                catch (NotFoundException)
                {
                    _logger.LogWarning("DeleteUserMapping - NotFound - {userID}, {userMappingId}", userID, userMappingId);
                    return NotFound();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "DeleteUserMapping - Failed - {userID}, {userMappingId}", userID, userMappingId);
                    return BadRequest();
                }
            }
        }
        #endregion //UserMappings

        [HttpPost("login")]
        [ApiVersion("4.0")]
        [ProducesResponseType(typeof(AuthResponseV4), 200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(typeof(UnauthorizedResponseV4), 401)]
        [ProducesResponseType(500)]
        public async Task<IActionResult> Login([Required][FromBody] LoginRequestV4 request)
        {
            using (_logger.BeginScope(_loggerStateFactory.Create(Request.Headers["Transaction-ID"])))
            {
                try
                {
                    _logger.LogInformation("Login - {Username}", request.Username);

                    AuthResponseV4 result;
                    try
                    {
                        UserModel user = await _userService.Login(request.Username, request.Password);
                        result = await _keycloakService.GetPasswordTokens(user);
                    }
                    catch (PasswordExpiredException)
                    {
                        _logger.LogWarning("Login - BadRequest - {username}", request.Username);

                        return Unauthorized(new UnauthorizedResponseV4 { Message = "Your password has expired. Please choose a new password." });
                    }
                    catch (AuthenticationFailedException)
                    {
                        _logger.LogWarning("Login - InvalidCredentials - {Username}", request.Username);
                        return Unauthorized(new UnauthorizedResponseV4 { Message = "No valid credentials were provided." });
                    }

                    return Ok(result);
                }

                catch (Exception ex)
                {
                    _logger.LogError(ex, "Login - Failed - {Username}", request.Username);
                    return StatusCode((int)HttpStatusCode.InternalServerError);
                }
            }
        }

        [HttpPost("login-apitoken")]
        [ApiVersion("4.0")]
        [ProducesResponseType(typeof(AuthResponseV4), 200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(401)]
        [ProducesResponseType(500)]
        [Authorize]
        public async Task<IActionResult> LoginWithToken([Required][FromBody] LoginAPITokenRequestV4 request)
        {
            using (_logger.BeginScope(_loggerStateFactory.Create(Request.Headers["Transaction-ID"])))
            {
                try
                {
                    _logger.LogInformation("LoginWithToken - {Username}", request.Username);

                    UserModel user = _userService.GetUserFromUsername(request.Username);
                    if (user == null)
                    {
                        _logger.LogWarning("LoginWithToken - NotFound - {Username}", request.Username);
                        return NotFound();
                    }

                    AuthResponseV4 result = await _keycloakService.GetPasswordTokens(user);

                    return Ok(result);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "LoginWithToken - Failed - {Username}", request.Username);
                    return StatusCode((int)HttpStatusCode.InternalServerError);
                }
            }
        }

        [HttpPost("logout")]
        [ApiVersion("4.0")]
        [ProducesResponseType(204)]
        [ProducesResponseType(401)]
        [ProducesResponseType(500)]
        [Authorize]
        public async Task<IActionResult> Logout()
        {
            using (_logger.BeginScope(_loggerStateFactory.Create(Request.Headers["Transaction-ID"])))
            {
                string keycloakUserId = "";
                try
                {
                    keycloakUserId = User.FindFirst("sub").Value;
                    _logger.LogInformation("Logout - {keycloakUserId}", keycloakUserId);

                    await _keycloakService.Logout(keycloakUserId);

                    return NoContent();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Logout - Failed - {keycloakUserId}", keycloakUserId);
                    return StatusCode((int)HttpStatusCode.InternalServerError);
                }
            }
        }

        [HttpPost("recover-password")]
        [ApiVersion("4.0")]
        [ProducesResponseType(204)]
        [ProducesResponseType(400)]
        [ProducesResponseType(404)]
        [ProducesResponseType(500)]
        public async Task<IActionResult> RecoverPassword([Required][FromBody] RecoverPasswordRequestV4 request)
        {
            using (_logger.BeginScope(_loggerStateFactory.Create(Request.Headers["Transaction-ID"])))
            {
                _logger.LogInformation("RecoverPassword - {siteType}, {username}", request.SiteType, request.Username);

                try
                {
                    if (request == null || string.IsNullOrEmpty(request.Username))
                    {
                        _logger.LogWarning("RecoverPassword - BadRequest - {siteType}, {username}", request.SiteType, request.Username);
                        return BadRequest();
                    }

                    await _userService.RecoverPassword(request);

                    return NoContent();
                }
                catch (NotFoundException)
                {
                    _logger.LogWarning("RecoverPassword - NotFound - {siteType}, {username}", request.SiteType, request.Username);
                    return NotFound();
                }
                catch (EmailCallException)
                {
                    _logger.LogWarning("RecoverPassword - EmailError - {siteType}, {username}", request.SiteType, request.Username);
                    return StatusCode((int)HttpStatusCode.InternalServerError);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "RecoverPassword - Failed - {siteType}, {username}", request.SiteType, request.Username);
                    return StatusCode((int)HttpStatusCode.InternalServerError);
                }
            }
        }

        [HttpHead("validate-function-code/{code}")]
        [ApiVersion("4.0")]
        [ProducesResponseType(204)]
        [ProducesResponseType(400)]
        [ProducesResponseType(403)]
        [ProducesResponseType(500)]
        public IActionResult ValidateFunctionCode(string code)
        {
            using (_logger.BeginScope(_loggerStateFactory.Create(Request.Headers["Transaction-ID"])))
            {
                _logger.LogInformation("ValidateFunctionCode - {code}", code);

                try
                {
                    var result = _userService.ValidateFunctionCode(code);
                    if (!result)
                    {
                        return Forbid();
                    }

                    return NoContent();
                }

                catch (Exception ex)
                {
                    _logger.LogError(ex, "ValidateFunctionCode - Failed - {code}", code);
                    return StatusCode((int)HttpStatusCode.InternalServerError);
                }
            }
        }

        [HttpPost("change-password")]
        [ApiVersion("4.0")]
        [ProducesResponseType(204)]
        [ProducesResponseType(400)]
        [ProducesResponseType(typeof(UnauthorizedResponseV4), 401)]
        [ProducesResponseType(500)]
        public IActionResult ChangePassword([Required][FromBody] UserChangePasswordRequest changePassword)
        {
            using (_logger.BeginScope(_loggerStateFactory.Create(Request.Headers["Transaction-ID"])))
            {
                try
                {
                    if (changePassword is null) return BadRequest("Request invalid, request body can't be null or empty.");

                    if (string.IsNullOrEmpty(changePassword.Code))
                    {
                        _logger.LogWarning("ChangePassword - BadRequest - nullempty");
                        return Unauthorized(new UnauthorizedResponseV4 { Message = "No valid credentials were provided." });
                    }

                    _logger.LogInformation("ChangePassword - {code}", changePassword.Code);

                    if (!_userService.ChangePassword(changePassword))
                    {
                        _logger.LogWarning("ChangePassword - Invalidfunctioncode - {code}", changePassword.Code);
                        return Unauthorized(new UnauthorizedResponseV4 { Message = "No valid credentials were provided." });
                    }

                    return NoContent();
                }

                catch (Exception ex)
                {
                    string code = changePassword?.Code ?? string.Empty;
                    _logger.LogError(ex, "ChangePassword - Failed - {code}", code);
                    return StatusCode((int)HttpStatusCode.InternalServerError);
                }
            }
        }

        [HttpPost("set-password")]
        [ApiVersion("4.0")]
        [ProducesResponseType(204)]
        [ProducesResponseType(400)]
        [ProducesResponseType(typeof(UnauthorizedResponseV4), 401)]
        [ProducesResponseType(403)]
        [ProducesResponseType(404)]
        [ProducesResponseType(500)]
        [Authorize]
        public IActionResult SetPassword([Required][FromBody] UserChangePasswordOnlyRequest changePassword)
        {
            using (_logger.BeginScope(_loggerStateFactory.Create(Request.Headers["Transaction-ID"])))
            {
                int userId = _darwinAuthorizationContext.UserId;
                try
                {
                    if (changePassword is null)
                        return BadRequest("Request invalid, request body can't be null or empty.");
                    if (userId <= 0)
                        ModelState.AddModelError("UserId", "UserId required");
                    if (!ModelState.IsValid)
                    {
                        _logger.LogWarning("SetPassword - BadRequest - {userId}", userId);
                        return BadRequest(ModelState);
                    }

                    _userService.ChangePassword(new UserChangePasswordRequest()
                    {
                        NewPassword = changePassword.Password
                    }, userId);
                    return NoContent();
                }
                catch (NotFoundException)
                {
                    _logger.LogWarning("SetPassword - NotFound - {userId}", userId);
                    return NotFound();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "SetPassword - Failed - {userId}", userId);
                    return StatusCode((int)HttpStatusCode.InternalServerError);
                }
            }
        }

        [HttpGet("function-code/{code}")]
        [ApiVersion("4.0")]
        [ProducesResponseType(typeof(ValidateCodeResponse), 200)]
        [ProducesResponseType(typeof(BadRequestMessage), 400)]
        [ProducesResponseType(typeof(UnauthorizedResponseV4), 401)]
        [ProducesResponseType(500)]
        [Authorize]
        public IActionResult FunctionCode(string code)
        {
            using (_logger.BeginScope(_loggerStateFactory.Create(Request.Headers["Transaction-ID"])))
            {
                if (!_darwinAuthorizationContext.HasApiKey)
                {
                    _logger.LogWarning("Users - Function Code - Unauthorized {API_Key}", _darwinAuthorizationContext.HasApiKey);
                    return Unauthorized();
                }
                try
                {
                    var result = _userService.FunctionCode(code);
                    return Ok(result);
                }
                catch (BadRequestException ex)
                {
                    ModelState.AddModelError("Code", ex.Message);
                    _logger.LogWarning(ex, "Users - Function Code BadRequest - {code}.", code);
                    return BadRequest(NonSuccessfullRequestMessageFormatter.FormatBadRequestResponse(ModelState));
                }
                catch (NotFoundException ex)
                {
                    _logger.LogWarning(ex, "Users - Function Code Not Found - {code}.", code);
                    return NotFound(NonSuccessfullRequestMessageFormatter.FormatResourceNotFoundResponse());
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Users - Function Code Failed - {code}.", code);
                    return StatusCode((int)HttpStatusCode.InternalServerError);
                }
            }
        }

        #region User Consents

        [HttpPost("{userID}/user-consents")]
        [ProducesResponseType(typeof(UserConsent), 201)]
        [ProducesResponseType(400)]
        [ProducesResponseType(401)]
        [ProducesResponseType(403)]
        [ProducesResponseType(404)]
        [ProducesResponseType(409)]
        [ApiVersion("4.0")]
        [Authorize]
        public async Task<IActionResult> CreateUserConsentAsync([FromRoute] int userID, [FromBody, Required] CreateUserConsentRequest request)
        {
            using (_logger.BeginScope(_loggerStateFactory.Create(Request.Headers["Transaction-ID"])))
            {
                try
                {
                    if (userID == 0)
                    {
                        _logger.LogWarning("CreateUserConsent - Bad Request userID");
                        return BadRequest();
                    }
                    if (request == null)
                    {
                        _logger.LogWarning("CreateUserConsent - Bad Request CreateUserConsentRequest");
                        return BadRequest();
                    }

                    UserResponseModel user = _userService.GetUser(userID);
                    if (user == null)
                    {
                        _logger.LogWarning("CreateUserConsent - User NotFound - {userID}", userID);
                        return NotFound();
                    }

                    if (!ModelState.IsValid)
                    {
                        _logger.LogWarning("CreateUserConsent - Invalid Model State");
                        return BadRequest(ModelState);
                    }

                    UserConsent result = await _userConsentService.CreateUserConsent(userID, request);
                    string detailUrlFormat = string.Concat(
                        HttpContext.Request.Scheme,
                        "://",
                        HttpContext.Request.Host.ToUriComponent(),
                        HttpContext.Request.Path,
                        $"/{result.Id}");

                    return Created(detailUrlFormat, result);
                }
                catch (ConflictException)
                {
                    _logger.LogWarning("CreateUserConsent - Conflict - {userID}", userID);
                    return Conflict();
                }
                catch (NotFoundException)
                {
                    _logger.LogWarning("CreateUserConsent - NotFound - {userID}", userID);
                    return NotFound();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "CreateUserConsent - Failed - {userID}", userID);
                    return StatusCode(500);
                }
            }
        }

        [HttpGet("{userID}/user-consents")]
        [ProducesResponseType(typeof(IEnumerable<UserConsent>), 200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(401)]
        [ProducesResponseType(403)]
        [ApiVersion("4.0")]
        [Authorize]
        public async Task<IActionResult> GetUserConsentsAsync([FromRoute] int userID, [FromQuery] string PolicyType = "",
            [FromQuery] bool LatestVersion = false)
        {
            using (_logger.BeginScope(_loggerStateFactory.Create(Request.Headers["Transaction-ID"])))
            {
                try
                {
                    if (userID == 0)
                    {
                        _logger.LogWarning("GetUserConsents - Bad Request userID");
                        return BadRequest();
                    }
                    UserResponseModel user = _userService.GetUser(userID);
                    if (user == null)
                    {
                        _logger.LogWarning("GetUserConsents - User NotFound - {userID}", userID);
                        return NotFound();
                    }

                    if (!ModelState.IsValid)
                    {
                        _logger.LogWarning("GetUserConsents - Invalid Model State");
                        return BadRequest(ModelState);
                    }

                    IEnumerable<UserConsent> results = await _userConsentService.SearchUserConsents(userID, PolicyType, LatestVersion);

                    return Ok(new GetUserConsentsResponse
                    {
                        Count = results.Count(),
                        Items = results
                    });
                }
                catch (NotFoundException)
                {
                    _logger.LogWarning("GetUserConsents - NotFound - {userID}", userID);
                    return NotFound();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "GetUserConsents - Failed - {userID}", userID);
                    return StatusCode(500);
                }
            }
        }

        [HttpGet("{userID}/user-consents/{userConsentId}")]
        [ProducesResponseType(typeof(UserConsent), 200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(401)]
        [ProducesResponseType(403)]
        [ProducesResponseType(404)]
        [ApiVersion("4.0")]
        [Authorize]
        public async Task<IActionResult> GetUserConsentAsync([FromRoute] int userID, [FromRoute] int userConsentId)
        {
            using (_logger.BeginScope(_loggerStateFactory.Create(Request.Headers["Transaction-ID"])))
            {
                try
                {
                    if (userID == 0)
                    {
                        _logger.LogWarning("GetUserConsent - Bad Request userID");
                        return BadRequest();
                    }
                    UserResponseModel user = _userService.GetUser(userID);
                    if (user == null)
                    {
                        _logger.LogWarning("GetUserConsent - User NotFound - {userID}", userID);
                        return NotFound();
                    }

                    if (userConsentId == 0)
                    {
                        _logger.LogWarning("GetUserConsent - Bad Request userConsentId");
                        return BadRequest();
                    }

                    UserConsent result = await _userConsentService.GetUserConsentById(userID, userConsentId);

                    if (result == null)
                        return NotFound();

                    return Ok(result);
                }
                catch (NotFoundException)
                {
                    _logger.LogWarning("GetUserConsent - NotFound - {userID}", userID);
                    return NotFound();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "GetUserConsent - Failed - {userID}", userID);
                    return StatusCode(500);
                }
            }
        }

        [HttpDelete("{userID}/user-consents/{userConsentId}")]
        [ProducesResponseType(204)]
        [ProducesResponseType(400)]
        [ProducesResponseType(401)]
        [ProducesResponseType(403)]
        [ProducesResponseType(404)]
        [ApiVersion("4.0")]
        [Authorize]
        public async Task<IActionResult> DeleteUserConsentAsync([FromRoute] int userID, [FromRoute] int userConsentId)
        {
            using (_logger.BeginScope(_loggerStateFactory.Create(Request.Headers["Transaction-ID"])))
            {
                try
                {
                    if (userID == 0)
                    {
                        _logger.LogWarning("DeleteUserConsent - Bad Request userID");
                        return BadRequest();
                    }
                    UserResponseModel user = _userService.GetUser(userID);
                    if (user == null)
                    {
                        _logger.LogWarning("DeleteUserConsent - User NotFound - {userID}", userID);
                        return NotFound();
                    }

                    if (userConsentId == 0)
                    {
                        _logger.LogWarning("DeleteUserConsent - Bad Request userConsentId");
                        return BadRequest();
                    }

                    await _userConsentService.DeleteUserConsent(userID, userConsentId);

                    return NoContent();
                }
                catch (NotFoundException)
                {
                    _logger.LogWarning("DeleteUserConsent - NotFound - {userID}", userID);
                    return NotFound();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "DeleteUserConsent - Failed - {userID}", userID);
                    return StatusCode(500);
                }
            }
        }

        #endregion //User Consent

        #endregion //V4 End-points
    }
}
