using System.Globalization;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using Core.Options;
using Microsoft.Extensions.Options;

namespace Core.Services.MatchSync
{
    /// <summary>
    /// Fetches Goal! / Own Goal / Penalty Goal events from FIFA's public timeline API.
    /// Does not apply scorers to the DB — that is Phase 11 Tasks 3–5.
    /// </summary>
    public class FifaTimelineEventsProvider : IExternalMatchEventsProvider
    {
        private static readonly HashSet<string> GoalTypeLabels = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "Goal!",
            "Own Goal",
            "Own Goal!",
            "Penalty Goal",
            "Penalty Goal!",
            "Penalty!"
        };

        private static readonly Regex PlayerNameFromDescription = new Regex(
            @"^\s*(.+?)\s*\(",
            RegexOptions.Compiled | RegexOptions.CultureInvariant);

        private static readonly Regex MinuteWithStoppage = new Regex(
            @"^\s*(\d+)\s*'\s*\+\s*(\d+)\s*'\s*$",
            RegexOptions.Compiled | RegexOptions.CultureInvariant);

        private static readonly Regex MinuteSimple = new Regex(
            @"^\s*(\d+)\s*'\s*$",
            RegexOptions.Compiled | RegexOptions.CultureInvariant);

        private readonly HttpClient _httpClient;
        private readonly MatchResultSyncOptions _options;

        public FifaTimelineEventsProvider(HttpClient httpClient, IOptions<MatchResultSyncOptions> options)
        {
            _httpClient = httpClient;
            _options = options.Value;
        }

        public async Task<ExternalMatchEvents> FetchGoalEventsAsync(
            string externalMatchId,
            string externalStageId,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(externalMatchId))
            {
                throw new ArgumentException("External match id is required.", nameof(externalMatchId));
            }

            if (string.IsNullOrWhiteSpace(externalStageId))
            {
                throw new ArgumentException("External stage id is required.", nameof(externalStageId));
            }

            string trimmedMatchId = externalMatchId.Trim();
            string trimmedStageId = externalStageId.Trim();

            string requestUri =
                $"{Uri.EscapeDataString(_options.IdCompetition)}/" +
                $"{Uri.EscapeDataString(_options.IdSeason)}/" +
                $"{Uri.EscapeDataString(trimmedStageId)}/" +
                $"{Uri.EscapeDataString(trimmedMatchId)}" +
                $"?language={Uri.EscapeDataString(_options.Language)}";

            using HttpResponseMessage response = await _httpClient.GetAsync(requestUri, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                throw new InvalidOperationException(
                    $"FIFA timeline request failed with status {(int)response.StatusCode} ({response.ReasonPhrase}).");
            }

            FifaTimelineResponse? payload = await response.Content.ReadFromJsonAsync<FifaTimelineResponse>(
                cancellationToken: cancellationToken);

            if (payload == null)
            {
                throw new InvalidOperationException(
                    $"FIFA timeline returned an empty payload for external id '{trimmedMatchId}'.");
            }

            if (payload.Event == null)
            {
                throw new InvalidOperationException(
                    $"FIFA timeline for external id '{trimmedMatchId}' did not include an Event array.");
            }

            if (!string.IsNullOrWhiteSpace(payload.IdMatch)
                && !string.Equals(payload.IdMatch.Trim(), trimmedMatchId, StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException(
                    $"FIFA timeline IdMatch '{payload.IdMatch}' does not match requested '{trimmedMatchId}'.");
            }

            List<ExternalMatchGoalEvent> goals = ParseGoalEvents(payload, trimmedMatchId);

            return new ExternalMatchEvents
            {
                ExternalMatchId = string.IsNullOrWhiteSpace(payload.IdMatch) ? trimmedMatchId : payload.IdMatch.Trim(),
                ExternalStageId = string.IsNullOrWhiteSpace(payload.IdStage) ? trimmedStageId : payload.IdStage.Trim(),
                Goals = goals
            };
        }

        internal static List<ExternalMatchGoalEvent> ParseGoalEvents(FifaTimelineResponse payload, string fallbackMatchId)
        {
            List<ExternalMatchGoalEvent> goals = new List<ExternalMatchGoalEvent>();
            if (payload.Event == null || payload.Event.Count == 0)
            {
                return goals;
            }

            string matchId = string.IsNullOrWhiteSpace(payload.IdMatch) ? fallbackMatchId : payload.IdMatch.Trim();
            int previousHomeGoals = 0;
            int previousAwayGoals = 0;

            foreach (FifaTimelineEvent timelineEvent in payload.Event)
            {
                int homeGoals = timelineEvent.HomeGoals ?? previousHomeGoals;
                int awayGoals = timelineEvent.AwayGoals ?? previousAwayGoals;

                string? typeLabel = FirstLocalizedDescription(timelineEvent.TypeLocalized);
                bool homeIncreased = homeGoals > previousHomeGoals;
                bool awayIncreased = awayGoals > previousAwayGoals;
                bool scoreIncreased = homeIncreased || awayIncreased;

                // Only count Goal! (etc.) when FT score actually moved — skips duplicate/VAR-overturned lines.
                if (IsGoalType(typeLabel) && scoreIncreased)
                {
                    bool isHomeSide = ResolveIsHomeSide(homeIncreased, awayIncreased, typeLabel);
                    bool isOwnGoal = IsOwnGoalType(typeLabel);
                    bool isPenalty = IsPenaltyType(typeLabel);
                    string? description = FirstLocalizedDescription(timelineEvent.EventDescription);

                    goals.Add(new ExternalMatchGoalEvent
                    {
                        ExternalMatchId = matchId,
                        ExternalTeamId = string.IsNullOrWhiteSpace(timelineEvent.IdTeam)
                            ? null
                            : timelineEvent.IdTeam.Trim(),
                        ExternalPlayerId = string.IsNullOrWhiteSpace(timelineEvent.IdPlayer)
                            ? null
                            : timelineEvent.IdPlayer.Trim(),
                        PlayerDisplayName = ExtractPlayerDisplayName(description),
                        Minute = NormalizeMinute(timelineEvent.MatchMinute),
                        IsHomeSide = isHomeSide,
                        IsOwnGoal = isOwnGoal,
                        IsPenalty = isPenalty,
                        EventTypeLabel = typeLabel
                    });
                }

                previousHomeGoals = homeGoals;
                previousAwayGoals = awayGoals;
            }

            return goals;
        }

