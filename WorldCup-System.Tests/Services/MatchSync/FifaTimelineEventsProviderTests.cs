using System.Net;
using System.Text;
using Core.Options;
using Core.Services.MatchSync;
using Microsoft.Extensions.Options;

namespace WorldCup_System.Tests.Services.MatchSync
{
    public class FifaTimelineEventsProviderTests
    {
        /// <summary>
        /// Recorded shape from FIFA timeline for Mexico 2–0 South Africa (IdMatch 400021443).
        /// </summary>
        private const string MexicoSouthAfricaTimelineJson = """
            {
              "IdCompetition": "17",
              "IdSeason": "285023",
              "IdStage": "289273",
              "IdMatch": "400021443",
              "Event": [
                {
                  "IdTeam": "43911",
                  "IdPlayer": "429157",
                  "MatchMinute": "9'",
                  "HomeGoals": 1,
                  "AwayGoals": 0,
                  "TypeLocalized": [{ "Locale": "en-GB", "Description": "Goal!" }],
                  "EventDescription": [{ "Locale": "en-GB", "Description": "Julian QUINONES (Mexico) scores!!" }]
                },
                {
                  "IdTeam": "43911",
                  "IdPlayer": "356731",
                  "MatchMinute": "67'",
                  "HomeGoals": 2,
                  "AwayGoals": 0,
                  "TypeLocalized": [{ "Locale": "en-GB", "Description": "Goal!" }],
                  "EventDescription": [{ "Locale": "en-GB", "Description": "Raul JIMENEZ (Mexico) scores!!" }]
                }
              ]
            }
            """;

        [Fact]
        public async Task FetchGoalEventsAsync_MexicoSouthAfrica_ReturnsQuiñonesAndJimenezGoals()
        {
            using StubHttpMessageHandler handler = new StubHttpMessageHandler(HttpStatusCode.OK, MexicoSouthAfricaTimelineJson);
            using HttpClient httpClient = new HttpClient(handler)
            {
                BaseAddress = new Uri("https://api.fifa.com/api/v3/timelines/")
            };
            FifaTimelineEventsProvider provider = new FifaTimelineEventsProvider(
                httpClient,
                Options.Create(new MatchResultSyncOptions()));

            ExternalMatchEvents result = await provider.FetchGoalEventsAsync("400021443", "289273");

            Assert.Equal("400021443", result.ExternalMatchId);
            Assert.Equal("289273", result.ExternalStageId);
            Assert.Equal(2, result.Goals.Count);

            ExternalMatchGoalEvent first = result.Goals[0];
            Assert.Equal("429157", first.ExternalPlayerId);
            Assert.Equal("43911", first.ExternalTeamId);
            Assert.Equal("Julian QUINONES", first.PlayerDisplayName);
            Assert.Equal(9, first.Minute);
            Assert.True(first.IsHomeSide);
            Assert.False(first.IsOwnGoal);
            Assert.Equal("Goal!", first.EventTypeLabel);

            ExternalMatchGoalEvent second = result.Goals[1];
            Assert.Equal("356731", second.ExternalPlayerId);
            Assert.Equal("Raul JIMENEZ", second.PlayerDisplayName);
            Assert.Equal(67, second.Minute);
            Assert.True(second.IsHomeSide);
            Assert.False(second.IsOwnGoal);

            Assert.Contains("17/285023/289273/400021443", handler.LastRequestUri);
            Assert.Contains("language=en", handler.LastRequestUri);
        }

        [Fact]
        public async Task FetchGoalEventsAsync_WhenHttpFails_ThrowsInvalidOperationException()
        {
            using StubHttpMessageHandler handler = new StubHttpMessageHandler(HttpStatusCode.BadGateway, "{}");
            using HttpClient httpClient = new HttpClient(handler)
            {
                BaseAddress = new Uri("https://api.fifa.com/api/v3/timelines/")
            };
            FifaTimelineEventsProvider provider = new FifaTimelineEventsProvider(
                httpClient,
                Options.Create(new MatchResultSyncOptions()));

            InvalidOperationException exception = await Assert.ThrowsAsync<InvalidOperationException>(
                () => provider.FetchGoalEventsAsync("400021443", "289273"));

            Assert.Contains("FIFA timeline request failed", exception.Message);
        }

