namespace WorldCup_System.Tests.Integration
{
    public class HealthEndpointIntegrationTests : IClassFixture<WorldCupWebApplicationFactory>
    {
        private readonly HttpClient _client;

        public HealthEndpointIntegrationTests(WorldCupWebApplicationFactory factory)
        {
            _client = factory.CreateClient();
        }

        [Fact]
        public async Task Health_ReturnsHealthyStatus()
        {
            HttpResponseMessage response = await _client.GetAsync("/health");

            response.EnsureSuccessStatusCode();
            string content = await response.Content.ReadAsStringAsync();
            Assert.Contains("Healthy", content, StringComparison.OrdinalIgnoreCase);
        }
    }
}