        public static int NormalizeMinute(string? matchMinute)
        {
            if (string.IsNullOrWhiteSpace(matchMinute))
            {
                return 0;
            }

            string trimmed = matchMinute.Trim();
            Match stoppage = MinuteWithStoppage.Match(trimmed);
            if (stoppage.Success)
            {
                int baseMinute = int.Parse(stoppage.Groups[1].Value, CultureInfo.InvariantCulture);
                int extra = int.Parse(stoppage.Groups[2].Value, CultureInfo.InvariantCulture);
                return baseMinute + extra;
            }

            Match simple = MinuteSimple.Match(trimmed);
            if (simple.Success)
            {
                return int.Parse(simple.Groups[1].Value, CultureInfo.InvariantCulture);
            }

            // Fallback: leading digits only (e.g. "9" without apostrophe).
            string digits = new string(trimmed.TakeWhile(char.IsDigit).ToArray());
            if (digits.Length > 0
                && int.TryParse(digits, NumberStyles.Integer, CultureInfo.InvariantCulture, out int parsed))
            {
                return parsed;
            }

            return 0;
        }

        public static string? ExtractPlayerDisplayName(string? eventDescription)
        {
            if (string.IsNullOrWhiteSpace(eventDescription))
            {
                return null;
            }

            Match match = PlayerNameFromDescription.Match(eventDescription);
            if (match.Success)
            {
                string name = match.Groups[1].Value.Trim();
                return string.IsNullOrWhiteSpace(name) ? null : name;
            }

            return eventDescription.Trim();
        }

        private static bool ResolveIsHomeSide(bool homeIncreased, bool awayIncreased, string? typeLabel)
        {
            if (homeIncreased && !awayIncreased)
            {
                return true;
            }

            if (awayIncreased && !homeIncreased)
            {
                return false;
            }

            // Ambiguous score delta (or missing HomeGoals/AwayGoals): default home for regular goals.
            // Own goals without a clear delta still need a side; prefer away so we do not invent home credit.
            return !IsOwnGoalType(typeLabel);
        }

        private static bool IsGoalType(string? typeLabel)
        {
            return !string.IsNullOrWhiteSpace(typeLabel) && GoalTypeLabels.Contains(typeLabel.Trim());
        }

        private static bool IsOwnGoalType(string? typeLabel)
        {
            if (string.IsNullOrWhiteSpace(typeLabel))
            {
                return false;
            }

            string normalized = typeLabel.Trim();
            return normalized.Equals("Own Goal", StringComparison.OrdinalIgnoreCase)
                || normalized.Equals("Own Goal!", StringComparison.OrdinalIgnoreCase);
        }

        private static bool IsPenaltyType(string? typeLabel)
        {
            if (string.IsNullOrWhiteSpace(typeLabel))
            {
                return false;
            }

            string normalized = typeLabel.Trim();
            return normalized.Equals("Penalty Goal", StringComparison.OrdinalIgnoreCase)
                || normalized.Equals("Penalty Goal!", StringComparison.OrdinalIgnoreCase)
                || normalized.Equals("Penalty!", StringComparison.OrdinalIgnoreCase);
        }

        private static string? FirstLocalizedDescription(List<FifaLocalizedName>? names)
        {
            return names?.FirstOrDefault()?.Description;
        }

        /// <summary>Deserialization shape for FIFA timeline JSON (subset used for goals).</summary>
        internal sealed class FifaTimelineResponse
        {
            [JsonPropertyName("IdMatch")]
            public string? IdMatch { get; set; }

            [JsonPropertyName("IdStage")]
            public string? IdStage { get; set; }

            [JsonPropertyName("Event")]
            public List<FifaTimelineEvent>? Event { get; set; }
        }

        internal sealed class FifaTimelineEvent
        {
            [JsonPropertyName("IdTeam")]
            public string? IdTeam { get; set; }

            [JsonPropertyName("IdPlayer")]
            public string? IdPlayer { get; set; }

            [JsonPropertyName("MatchMinute")]
            public string? MatchMinute { get; set; }

            [JsonPropertyName("HomeGoals")]
            public int? HomeGoals { get; set; }

            [JsonPropertyName("AwayGoals")]
            public int? AwayGoals { get; set; }

            [JsonPropertyName("TypeLocalized")]
            public List<FifaLocalizedName>? TypeLocalized { get; set; }

            [JsonPropertyName("EventDescription")]
            public List<FifaLocalizedName>? EventDescription { get; set; }
        }

        internal sealed class FifaLocalizedName
        {
            [JsonPropertyName("Description")]
            public string? Description { get; set; }
        }
    }
}
