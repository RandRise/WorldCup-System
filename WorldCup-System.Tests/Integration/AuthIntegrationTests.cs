using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Core.DTOs.Users;

namespace WorldCup_System.Tests.Integration
{
    public class AuthIntegrationTests : IClassFixture<WorldCupWebApplicationFactory>
    {
        private readonly HttpClient _client;

        public AuthIntegrationTests(WorldCupWebApplicationFactory factory)
        {
            _client = factory.CreateClient();
        }

        [Fact]
        public async Task Login_InvalidCredentials_ReturnsUnauthorized()
        {
            LoginModel loginModel = new LoginModel
            {
                Email = "nobody@example.com",
                Password = "WrongPass1!"
            };

            HttpResponseMessage response = await _client.PostAsJsonAsync("/User/Login", loginModel);

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task RegisterAndLogin_ReturnsToken()
        {
            string uniqueEmail = $"integration-{Guid.NewGuid():N}@example.com";
            CreateUserDto createUserDto = new CreateUserDto
            {
                Name = "Integration User",
                Email = uniqueEmail,
                Password = "TestPass1!"
            };

            HttpResponseMessage registerResponse = await _client.PostAsJsonAsync("/User/CreateNewUser", createUserDto);
            registerResponse.EnsureSuccessStatusCode();

            LoginModel loginModel = new LoginModel
            {
                Email = uniqueEmail,
                Password = "TestPass1!"
            };

            HttpResponseMessage loginResponse = await _client.PostAsJsonAsync("/User/Login", loginModel);
            loginResponse.EnsureSuccessStatusCode();

            JsonElement loginBody = await loginResponse.Content.ReadFromJsonAsync<JsonElement>();
            Assert.True(loginBody.TryGetProperty("token", out JsonElement tokenElement));
            Assert.False(string.IsNullOrWhiteSpace(tokenElement.GetString()));
        }
    }
}
