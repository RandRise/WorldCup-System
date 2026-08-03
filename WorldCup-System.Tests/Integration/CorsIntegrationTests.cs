namespace WorldCup_System.Tests.Integration
{
    public class CorsIntegrationTests : IClassFixture<WorldCupWebApplicationFactory>
    {
        private readonly HttpClient _client;

        public CorsIntegrationTests(WorldCupWebApplicationFactory factory)
        {
            _client = factory.CreateClient();
        }

        [Fact]
        public async Task Health_WithAllowedOrigin_ReturnsAccessControlAllowOrigin()
        {
            HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, "/health");
            request.Headers.Add("Origin", "http://localhost:4200");

            HttpResponseMessage response = await _client.SendAsync(request);

            response.EnsureSuccessStatusCode();
            Assert.True(response.Headers.TryGetValues("Access-Control-Allow-Origin", out IEnumerable<string>? values));
            Assert.Equal("http://localhost:4200", Assert.Single(values));
            Assert.True(response.Headers.TryGetValues("Access-Control-Allow-Credentials", out IEnumerable<string>? credentialValues));
            Assert.Equal("true", Assert.Single(credentialValues));
        }

        [Fact]
        public async Task Health_WithDisallowedOrigin_DoesNotReflectOrigin()
        {
            HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, "/health");
            request.Headers.Add("Origin", "https://evil.example");

            HttpResponseMessage response = await _client.SendAsync(request);

            response.EnsureSuccessStatusCode();
            bool hasAllowOrigin = response.Headers.TryGetValues("Access-Control-Allow-Origin", out IEnumerable<string>? values);
            if (hasAllowOrigin)
            {
                Assert.DoesNotContain("https://evil.example", values!);
            }
        }

        [Fact]
        public async Task Health_OptionsPreflight_WithAllowedOrigin_ReturnsCorsHeaders()
        {
            HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Options, "/health");
            request.Headers.Add("Origin", "http://localhost:4200");
            request.Headers.Add("Access-Control-Request-Method", "GET");

            HttpResponseMessage response = await _client.SendAsync(request);

            Assert.True(response.Headers.TryGetValues("Access-Control-Allow-Origin", out IEnumerable<string>? values));
            Assert.Equal("http://localhost:4200", Assert.Single(values));
            Assert.True(response.Headers.TryGetValues("Access-Control-Allow-Credentials", out IEnumerable<string>? credentialValues));
            Assert.Equal("true", Assert.Single(credentialValues));
        }

        [Fact]
        public async Task Health_OptionsPreflight_WithDisallowedOrigin_DoesNotReflectOrigin()
        {
            HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Options, "/health");
            request.Headers.Add("Origin", "https://evil.example");
            request.Headers.Add("Access-Control-Request-Method", "GET");

            HttpResponseMessage response = await _client.SendAsync(request);

            bool hasAllowOrigin = response.Headers.TryGetValues("Access-Control-Allow-Origin", out IEnumerable<string>? values);
            if (hasAllowOrigin)
            {
                Assert.DoesNotContain("https://evil.example", values!);
            }
        }
    }
}