        [Fact]
        public async Task FetchGoalEventsAsync_WhenStageIdMissing_ThrowsArgumentException()
        {
            using StubHttpMessageHandler handler = new StubHttpMessageHandler(HttpStatusCode.OK, "{}");
            using HttpClient httpClient = new HttpClient(handler)
            {
                BaseAddress = new Uri("https://api.fifa.com/api/v3/timelines/")
            };
            FifaTimelineEventsProvider provider = new FifaTimelineEventsProvider(
                httpClient,
                Options.Create(new MatchResultSyncOptions()));

            await Assert.ThrowsAsync<ArgumentException>(
                () => provider.FetchGoalEventsAsync("400021443", "  "));
        }

        [Fact]
        public async Task FetchGoalEventsAsync_WhenMatchIdMissing_ThrowsArgumentException()
        {
            using StubHttpMessageHandler handler = new StubHttpMessageHandler(HttpStatusCode.OK, "{}");
            using HttpClient httpClient = new HttpClient(handler)
            {
                BaseAddress = new Uri("https://api.fifa.com/api/v3/timelines/")
            };
            FifaTimelineEventsProvider provider = new FifaTimelineEventsProvider(
                httpClient,
                Options.Create(new MatchResultSyncOptions()));

            ArgumentException exception = await Assert.ThrowsAsync<ArgumentException>(
                () => provider.FetchGoalEventsAsync("  ", "289273"));

            Assert.Equal("externalMatchId", exception.ParamName);
        }

        [Fact]
        public async Task FetchGoalEventsAsync_WhenEventArrayMissing_ThrowsInvalidOperationException()
        {
            string json = """{ "IdMatch": "400021443", "IdStage": "289273" }""";

            using StubHttpMessageHandler handler = new StubHttpMessageHandler(HttpStatusCode.OK, json);
            using HttpClient httpClient = new HttpClient(handler)
            {
                BaseAddress = new Uri("https://api.fifa.com/api/v3/timelines/")
            };
            FifaTimelineEventsProvider provider = new FifaTimelineEventsProvider(
                httpClient,
                Options.Create(new MatchResultSyncOptions()));

            InvalidOperationException exception = await Assert.ThrowsAsync<InvalidOperationException>(
                () => provider.FetchGoalEventsAsync("400021443", "289273"));

            Assert.Contains("Event array", exception.Message);
        }

        [Fact]
        public async Task FetchGoalEventsAsync_WhenIdMatchMismatch_ThrowsInvalidOperationException()
        {
            string json = """
                {
                  "IdMatch": "999999999",
                  "IdStage": "289273",
                  "Event": []
                }
                """;

            using StubHttpMessageHandler handler = new StubHttpMessageHandler(HttpStatusCode.OK, json);
            using HttpClient httpClient = new HttpClient(handler)
            {
                BaseAddress = new Uri("https://api.fifa.com/api/v3/timelines/")
            };
            FifaTimelineEventsProvider provider = new FifaTimelineEventsProvider(
                httpClient,
                Options.Create(new MatchResultSyncOptions()));

            InvalidOperationException exception = await Assert.ThrowsAsync<InvalidOperationException>(
                () => provider.FetchGoalEventsAsync("400021443", "289273"));

            Assert.Contains("does not match requested", exception.Message);
        }

