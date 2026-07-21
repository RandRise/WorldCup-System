using System.Globalization;
using System.Text;
using Data.Entities;
using Data.Repos;
using Microsoft.Extensions.Logging;

namespace Core.Services.MatchSync
{
    /// <summary>
    /// Resolves FIFA timeline scorers onto local squad players (ExternalPlayerId / name / create).
    /// </summary>
    public class TimelinePlayerResolver : ITimelinePlayerResolver
    {
        public const string PlaceholderScorerName = MatchResultSyncService.PlaceholderScorerName;
        public const string ForwardPositionName = "Forward";

        private readonly IRepositoryManager _repository;
        private readonly ILogger<TimelinePlayerResolver> _logger;

        public TimelinePlayerResolver(
            IRepositoryManager repository,
            ILogger<TimelinePlayerResolver> logger)
        {
            _repository = repository;
            _logger = logger;
        }

        public async Task<TimelinePlayerResolveResult> ResolveAsync(
            int teamId,
            string? externalPlayerId,
            string? playerDisplayName,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            string? trimmedExternalId = string.IsNullOrWhiteSpace(externalPlayerId)
                ? null
                : externalPlayerId.Trim();
            string? trimmedDisplayName = string.IsNullOrWhiteSpace(playerDisplayName)
                ? null
                : TruncateName(playerDisplayName.Trim());

            if (trimmedExternalId == null && trimmedDisplayName == null)
            {
                throw new ArgumentException(
                    "Timeline player resolve requires ExternalPlayerId and/or PlayerDisplayName.");
            }

            await _repository.Team.GetByIdAsync(teamId);

            List<Player> squad = LoadEligibleSquad(teamId);

            if (trimmedExternalId != null)
            {
                Player? byExternalId = squad.FirstOrDefault(player =>
                    string.Equals(player.ExternalPlayerId, trimmedExternalId, StringComparison.Ordinal));
                if (byExternalId != null)
                {
                    return new TimelinePlayerResolveResult
                    {
                        Player = byExternalId,
                        MatchMethod = TimelinePlayerMatchMethod.ExternalPlayerId
                    };
                }

                // Global unique id may already exist — reuse same-team row; never steal from another team.
                Player? byGlobalExternalId = _repository.Player
                    .Find(player => player.ExternalPlayerId == trimmedExternalId)
                    .FirstOrDefault();
                if (byGlobalExternalId != null)
                {
                    if (byGlobalExternalId.TeamId != teamId)
                    {
                        throw new InvalidOperationException(
                            $"ExternalPlayerId '{trimmedExternalId}' is already assigned to player {byGlobalExternalId.Id} on team {byGlobalExternalId.TeamId}, not team {teamId}.");
                    }

                    if (string.Equals(byGlobalExternalId.Name, PlaceholderScorerName, StringComparison.Ordinal))
                    {
                        throw new InvalidOperationException(
                            $"ExternalPlayerId '{trimmedExternalId}' is on placeholder player {byGlobalExternalId.Id}; cannot use Tournament Scorer for timeline resolve.");
                    }

                    return new TimelinePlayerResolveResult
                    {
                        Player = byGlobalExternalId,
                        MatchMethod = TimelinePlayerMatchMethod.ExternalPlayerId,
                        Warning = $"Resolved ExternalPlayerId '{trimmedExternalId}' via global lookup on team {teamId}."
                    };
                }
            }

            if (trimmedDisplayName != null)
            {
                List<Player> exactMatches = squad
                    .Where(player =>
                        string.Equals(player.Name, trimmedDisplayName, StringComparison.OrdinalIgnoreCase))
                    .ToList();
                if (exactMatches.Count == 1)
                {
                    return await FinishNameMatchAsync(
                        exactMatches[0],
                        trimmedExternalId,
                        TimelinePlayerMatchMethod.ExactName);
                }

                if (exactMatches.Count > 1)
                {
                    throw new InvalidOperationException(
                        $"Ambiguous exact name match for '{trimmedDisplayName}' on team {teamId} ({exactMatches.Count} players).");
                }

                string normalizedTarget = NormalizePlayerName(trimmedDisplayName);
                if (!string.IsNullOrEmpty(normalizedTarget))
                {
                    List<Player> normalizedMatches = squad
                        .Where(player => NormalizePlayerName(player.Name) == normalizedTarget)
                        .ToList();
                    if (normalizedMatches.Count == 1)
                    {
                        return await FinishNameMatchAsync(
                            normalizedMatches[0],
                            trimmedExternalId,
                            TimelinePlayerMatchMethod.NormalizedName);
                    }

                    if (normalizedMatches.Count > 1)
                    {
                        throw new InvalidOperationException(
                            $"Ambiguous normalized name match for '{trimmedDisplayName}' on team {teamId} ({normalizedMatches.Count} players).");
                    }
                }
            }

            if (trimmedDisplayName == null)
            {
                throw new InvalidOperationException(
                    $"No squad player has ExternalPlayerId '{trimmedExternalId}' on team {teamId}, and no display name was provided to create one.");
            }

            Player created = await CreateNamedPlayerAsync(teamId, trimmedDisplayName, trimmedExternalId);
            _logger.LogInformation(
                "Created player {PlayerId} '{PlayerName}' on team {TeamId} for FIFA ExternalPlayerId {ExternalPlayerId}.",
                created.Id,
                created.Name,
                teamId,
                trimmedExternalId ?? "(none)");

            return new TimelinePlayerResolveResult
            {
                Player = created,
                MatchMethod = TimelinePlayerMatchMethod.Created
            };
        }

