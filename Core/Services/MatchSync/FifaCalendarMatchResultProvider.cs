using System.Net.Http.Json;
using System.Text.Json.Serialization;
using Core.Options;
using Microsoft.Extensions.Options;

namespace Core.Services.MatchSync
{
    /// <summary>
    /// Fetches FT scores from FIFA's public calendar JSON API (api.fifa.com).
    /// Score-only: does not import cards, stats, or scorer identities.
    /// </summary>
    public class FifaCalendarMatchResultProvider : IExternalMatchResultProvider
    {
        /// <summary>FIFA MatchStatus value for a completed match.</summary>
        private const int FifaMatchStatusPlayed = 0;

        private readonly HttpClient _httpClient;
        private readonly MatchResultSyncOptions _options;

        public FifaCalendarMatchResultProvider(HttpClient httpClient, IOptions<MatchResultSyncOptions> options)
        {
            _httpClient = httpClient;
            _options = options.Value;
        }

        public async Task<ExternalMatchResult> FetchResultAsync(
            string externalMatchId,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(externalMatchId))
            {
                throw new ArgumentException("External match id is required.", nameof(externalMatchId));
            }

            string requestUri =
                $"?idCompetition={Uri.EscapeDataString(_options.IdCompetition)}" +
                $"&idSeason={Uri.EscapeDataString(_options.IdSeason)}" +
                $"&idMatch={Uri.EscapeDataString(externalMatchId.Trim())}" +
                $"&language={Uri.EscapeDataString(_options.Language)}" +
                "&count=1";

            using HttpResponseMessage response = await _httpClient.GetAsync(requestUri, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                throw new InvalidOperationException(
                    $"FIFA calendar request failed with status {(int)response.StatusCode} ({response.ReasonPhrase}).");
            }

            FifaCalendarResponse? payload = await response.Content.ReadFromJsonAsync<FifaCalendarResponse>(
                cancellationToken: cancellationToken);
            FifaCalendarMatch? match = payload?.Results?
                .FirstOrDefault(result =>
                    string.Equals(result.IdMatch, externalMatchId.Trim(), StringComparison.OrdinalIgnoreCase));

            if (match == null)
            {
                throw new InvalidOperationException(
                    $"FIFA calendar returned no match for external id '{externalMatchId}'.");
            }

            if (!match.MatchStatus.HasValue)
            {
                throw new InvalidOperationException(
                    $"FIFA calendar match '{externalMatchId}' did not include MatchStatus; refusing to treat as finished.");
            }

            int homeScore = match.HomeTeamScore ?? match.Home?.Score ?? 0;
            int awayScore = match.AwayTeamScore ?? match.Away?.Score ?? 0;
            bool isFinished = match.MatchStatus.Value == FifaMatchStatusPlayed;

            return new ExternalMatchResult
            {
                ExternalMatchId = match.IdMatch ?? externalMatchId.Trim(),
                HomeScore = homeScore,
                AwayScore = awayScore,
                IsFinished = isFinished,
                StatusLabel = $"MatchStatus={match.MatchStatus.Value}",
                HomeTeamName = FirstLocalizedName(match.Home?.TeamName) ?? match.Home?.ShortClubName,
                AwayTeamName = FirstLocalizedName(match.Away?.TeamName) ?? match.Away?.ShortClubName,
                HomeTeamCountryCode = match.Home?.IdCountry,
                AwayTeamCountryCode = match.Away?.IdCountry
            };
        }

        private static string? FirstLocalizedName(List<FifaLocalizedName>? names)
        {
            return names?.FirstOrDefault()?.Description;
        }

        private sealed class FifaCalendarResponse
        {
            [JsonPropertyName("Results")]
            public List<FifaCalendarMatch>? Results { get; set; }
        }

        private sealed class FifaCalendarMatch
        {
            [JsonPropertyName("IdMatch")]
            public string? IdMatch { get; set; }

            [JsonPropertyName("MatchStatus")]
            public int? MatchStatus { get; set; }

            [JsonPropertyName("HomeTeamScore")]
            public int? HomeTeamScore { get; set; }

            [JsonPropertyName("AwayTeamScore")]
            public int? AwayTeamScore { get; set; }

            [JsonPropertyName("Home")]
            public FifaTeamSide? Home { get; set; }

            [JsonPropertyName("Away")]
            public FifaTeamSide? Away { get; set; }
        }

        private sealed class FifaTeamSide
        {
            [JsonPropertyName("Score")]
            public int? Score { get; set; }

            [JsonPropertyName("ShortClubName")]
            public string? ShortClubName { get; set; }

            [JsonPropertyName("IdCountry")]
            public string? IdCountry { get; set; }

            [JsonPropertyName("TeamName")]
            public List<FifaLocalizedName>? TeamName { get; set; }
        }

        private sealed class FifaLocalizedName
        {
            [JsonPropertyName("Description")]
            public string? Description { get; set; }
        }
    }
}