        [Fact]
        public async Task FetchGoalEventsAsync_WhenEventArrayEmpty_ReturnsNoGoals()
        {
            string json = """
                {
                  "IdMatch": "400021443",
                  "IdStage": "289273",
                  "Event": []
                }
                """;

            using StubHttpMessageHandler handler = new StubHttpMessageHandler(HttpStatusCode.OK, json);
            using HttpClient httpClient = new HttpClient(handler)
            {
                BaseAddress = new Uri("https://api.fifa.com/api/v3/timelines/")
            };
            FifaTimelineEventsProvider provider = new FifaTimelineEventsProvider(
                httpClient,
                Options.Create(new MatchResultSyncOptions()));

            ExternalMatchEvents result = await provider.FetchGoalEventsAsync("400021443", "289273");

            Assert.Equal("400021443", result.ExternalMatchId);
            Assert.Equal("289273", result.ExternalStageId);
            Assert.Empty(result.Goals);
        }

        [Fact]
        public async Task FetchGoalEventsAsync_WhenPayloadOmitsIds_UsesRequestedIds()
        {
            string json = """
                {
                  "Event": [
                    {
                      "IdTeam": "10",
                      "IdPlayer": "20",
                      "MatchMinute": "12'",
                      "HomeGoals": 1,
                      "AwayGoals": 0,
                      "TypeLocalized": [{ "Description": "Goal!" }],
                      "EventDescription": [{ "Description": "Fallback SCORER (Home) scores!!" }]
                    }
                  ]
                }
                """;

            using StubHttpMessageHandler handler = new StubHttpMessageHandler(HttpStatusCode.OK, json);
            using HttpClient httpClient = new HttpClient(handler)
            {
                BaseAddress = new Uri("https://api.fifa.com/api/v3/timelines/")
            };
            FifaTimelineEventsProvider provider = new FifaTimelineEventsProvider(
                httpClient,
                Options.Create(new MatchResultSyncOptions()));

            ExternalMatchEvents result = await provider.FetchGoalEventsAsync("requested-match", "requested-stage");

            Assert.Equal("requested-match", result.ExternalMatchId);
            Assert.Equal("requested-stage", result.ExternalStageId);
            Assert.Single(result.Goals);
            Assert.Equal("requested-match", result.Goals[0].ExternalMatchId);
        }

        [Fact]
        public async Task FetchGoalEventsAsync_TrimsMatchAndStageIdsInRequest()
        {
            string json = """
                {
                  "IdMatch": "400021443",
                  "IdStage": "289273",
                  "Event": []
                }
                """;

            using StubHttpMessageHandler handler = new StubHttpMessageHandler(HttpStatusCode.OK, json);
            using HttpClient httpClient = new HttpClient(handler)
            {
                BaseAddress = new Uri("https://api.fifa.com/api/v3/timelines/")
            };
            FifaTimelineEventsProvider provider = new FifaTimelineEventsProvider(
                httpClient,
                Options.Create(new MatchResultSyncOptions()));

            await provider.FetchGoalEventsAsync("  400021443  ", "  289273  ");

            Assert.Contains("17/285023/289273/400021443", handler.LastRequestUri);
            Assert.DoesNotContain("%20", handler.LastRequestUri);
        }

        [Fact]
        public async Task FetchGoalEventsAsync_SkipsGoalLabelWhenScoreUnchanged()
        {
            string json = """
                {
                  "IdMatch": "1",
                  "IdStage": "2",
                  "Event": [
                    {
                      "IdTeam": "10",
                      "IdPlayer": "20",
                      "MatchMinute": "10'",
                      "HomeGoals": 1,
                      "AwayGoals": 0,
                      "TypeLocalized": [{ "Description": "Goal!" }],
                      "EventDescription": [{ "Description": "Real SCORER (Home) scores!!" }]
                    },
                    {
                      "IdTeam": "10",
                      "IdPlayer": "99",
                      "MatchMinute": "11'",
                      "HomeGoals": 1,
                      "AwayGoals": 0,
                      "TypeLocalized": [{ "Description": "Goal!" }],
                      "EventDescription": [{ "Description": "Duplicate GHOST (Home) scores!!" }]
                    }
                  ]
                }
                """;

            using StubHttpMessageHandler handler = new StubHttpMessageHandler(HttpStatusCode.OK, json);
            using HttpClient httpClient = new HttpClient(handler)
            {
                BaseAddress = new Uri("https://api.fifa.com/api/v3/timelines/")
            };
            FifaTimelineEventsProvider provider = new FifaTimelineEventsProvider(
                httpClient,
                Options.Create(new MatchResultSyncOptions()));

            ExternalMatchEvents result = await provider.FetchGoalEventsAsync("1", "2");

            Assert.Single(result.Goals);
            Assert.Equal("Real SCORER", result.Goals[0].PlayerDisplayName);
        }

