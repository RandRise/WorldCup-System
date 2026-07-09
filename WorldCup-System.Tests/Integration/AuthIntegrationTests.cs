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
            _client = factory.CreateClient(new Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactoryClientOptions
            {
                AllowAutoRedirect = false
            });
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
            string token = tokenElement.GetString() ?? string.Empty;
            Assert.False(string.IsNullOrWhiteSpace(token));

            using HttpRequestMessage getMeRequest = new HttpRequestMessage(HttpMethod.Get, "/User/GetMe");
            getMeRequest.Headers.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

            HttpResponseMessage getMeResponse = await _client.SendAsync(getMeRequest);
            getMeResponse.EnsureSuccessStatusCode();

            JsonElement profile = await getMeResponse.Content.ReadFromJsonAsync<JsonElement>();
            Assert.Equal(uniqueEmail, profile.GetProperty("email").GetString());
            Assert.Equal("Integration User", profile.GetProperty("name").GetString());
            Assert.Contains("User", profile.GetProperty("roles").EnumerateArray().Select(role => role.GetString()));
        }
    }
}
