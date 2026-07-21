using System.Net;
using System.Text;
using Core.Options;
using Core.Services.MatchSync;
using Microsoft.Extensions.Options;

namespace WorldCup_System.Tests.Services.MatchSync
{
    public class FifaCalendarMatchResultProviderTests
    {
        [Fact]
        public async Task FetchResultAsync_WhenMatchPlayed_ReturnsFinishedResultWithScores()
        {
            string json = """
                {
                  "Results": [
                    {
                      "IdMatch": "400234567",
                      "IdStage": "  12345678  ",
                      "MatchStatus": 0,
                      "HomeTeamScore": 2,
                      "AwayTeamScore": 1,
                      "Home": {
                        "IdCountry": "BRA",
                        "ShortClubName": "Brazil",
                        "TeamName": [{ "Description": "Brazil" }]
                      },
                      "Away": {
                        "IdCountry": "FRA",
                        "ShortClubName": "France",
                        "TeamName": [{ "Description": "France" }]
                      }
                    }
                  ]
                }
                """;

            using StubHttpMessageHandler handler = new StubHttpMessageHandler(HttpStatusCode.OK, json);
            using HttpClient httpClient = new HttpClient(handler)
            {
                BaseAddress = new Uri("https://api.fifa.com/api/v3/calendar/matches")
            };
            FifaCalendarMatchResultProvider provider = new FifaCalendarMatchResultProvider(
                httpClient,
                Options.Create(new MatchResultSyncOptions()));

            ExternalMatchResult result = await provider.FetchResultAsync("400234567");

            Assert.Equal("400234567", result.ExternalMatchId);
            Assert.Equal("12345678", result.ExternalStageId);
            Assert.True(result.IsFinished);
            Assert.Equal(2, result.HomeScore);
            Assert.Equal(1, result.AwayScore);
            Assert.Equal("Brazil", result.HomeTeamName);
            Assert.Equal("France", result.AwayTeamName);
            Assert.Equal("BRA", result.HomeTeamCountryCode);
            Assert.Equal("FRA", result.AwayTeamCountryCode);
            Assert.Contains("idMatch=400234567", handler.LastRequestUri);
        }

        [Fact]
        public async Task FetchResultAsync_WhenIdStageMissing_ReturnsNullExternalStageId()
        {
            string json = """
                {
                  "Results": [
                    {
                      "IdMatch": "400234567",
                      "MatchStatus": 0,
                      "HomeTeamScore": 1,
                      "AwayTeamScore": 0,
                      "Home": { "TeamName": [{ "Description": "Brazil" }] },
                      "Away": { "TeamName": [{ "Description": "France" }] }
                    }
                  ]
                }
                """;

            using StubHttpMessageHandler handler = new StubHttpMessageHandler(HttpStatusCode.OK, json);
            using HttpClient httpClient = new HttpClient(handler)
            {
                BaseAddress = new Uri("https://api.fifa.com/api/v3/calendar/matches")
            };
            FifaCalendarMatchResultProvider provider = new FifaCalendarMatchResultProvider(
                httpClient,
                Options.Create(new MatchResultSyncOptions()));

            ExternalMatchResult result = await provider.FetchResultAsync("400234567");

            Assert.Null(result.ExternalStageId);
        }

        [Fact]
        public async Task FetchResultAsync_WhenMatchStatusNotPlayed_ReturnsNotFinished()
        {
            string json = """
                {
                  "Results": [
                    {
                      "IdMatch": "400234567",
                      "MatchStatus": 3,
                      "HomeTeamScore": 0,
                      "AwayTeamScore": 0,
                      "Home": { "TeamName": [{ "Description": "Brazil" }] },
                      "Away": { "TeamName": [{ "Description": "France" }] }
                    }
                  ]
                }
                """;

            using StubHttpMessageHandler handler = new StubHttpMessageHandler(HttpStatusCode.OK, json);
            using HttpClient httpClient = new HttpClient(handler)
            {
                BaseAddress = new Uri("https://api.fifa.com/api/v3/calendar/matches")
            };
            FifaCalendarMatchResultProvider provider = new FifaCalendarMatchResultProvider(
                httpClient,
                Options.Create(new MatchResultSyncOptions()));

            ExternalMatchResult result = await provider.FetchResultAsync("400234567");

            Assert.False(result.IsFinished);
            Assert.Equal(0, result.HomeScore);
            Assert.Equal(0, result.AwayScore);
        }

        [Fact]
        public async Task FetchResultAsync_WhenHttpFails_ThrowsInvalidOperationException()
        {
            using StubHttpMessageHandler handler = new StubHttpMessageHandler(HttpStatusCode.BadGateway, "{}");
            using HttpClient httpClient = new HttpClient(handler)
            {
                BaseAddress = new Uri("https://api.fifa.com/api/v3/calendar/matches")
            };
            FifaCalendarMatchResultProvider provider = new FifaCalendarMatchResultProvider(
                httpClient,
                Options.Create(new MatchResultSyncOptions()));

            InvalidOperationException exception = await Assert.ThrowsAsync<InvalidOperationException>(
                () => provider.FetchResultAsync("400234567"));

            Assert.Contains("FIFA calendar request failed", exception.Message);
        }

        [Fact]
        public async Task FetchResultAsync_WhenMatchMissing_ThrowsInvalidOperationException()
        {
            string json = """{ "Results": [] }""";

            using StubHttpMessageHandler handler = new StubHttpMessageHandler(HttpStatusCode.OK, json);
            using HttpClient httpClient = new HttpClient(handler)
            {
                BaseAddress = new Uri("https://api.fifa.com/api/v3/calendar/matches")
            };
            FifaCalendarMatchResultProvider provider = new FifaCalendarMatchResultProvider(
                httpClient,
                Options.Create(new MatchResultSyncOptions()));

            InvalidOperationException exception = await Assert.ThrowsAsync<InvalidOperationException>(
                () => provider.FetchResultAsync("missing-id"));

            Assert.Contains("no match for external id", exception.Message);
        }

        private sealed class StubHttpMessageHandler : HttpMessageHandler
        {
            private readonly HttpStatusCode _statusCode;
            private readonly string _responseBody;

            public StubHttpMessageHandler(HttpStatusCode statusCode, string responseBody)
            {
                _statusCode = statusCode;
                _responseBody = responseBody;
            }

            public string? LastRequestUri { get; private set; }

            protected override Task<HttpResponseMessage> SendAsync(
                HttpRequestMessage request,
                CancellationToken cancellationToken)
            {
                LastRequestUri = request.RequestUri?.ToString();
                HttpResponseMessage response = new HttpResponseMessage(_statusCode)
                {
                    Content = new StringContent(_responseBody, Encoding.UTF8, "application/json")
                };
                return Task.FromResult(response);
            }
        }
    }
}