        [Fact]
        public async Task FetchGoalEventsAsync_IgnoresNonGoalEvents()
        {
            string json = """
                {
                  "IdMatch": "1",
                  "IdStage": "2",
                  "Event": [
                    {
                      "MatchMinute": "4'",
                      "HomeGoals": 0,
                      "AwayGoals": 0,
                      "TypeLocalized": [{ "Description": "Attempt at Goal" }],
                      "EventDescription": [{ "Description": "Someone shoots" }]
                    },
                    {
                      "IdTeam": "10",
                      "IdPlayer": "20",
                      "MatchMinute": "10'",
                      "HomeGoals": 0,
                      "AwayGoals": 1,
                      "TypeLocalized": [{ "Description": "Goal!" }],
                      "EventDescription": [{ "Description": "Away SCORER (Away) scores!!" }]
                    }
                  ]
                }
                """;

            using StubHttpMessageHandler handler = new StubHttpMessageHandler(HttpStatusCode.OK, json);
            using HttpClient httpClient = new HttpClient(handler)
            {
                BaseAddress = new Uri("https://api.fifa.com/api/v3/timelines/")
            };
            FifaTimelineEventsProvider provider = new FifaTimelineEventsProvider(
                httpClient,
                Options.Create(new MatchResultSyncOptions()));

            ExternalMatchEvents result = await provider.FetchGoalEventsAsync("1", "2");

            Assert.Single(result.Goals);
            Assert.False(result.Goals[0].IsHomeSide);
            Assert.Equal("Away SCORER", result.Goals[0].PlayerDisplayName);
        }

        [Fact]
        public async Task FetchGoalEventsAsync_ParsesOwnGoalAndPenaltyTypes()
        {
            string json = """
                {
                  "IdMatch": "1",
                  "IdStage": "2",
                  "Event": [
                    {
                      "IdTeam": "away-team",
                      "IdPlayer": "og-player",
                      "MatchMinute": "22'",
                      "HomeGoals": 1,
                      "AwayGoals": 0,
                      "TypeLocalized": [{ "Description": "Own Goal" }],
                      "EventDescription": [{ "Description": "Own GOALER (Away) scores an own goal" }]
                    },
                    {
                      "IdTeam": "home-team",
                      "IdPlayer": "pen-player",
                      "MatchMinute": "55'",
                      "HomeGoals": 2,
                      "AwayGoals": 0,
                      "TypeLocalized": [{ "Description": "Penalty Goal" }],
                      "EventDescription": [{ "Description": "Pen TAKER (Home) scores!!" }]
                    }
                  ]
                }
                """;

            using StubHttpMessageHandler handler = new StubHttpMessageHandler(HttpStatusCode.OK, json);
            using HttpClient httpClient = new HttpClient(handler)
            {
                BaseAddress = new Uri("https://api.fifa.com/api/v3/timelines/")
            };
            FifaTimelineEventsProvider provider = new FifaTimelineEventsProvider(
                httpClient,
                Options.Create(new MatchResultSyncOptions()));

            ExternalMatchEvents result = await provider.FetchGoalEventsAsync("1", "2");

            Assert.Equal(2, result.Goals.Count);
            Assert.True(result.Goals[0].IsOwnGoal);
            Assert.True(result.Goals[0].IsHomeSide);
            Assert.Equal("Own GOALER", result.Goals[0].PlayerDisplayName);
            Assert.True(result.Goals[1].IsPenalty);
            Assert.False(result.Goals[1].IsOwnGoal);
            Assert.Equal("Pen TAKER", result.Goals[1].PlayerDisplayName);
        }

