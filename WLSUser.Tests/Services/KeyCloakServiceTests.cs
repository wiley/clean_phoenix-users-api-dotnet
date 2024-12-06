using Moq;
using Moq.Protected;
using Newtonsoft.Json;
using NSubstitute;

using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using WLSUser.Domain.Models;
using WLSUser.Domain.Models.Authentication;
using WLSUser.Services.Authentication;
using WLSUser.Tests.Util;

using Xunit;

namespace WLSUser.Tests.Services
{
	public class KeyCloakServiceTests
	{
		private readonly MockLogger<KeyCloakService> _serviceLogger;
		private readonly IHttpClientFactory _httpFactory;

		public KeyCloakServiceTests()
		{
			_serviceLogger = Substitute.For<MockLogger<KeyCloakService>>();
			_httpFactory = Substitute.For<IHttpClientFactory>();
		}

		#region GetPasswordTokens
		[Fact]
		public async Task GetPasswordTokens_Sucess()
		{
			//Arrange
            string jsonUsersResponse = @"[ { ""attributes"": { ""user_id"": [""1""],""email"": [""user@example.com""] } } ]";

            var user = new UserModel
			{
				Username = "test",
				UserID = 1
			};

			var mockMessageHandler = new Mock<HttpMessageHandler>();
			mockMessageHandler.Protected()
							  .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.Is<HttpRequestMessage>(r => r.Method == HttpMethod.Get), ItExpr.IsAny<CancellationToken>())
							  .ReturnsAsync(new HttpResponseMessage
							  {
								StatusCode = HttpStatusCode.OK,
								Content = new StringContent(jsonUsersResponse, Encoding.UTF8, "application/json")
                              });

            mockMessageHandler.Protected()
                  .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.Is<HttpRequestMessage>(r => r.Method == HttpMethod.Post), ItExpr.IsAny<CancellationToken>())
                  .ReturnsAsync(new HttpResponseMessage
                  {
                      StatusCode = HttpStatusCode.OK,
                      Content = new StringContent(JsonConvert.SerializeObject(new AuthResponseV4() { AccessToken = "accesstoken", RefreshToken = "refreshtoken"}), Encoding.UTF8, "application/json")
                  });

            var httpClient = new HttpClient(mockMessageHandler.Object)
            {
                BaseAddress = new Uri("http://www.url.com/api")
            };

            _httpFactory.CreateClient().ReturnsForAnyArgs(httpClient);
            var keyCloakService = new KeyCloakService(_serviceLogger, _httpFactory);

			//Act
            var response = await keyCloakService.GetPasswordTokens(user);