        private List<Player> LoadEligibleSquad(int teamId)
        {
            // Exclude import/sync placeholder by name only — jersey 99 may be a real squad player.
            return _repository.Player
                .Find(player =>
                    player.TeamId == teamId
                    && player.Name != PlaceholderScorerName)
                .OrderBy(player => player.Number)
                .ToList();
        }

        private async Task<TimelinePlayerResolveResult> FinishNameMatchAsync(
            Player matched,
            string? trimmedExternalId,
            TimelinePlayerMatchMethod method)
        {
            string? warning = null;
            if (trimmedExternalId != null
                && !string.Equals(matched.ExternalPlayerId, trimmedExternalId, StringComparison.Ordinal))
            {
                if (!string.IsNullOrWhiteSpace(matched.ExternalPlayerId)
                    && !string.Equals(matched.ExternalPlayerId, trimmedExternalId, StringComparison.Ordinal))
                {
                    throw new InvalidOperationException(
                        $"Player {matched.Id} already has ExternalPlayerId '{matched.ExternalPlayerId}'; cannot assign '{trimmedExternalId}'.");
                }

                matched.ExternalPlayerId = trimmedExternalId;
                _repository.Player.Update(matched);
                await _repository.SaveAsync();
                warning = $"Backfilled ExternalPlayerId '{trimmedExternalId}' onto player {matched.Id}.";
            }

            return new TimelinePlayerResolveResult
            {
                Player = matched,
                MatchMethod = method,
                Warning = warning
            };
        }

        private async Task<Player> CreateNamedPlayerAsync(
            int teamId,
            string displayName,
            string? externalPlayerId)
        {
            PlayerPosition? forward = _repository.PlayerPosition
                .Find(position => position.Name == ForwardPositionName)
                .FirstOrDefault();
            if (forward == null)
            {
                throw new InvalidOperationException(
                    $"Player position '{ForwardPositionName}' is not seeded; cannot create timeline scorers.");
            }

            int jerseyNumber = AllocateJerseyNumber(teamId);

            Player created = new Player
            {
                Name = displayName,
                Number = jerseyNumber,
                TeamId = teamId,
                PositionId = forward.Id,
                ExternalPlayerId = externalPlayerId
            };
            _repository.Player.Create(created);
            await _repository.SaveAsync();
            return created;
        }

        private int AllocateJerseyNumber(int teamId)
        {
            HashSet<int> usedNumbers = _repository.Player
                .Find(player => player.TeamId == teamId)
                .Select(player => player.Number)
                .ToHashSet();

            for (int number = 1; number <= 98; number++)
            {
                if (!usedNumbers.Contains(number))
                {
                    return number;
                }
            }

            throw new InvalidOperationException(
                $"Team {teamId} has no free jersey numbers 1–98 for a new timeline scorer.");
        }

        /// <summary>
        /// Uppercase, strip diacritics, keep letters/digits, collapse other runs to single spaces.
        /// </summary>
        public static string NormalizePlayerName(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                return string.Empty;
            }

            string decomposed = name.Normalize(NormalizationForm.FormD);
            StringBuilder builder = new StringBuilder(decomposed.Length);
            foreach (char character in decomposed)
            {
                UnicodeCategory category = CharUnicodeInfo.GetUnicodeCategory(character);
                if (category == UnicodeCategory.NonSpacingMark)
                {
                    continue;
                }

                if (char.IsLetterOrDigit(character))
                {
                    builder.Append(char.ToUpperInvariant(character));
                }
                else if (builder.Length > 0 && builder[^1] != ' ')
                {
                    builder.Append(' ');
                }
            }

            return builder.ToString().Trim();
        }

        private static string TruncateName(string name)
        {
            const int maxLength = 64;
            return name.Length <= maxLength ? name : name[..maxLength];
        }
    }
}