        [Fact]
        public async Task FetchGoalEventsAsync_RecognizesAlternateOwnGoalAndPenaltyLabels()
        {
            string json = """
                {
                  "IdMatch": "1",
                  "IdStage": "2",
                  "Event": [
                    {
                      "IdTeam": "away-team",
                      "IdPlayer": "og-player",
                      "MatchMinute": "18'",
                      "HomeGoals": 1,
                      "AwayGoals": 0,
                      "TypeLocalized": [{ "Description": "Own Goal!" }],
                      "EventDescription": [{ "Description": "Alt OG (Away) own goal" }]
                    },
                    {
                      "IdTeam": "home-team",
                      "IdPlayer": "pen-player",
                      "MatchMinute": "70'",
                      "HomeGoals": 2,
                      "AwayGoals": 0,
                      "TypeLocalized": [{ "Description": "Penalty!" }],
                      "EventDescription": [{ "Description": "Alt PEN (Home) scores!!" }]
                    },
                    {
                      "IdTeam": "home-team",
                      "IdPlayer": "pen2-player",
                      "MatchMinute": "88'",
                      "HomeGoals": 3,
                      "AwayGoals": 0,
                      "TypeLocalized": [{ "Description": "Penalty Goal!" }],
                      "EventDescription": [{ "Description": "Alt PEN2 (Home) scores!!" }]
                    }
                  ]
                }
                """;

            using StubHttpMessageHandler handler = new StubHttpMessageHandler(HttpStatusCode.OK, json);
            using HttpClient httpClient = new HttpClient(handler)
            {
                BaseAddress = new Uri("https://api.fifa.com/api/v3/timelines/")
            };
            FifaTimelineEventsProvider provider = new FifaTimelineEventsProvider(
                httpClient,
                Options.Create(new MatchResultSyncOptions()));

            ExternalMatchEvents result = await provider.FetchGoalEventsAsync("1", "2");

            Assert.Equal(3, result.Goals.Count);
            Assert.True(result.Goals[0].IsOwnGoal);
            Assert.Equal("Own Goal!", result.Goals[0].EventTypeLabel);
            Assert.True(result.Goals[1].IsPenalty);
            Assert.Equal("Penalty!", result.Goals[1].EventTypeLabel);
            Assert.True(result.Goals[2].IsPenalty);
            Assert.Equal("Penalty Goal!", result.Goals[2].EventTypeLabel);
        }

        [Fact]
        public async Task FetchGoalEventsAsync_AmbiguousScoreDelta_RegularGoalDefaultsHome_OwnGoalDefaultsAway()
        {
            string json = """
                {
                  "IdMatch": "1",
                  "IdStage": "2",
                  "Event": [
                    {
                      "IdTeam": "10",
                      "IdPlayer": "20",
                      "MatchMinute": "10'",
                      "HomeGoals": 1,
                      "AwayGoals": 1,
                      "TypeLocalized": [{ "Description": "Goal!" }],
                      "EventDescription": [{ "Description": "Ambiguous REG (Home) scores!!" }]
                    },
                    {
                      "IdTeam": "11",
                      "IdPlayer": "21",
                      "MatchMinute": "40'",
                      "HomeGoals": 2,
                      "AwayGoals": 2,
                      "TypeLocalized": [{ "Description": "Own Goal" }],
                      "EventDescription": [{ "Description": "Ambiguous OG (Away) own goal" }]
                    }
                  ]
                }
                """;

            using StubHttpMessageHandler handler = new StubHttpMessageHandler(HttpStatusCode.OK, json);
            using HttpClient httpClient = new HttpClient(handler)
            {
                BaseAddress = new Uri("https://api.fifa.com/api/v3/timelines/")
            };
            FifaTimelineEventsProvider provider = new FifaTimelineEventsProvider(
                httpClient,
                Options.Create(new MatchResultSyncOptions()));

            ExternalMatchEvents result = await provider.FetchGoalEventsAsync("1", "2");

            Assert.Equal(2, result.Goals.Count);
            Assert.True(result.Goals[0].IsHomeSide);
            Assert.False(result.Goals[0].IsOwnGoal);
            Assert.False(result.Goals[1].IsHomeSide);
            Assert.True(result.Goals[1].IsOwnGoal);
        }