            //Assert
            Assert.IsType<AuthResponseV4>(response);
            Assert.Equal("accesstoken", response.AccessToken);
            Assert.Equal("refreshtoken", response.RefreshToken);
        }

		[Fact]
		public async Task GetPasswordTokens_UserNotFoundFail()
		{
            //Arrange
            string jsonUsersResponse = @"[ { ""attributes"": { ""user_id"": [""123""],""email"": [""user@example.com""] } } ]";

            var user = new UserModel
            {
                Username = "test",
                UserID = 1
            };

            var mockMessageHandler = new Mock<HttpMessageHandler>();
            mockMessageHandler.Protected()
                              .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.Is<HttpRequestMessage>(r => r.Method == HttpMethod.Get), ItExpr.IsAny<CancellationToken>())
                              .ReturnsAsync(new HttpResponseMessage
                              {
                                  StatusCode = HttpStatusCode.OK,
                                  Content = new StringContent(jsonUsersResponse, Encoding.UTF8, "application/json")
                              });

            mockMessageHandler.Protected()
                  .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.Is<HttpRequestMessage>(r => r.Method == HttpMethod.Post), ItExpr.IsAny<CancellationToken>())
                  .ReturnsAsync(new HttpResponseMessage
                  {
                      StatusCode = HttpStatusCode.OK,
                      Content = new StringContent(JsonConvert.SerializeObject(new AuthResponseV4() { AccessToken = "accesstoken", RefreshToken = "refreshtoken" }), Encoding.UTF8, "application/json")
                  });

            var httpClient = new HttpClient(mockMessageHandler.Object)
            {
                BaseAddress = new Uri("http://www.url.com/api")
            };

            _httpFactory.CreateClient().ReturnsForAnyArgs(httpClient);
            var keyCloakService = new KeyCloakService(_serviceLogger, _httpFactory);

			//Act and Assert
			await Assert.ThrowsAsync<Exception>(async () => await keyCloakService.GetPasswordTokens(user));
        }

        [Fact]
        public async Task GetPasswordTokens_FailToGetTokens()
        {
            //Arrange
            string jsonUsersResponse = @"[ { ""attributes"": { ""user_id"": [""1""],""email"": [""user@example.com""] } } ]";

            var user = new UserModel
            {
                Username = "test",
                UserID = 1
            };

            var mockMessageHandler = new Mock<HttpMessageHandler>();
            mockMessageHandler.Protected()
                              .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.Is<HttpRequestMessage>(r => r.Method == HttpMethod.Get), ItExpr.IsAny<CancellationToken>())
                              .ReturnsAsync(new HttpResponseMessage
                              {
                                  StatusCode = HttpStatusCode.OK,
                                  Content = new StringContent(jsonUsersResponse, Encoding.UTF8, "application/json")
                              });

            mockMessageHandler.Protected()
                  .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.Is<HttpRequestMessage>(r => r.Method == HttpMethod.Post), ItExpr.IsAny<CancellationToken>())
                  .ReturnsAsync(new HttpResponseMessage
                  {
                      StatusCode = HttpStatusCode.InternalServerError,
                      Content = new StringContent("anything")
                  });

            var httpClient = new HttpClient(mockMessageHandler.Object)
            {
                BaseAddress = new Uri("http://www.url.com/api")
            };

            _httpFactory.CreateClient().ReturnsForAnyArgs(httpClient);
            var keyCloakService = new KeyCloakService(_serviceLogger, _httpFactory);

            //Act and Assert
            await Assert.ThrowsAsync<Exception>(async () => await keyCloakService.GetPasswordTokens(user));
        }
        #endregion

        #region GetJwt
        [Fact]
		public async Task GetJwt_Successful()
		{
			var client = FakeHttpClient.GetFakeClientSimple(
				HttpStatusCode.OK,
				"{'access_token': 'jwtAccessToken', 'refresh_token': 'jwtRefreshToken'}");
			_httpFactory.CreateClient().ReturnsForAnyArgs(client);

			var keyCloakService = new KeyCloakService(_serviceLogger, _httpFactory);
			var response = await keyCloakService.GetJwt(Arg.Any<string>());

			Assert.IsType<Dictionary<string, string>>(response);
			Assert.Equal("jwtAccessToken", response["access_token"]);
			Assert.Equal("jwtRefreshToken", response["refresh_token"]);
		}

		[Fact]
		public async Task GetJwt_RequestFailed_ReturnsException()
		{
			var client = FakeHttpClient.GetFakeClientSimple(HttpStatusCode.InternalServerError, String.Empty);
			_httpFactory.CreateClient().ReturnsForAnyArgs(client);

			var keyCloakService = new KeyCloakService(_serviceLogger, _httpFactory);

			await Assert.ThrowsAsync<Exception>(() => keyCloakService.GetJwt(Arg.Any<string>()));
		}

		[Fact]
		public async Task GetJwt_EmptyResponse_ReturnsException()
		{
			var client = FakeHttpClient.GetFakeClientSimple(HttpStatusCode.OK, String.Empty);
			_httpFactory.CreateClient().ReturnsForAnyArgs(client);

			var keyCloakService = new KeyCloakService(_serviceLogger, _httpFactory);

            await Assert.ThrowsAsync<Exception>(() => keyCloakService.GetJwt(Arg.Any<string>()));
		}
		#endregion

		#region IsUserExists
		[Fact]
		public async Task IsUserExists_RespondsTrue_Successful()
		{
			string content = "[{'id': 'ee555254-1e29-4cfc-b525-3abdbb43dfac'}]";

			var client = FakeHttpClient.GetFakeClientSimple(HttpStatusCode.OK, content);
			_httpFactory.CreateClient().ReturnsForAnyArgs(client);

			var keyCloakService = new KeyCloakService(_serviceLogger, _httpFactory);
			var response = await keyCloakService.UserExists(Arg.Any<string>());

			Assert.True(response);
		}

		[Fact]
		public async Task IsUserExists_RespondsFalse_Successful()
		{
			var client = FakeHttpClient.GetFakeClientSimple(HttpStatusCode.OK, "[]");
			_httpFactory.CreateClient().ReturnsForAnyArgs(client);

			var keyCloakService = new KeyCloakService(_serviceLogger, _httpFactory);
			var response = await keyCloakService.UserExists(Arg.Any<string>());

			Assert.False(response);
		}

		[Fact]
		public async Task IsUserExists_RequestFail_ReturnsFalse()
		{
			var client = FakeHttpClient.GetFakeClientSimple(HttpStatusCode.Unauthorized, String.Empty);
			_httpFactory.CreateClient().ReturnsForAnyArgs(client);

			var keyCloakService = new KeyCloakService(_serviceLogger, _httpFactory);
			var response = await keyCloakService.UserExists(Arg.Any<string>());

			Assert.False(response);
		}
		#endregion

		#region CreateUser
		[Fact]
		public async Task CreateUser_Successful()
		{
			var client = FakeHttpClient.GetFakeClientSimple(HttpStatusCode.Created, "[]");
			_httpFactory.CreateClient().ReturnsForAnyArgs(client);

			var keyCloakService = new KeyCloakService(_serviceLogger, _httpFactory);
			var response = await keyCloakService.CreateUser(Arg.Any<string>());

			Assert.True(response);
		}

		[Fact]
		public async Task CreateUser_RequestFail_ReturnsFalse()
		{
			var client = FakeHttpClient.GetFakeClientSimple(HttpStatusCode.Unauthorized, String.Empty);
			_httpFactory.CreateClient().ReturnsForAnyArgs(client);

			var keyCloakService = new KeyCloakService(_serviceLogger, _httpFactory);

			var response = await keyCloakService.CreateUser(Arg.Any<string>());

			Assert.False(response);
		}
        #endregion

        #region Logout
        [Fact]
        public async Task Logout_Successful()
        {
            var client = FakeHttpClient.GetFakeClientSimple(HttpStatusCode.OK, String.Empty);
            _httpFactory.CreateClient().ReturnsForAnyArgs(client);

            var keyCloakService = new KeyCloakService(_serviceLogger, _httpFactory);

            var exception = await Record.ExceptionAsync(async () => await keyCloakService.Logout(Arg.Any<string>()));
            Assert.Null(exception);
        }

        [Fact]
        public async Task Logout_FailedWithException()
        {
            var client = FakeHttpClient.GetFakeClientSimple(HttpStatusCode.Unauthorized, String.Empty);
            _httpFactory.CreateClient().ReturnsForAnyArgs(client);

            var keyCloakService = new KeyCloakService(_serviceLogger, _httpFactory);

            await Assert.ThrowsAsync<Exception>(async () => await keyCloakService.Logout(Arg.Any<string>()));
        }
        #endregion
    }
}
