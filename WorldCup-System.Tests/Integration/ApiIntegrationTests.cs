using Core.DTOs.Countries;
using System.Net.Http.Json;

namespace WorldCup_System.Tests.Integration
{
    public class ApiIntegrationTests : IClassFixture<WorldCupWebApplicationFactory>
    {
        private readonly HttpClient _client;

        public ApiIntegrationTests(WorldCupWebApplicationFactory factory)
        {
            _client = factory.CreateClient();
        }

        [Fact]
        public async Task GetAllCountries_ReturnsOkWithJsonArray()
        {
            HttpResponseMessage response = await _client.GetAsync("/Country/GetAllCountries");

            response.EnsureSuccessStatusCode();
            List<CountryDTO>? countries = await response.Content.ReadFromJsonAsync<List<CountryDTO>>();
            Assert.NotNull(countries);
        }
    }
}