        [Fact]
        public async Task FetchGoalEventsAsync_BlankTeamAndPlayerIds_MapToNull()
        {
            string json = """
                {
                  "IdMatch": "1",
                  "IdStage": "2",
                  "Event": [
                    {
                      "IdTeam": "  ",
                      "IdPlayer": "  ",
                      "MatchMinute": "15'",
                      "HomeGoals": 1,
                      "AwayGoals": 0,
                      "TypeLocalized": [{ "Description": "Goal!" }],
                      "EventDescription": [{ "Description": "Nameless SCORER (Home) scores!!" }]
                    }
                  ]
                }
                """;

            using StubHttpMessageHandler handler = new StubHttpMessageHandler(HttpStatusCode.OK, json);
            using HttpClient httpClient = new HttpClient(handler)
            {
                BaseAddress = new Uri("https://api.fifa.com/api/v3/timelines/")
            };
            FifaTimelineEventsProvider provider = new FifaTimelineEventsProvider(
                httpClient,
                Options.Create(new MatchResultSyncOptions()));

            ExternalMatchEvents result = await provider.FetchGoalEventsAsync("1", "2");

            Assert.Single(result.Goals);
            Assert.Null(result.Goals[0].ExternalTeamId);
            Assert.Null(result.Goals[0].ExternalPlayerId);
            Assert.Equal("Nameless SCORER", result.Goals[0].PlayerDisplayName);
        }

        [Fact]
        public async Task FetchGoalEventsAsync_UsesCustomCompetitionSeasonAndLanguage()
        {
            string json = """
                {
                  "IdMatch": "m1",
                  "IdStage": "s1",
                  "Event": []
                }
                """;

            using StubHttpMessageHandler handler = new StubHttpMessageHandler(HttpStatusCode.OK, json);
            using HttpClient httpClient = new HttpClient(handler)
            {
                BaseAddress = new Uri("https://api.fifa.com/api/v3/timelines/")
            };
            MatchResultSyncOptions options = new MatchResultSyncOptions
            {
                IdCompetition = "99",
                IdSeason = "111",
                Language = "es"
            };
            FifaTimelineEventsProvider provider = new FifaTimelineEventsProvider(
                httpClient,
                Options.Create(options));

            await provider.FetchGoalEventsAsync("m1", "s1");

            Assert.Contains("99/111/s1/m1", handler.LastRequestUri);
            Assert.Contains("language=es", handler.LastRequestUri);
        }

        [Theory]
        [InlineData("9'", 9)]
        [InlineData("67'", 67)]
        [InlineData("90'+2'", 92)]
        [InlineData("45'+4'", 49)]
        [InlineData("120'+1'", 121)]
        [InlineData("9", 9)]
        [InlineData("  45'  ", 45)]
        [InlineData("HT", 0)]
        [InlineData("", 0)]
        [InlineData(null, 0)]
        public void NormalizeMinute_ParsesFifaMatchMinuteFormats(string? matchMinute, int expected)
        {
            Assert.Equal(expected, FifaTimelineEventsProvider.NormalizeMinute(matchMinute));
        }

        [Theory]
        [InlineData("Julian QUINONES (Mexico) scores!!", "Julian QUINONES")]
        [InlineData("Raul JIMENEZ (Mexico) scores!!", "Raul JIMENEZ")]
        [InlineData("No parentheses here", "No parentheses here")]
        [InlineData("  Trimmed NAME (Club) scores!!", "Trimmed NAME")]
        [InlineData("   ", null)]
        [InlineData(null, null)]
        public void ExtractPlayerDisplayName_ParsesDescription(string? description, string? expected)
        {
            Assert.Equal(expected, FifaTimelineEventsProvider.ExtractPlayerDisplayName(description));
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
