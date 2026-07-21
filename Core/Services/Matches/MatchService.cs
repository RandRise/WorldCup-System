using Core.DTOs.Matches;
using Core.Services.Bets;
using Core.Services.MatchSync;
using Data.Entities;
using Data.Repos;

namespace Core.Services.Matches
{
    public class MatchService : IMatchService
    {
        private readonly IRepositoryManager _repository;
        private readonly IBetService _betService;

        public MatchService(IRepositoryManager repository, IBetService betService)
        {
            _repository = repository;
            _betService = betService;
        }

        public List<MatchDTO> GetMatches()
        {
            List<Match> matches = _repository.Match.GetAllAsync().ToList();
            return BuildMatchDtos(matches);
        }

        public async Task<MatchDetailDTO> GetMatchById(int id)
        {
            Match match = await _repository.Match.GetByIdAsync(id);

            List<Team> teams = _repository.Team.GetAllAsync().ToList();
            List<Country> countries = _repository.Country.GetAllAsync().ToList();
            List<Player> players = _repository.Player.GetAllAsync().ToList();
            Stadium stadium = await _repository.Stadium.GetByIdAsync(match.StadiumId);

            List<TeamStats> stats = _repository.TeamStats.Find(teamStats => teamStats.MatchId == id).ToList();
            List<int> statIds = stats.Select(teamStats => teamStats.Id).ToList();
            List<Goal> goals = statIds.Count > 0
                ? _repository.Goal.Find(goal => statIds.Contains(goal.TeamStatsId)).ToList()
                : new List<Goal>();
            List<Card> cards = statIds.Count > 0
                ? _repository.Card.Find(card => statIds.Contains(card.TeamStatsId)).ToList()
                : new List<Card>();

            TeamStats? teamOneStats = match.TeamOneId.HasValue
                ? stats.FirstOrDefault(teamStats => teamStats.TeamId == match.TeamOneId.Value)
                : null;
            TeamStats? teamTwoStats = match.TeamTwoId.HasValue
                ? stats.FirstOrDefault(teamStats => teamStats.TeamId == match.TeamTwoId.Value)
                : null;
            (string status, bool canBet) = ResolveMatchStatus(match);

            return new MatchDetailDTO
            {
                Id = match.Id,
                Date = match.Date,
                Stage = match.Stage,
                StageName = FormatStageName(match.Stage),
                StadiumId = match.StadiumId,
                StadiumName = stadium.Name,
                TeamOneId = match.TeamOneId,
                TeamOneName = match.TeamOneId.HasValue
                    ? ResolveTeamName(match.TeamOneId.Value, teams, countries)
                    : "TBD",
                TeamTwoId = match.TeamTwoId,
                TeamTwoName = match.TeamTwoId.HasValue
                    ? ResolveTeamName(match.TeamTwoId.Value, teams, countries)
                    : "TBD",
                FeederMatchOneId = match.FeederMatchOneId,
                FeederMatchTwoId = match.FeederMatchTwoId,
                TeamOneScore = teamOneStats == null ? 0 : goals.Count(goal => goal.TeamStatsId == teamOneStats.Id),
                TeamTwoScore = teamTwoStats == null ? 0 : goals.Count(goal => goal.TeamStatsId == teamTwoStats.Id),
                Status = status,
                CanBet = canBet,
                ExternalMatchId = match.ExternalMatchId,
                ExternalStageId = match.ExternalStageId,
                TeamOneStats = BuildTeamStatsDto(teamOneStats, match, teams, countries, players, goals, cards),
                TeamTwoStats = BuildTeamStatsDto(teamTwoStats, match, teams, countries, players, goals, cards)
            };
        }

        public async Task AddMatch(AddMatchDTO matchDto)
        {
            if (matchDto.TeamOneId == matchDto.TeamTwoId)
            {
                throw new InvalidOperationException("A match must be between two different teams.");
            }

            if (!Enum.IsDefined(typeof(MatchStage), matchDto.Stage))
            {
                throw new InvalidOperationException("Stage must be a valid match stage.");
            }

            Team teamOne = await _repository.Team.GetByIdAsync(matchDto.TeamOneId);
            Team teamTwo = await _repository.Team.GetByIdAsync(matchDto.TeamTwoId);
            await _repository.Stadium.GetByIdAsync(matchDto.StadiumId);
            await EnsureTeamsEligibleForStage(teamOne, teamTwo, matchDto.Stage);

            Match match = new Match
            {
                Date = ToUtc(matchDto.Date),
                Stage = matchDto.Stage,
                StadiumId = matchDto.StadiumId,
                TeamOneId = matchDto.TeamOneId,
                TeamTwoId = matchDto.TeamTwoId,
                TeamStats = new List<TeamStats>
                {
                    new TeamStats { TeamId = matchDto.TeamOneId },
                    new TeamStats { TeamId = matchDto.TeamTwoId }
                }
            };

            _repository.Match.Create(match);
            await _repository.SaveAsync();
        }

        public async Task UpdateMatch(UpdateMatchDTO matchDto)
        {
            if (!Enum.IsDefined(typeof(MatchStage), matchDto.Stage))
            {
                throw new InvalidOperationException("Stage must be a valid match stage.");
            }

            bool bothTeamsSet = matchDto.TeamOneId.HasValue && matchDto.TeamTwoId.HasValue;
            bool bothTeamsTbd = !matchDto.TeamOneId.HasValue && !matchDto.TeamTwoId.HasValue;
            if (!bothTeamsSet && !bothTeamsTbd)
            {
                throw new InvalidOperationException(
                    "Set both teams, or leave both empty for a knockout TBD slot.");
            }

            if (bothTeamsSet && matchDto.TeamOneId == matchDto.TeamTwoId)
            {
                throw new InvalidOperationException("A match must be between two different teams.");
            }

            if (matchDto.Stage == MatchStage.Group && !bothTeamsSet)
            {
                throw new InvalidOperationException("Group stage matches require both teams.");
            }

            Match match = await _repository.Match.GetByIdAsync(matchDto.Id);
            await _repository.Stadium.GetByIdAsync(matchDto.StadiumId);

            List<TeamStats> stats = _repository.TeamStats.Find(teamStats => teamStats.MatchId == match.Id).ToList();
            bool hasRecordedEvents = MatchHasRecordedEvents(stats);

            bool stageChanged = match.Stage != matchDto.Stage;
            if (stageChanged && hasRecordedEvents)
            {
                throw new InvalidOperationException(
                    "Cannot change the match stage after goals or cards have been recorded.");
            }

            bool teamsChanged = match.TeamOneId != matchDto.TeamOneId || match.TeamTwoId != matchDto.TeamTwoId;
            if (teamsChanged)
            {
                if (hasRecordedEvents)
                {
                    throw new InvalidOperationException(
                        "Cannot change the participating teams after goals or cards have been recorded.");
                }

                if (bothTeamsSet)
                {
                    TeamStats? teamOneStats = stats.FirstOrDefault(teamStats => teamStats.TeamId == match.TeamOneId);
                    if (teamOneStats != null)
                    {
                        teamOneStats.TeamId = matchDto.TeamOneId!.Value;
                        _repository.TeamStats.Update(teamOneStats);
                    }

                    TeamStats? teamTwoStats = stats.FirstOrDefault(teamStats => teamStats.TeamId == match.TeamTwoId);
                    if (teamTwoStats != null)
                    {
                        teamTwoStats.TeamId = matchDto.TeamTwoId!.Value;
                        _repository.TeamStats.Update(teamTwoStats);
                    }
                }

                match.TeamOneId = matchDto.TeamOneId;
                match.TeamTwoId = matchDto.TeamTwoId;
            }

            DateTime kickoffUtc = ToUtc(matchDto.Date);
            bool dateChanged = match.Date != kickoffUtc;
            if (dateChanged && hasRecordedEvents)
            {
                throw new InvalidOperationException(
                    "Cannot reschedule a match after goals or cards have been recorded.");
            }

            if (bothTeamsSet)
            {
                Team teamOne = await _repository.Team.GetByIdAsync(matchDto.TeamOneId!.Value);
                Team teamTwo = await _repository.Team.GetByIdAsync(matchDto.TeamTwoId!.Value);
                await EnsureTeamsEligibleForStage(teamOne, teamTwo, matchDto.Stage);
            }

            match.Date = kickoffUtc;
            match.Stage = matchDto.Stage;
            match.StadiumId = matchDto.StadiumId;

            _repository.Match.Update(match);
            await _repository.SaveAsync();
        }

        public async Task DeleteMatch(int id)
        {
            Match match = await _repository.Match.GetByIdAsync(id);

            List<TeamStats> stats = _repository.TeamStats.Find(teamStats => teamStats.MatchId == id).ToList();
            List<int> statIds = stats.Select(teamStats => teamStats.Id).ToList();
            bool hasGoals = statIds.Count > 0 && _repository.Goal.Find(goal => statIds.Contains(goal.TeamStatsId)).Any();
            bool hasCards = statIds.Count > 0 && _repository.Card.Find(card => statIds.Contains(card.TeamStatsId)).Any();
            bool hasBets = _repository.Bet.Find(bet => bet.MatchId == id).Any();
            if (hasGoals || hasCards)
            {
                throw new InvalidOperationException(
                    "Cannot delete a match while goals or cards are recorded. Remove them first.");
            }

            if (hasBets)
            {
                throw new InvalidOperationException(
                    "Cannot delete a match while bets are placed on it. Remove bets first.");
            }

            foreach (TeamStats teamStats in stats)
            {
                _repository.TeamStats.Delete(teamStats);
            }

            _repository.Match.Delete(match);
            await _repository.SaveAsync();
        }

        public List<MatchDTO> GetFixturesByWorldCup(int worldCupId)
        {
            List<int> groupIds = _repository.Group
                .Find(group => group.WorldCupId == worldCupId)
                .Select(group => group.Id)
                .ToList();
            if (groupIds.Count == 0)
            {
                return new List<MatchDTO>();
            }

            List<int> teamIds = _repository.Team
                .Find(team => groupIds.Contains(team.GroupId))
                .Select(team => team.Id)
                .ToList();
            if (teamIds.Count == 0)
            {
                return new List<MatchDTO>();
            }

            List<Match> allMatches = _repository.Match.GetAllAsync().ToList();
            HashSet<int> matchIds = allMatches
                .Where(match =>
                    match.TeamOneId.HasValue
                    && match.TeamTwoId.HasValue
                    && teamIds.Contains(match.TeamOneId.Value)
                    && teamIds.Contains(match.TeamTwoId.Value))
                .Select(match => match.Id)
                .ToHashSet();

            bool expanded;
            do
            {
                expanded = false;
                foreach (Match match in allMatches)
                {
                    if (matchIds.Contains(match.Id))
                    {
                        continue;
                    }

                    bool linkedByFeeder =
                        (match.FeederMatchOneId.HasValue && matchIds.Contains(match.FeederMatchOneId.Value))
                        || (match.FeederMatchTwoId.HasValue && matchIds.Contains(match.FeederMatchTwoId.Value));
                    if (linkedByFeeder)
                    {
                        matchIds.Add(match.Id);
                        expanded = true;
                    }
                }
            }
            while (expanded);

            List<Match> matches = allMatches
                .Where(match => matchIds.Contains(match.Id))
                .OrderBy(match => match.Date)
                .ToList();

            return BuildMatchDtos(matches);
        }

        public List<MatchDTO> GetFixturesByGroup(int groupId)
        {
            List<int> teamIds = _repository.Team
                .Find(team => team.GroupId == groupId)
                .Select(team => team.Id)
                .ToList();
            if (teamIds.Count == 0)
            {
                return new List<MatchDTO>();
            }

            List<Match> matches = _repository.Match
                .Find(match =>
                    match.Stage == MatchStage.Group
                    && match.TeamOneId.HasValue
                    && match.TeamTwoId.HasValue
                    && teamIds.Contains(match.TeamOneId.Value)
                    && teamIds.Contains(match.TeamTwoId.Value))
                .OrderBy(match => match.Date)
                .ToList();

            return BuildMatchDtos(matches);
        }

        public List<MatchDTO> GetFixturesByDate(DateTime date)
        {
            DateTime start = date.Date;
            DateTime end = start.AddDays(1);

            List<Match> matches = _repository.Match
                .Find(match => match.Date >= start && match.Date < end)
                .OrderBy(match => match.Date)
                .ToList();

            return BuildMatchDtos(matches);
        }

        public async Task<LiveSnapshotDTO> GetLiveSnapshot(int worldCupId)
        {
            List<MatchDTO> fixtures = GetFixturesByWorldCup(worldCupId);
            List<LiveMatchSnapshotDTO> matchSnapshots = new List<LiveMatchSnapshotDTO>();
            List<LiveEventSnapshotDTO> recentEvents = new List<LiveEventSnapshotDTO>();

            if (fixtures.Count == 0)
            {
                return new LiveSnapshotDTO
                {
                    Matches = matchSnapshots,
                    RecentEvents = recentEvents
                };
            }

            List<int> matchIds = fixtures.Select(match => match.Id).ToList();
            List<Match> matches = _repository.Match
                .Find(match => matchIds.Contains(match.Id))
                .ToList();
            List<Team> teams = _repository.Team.GetAllAsync().ToList();
            List<Country> countries = _repository.Country.GetAllAsync().ToList();
            List<Player> players = _repository.Player.GetAllAsync().ToList();
            List<TeamStats> stats = _repository.TeamStats
                .Find(teamStats => matchIds.Contains(teamStats.MatchId))
                .ToList();
            List<int> statIds = stats.Select(teamStats => teamStats.Id).ToList();
            List<Goal> goals = statIds.Count > 0
                ? _repository.Goal.Find(goal => statIds.Contains(goal.TeamStatsId)).ToList()
                : new List<Goal>();
            List<Card> cards = statIds.Count > 0
                ? _repository.Card.Find(card => statIds.Contains(card.TeamStatsId)).ToList()
                : new List<Card>();

            DateTime now = DateTime.UtcNow;
            foreach (MatchDTO fixture in fixtures)
            {
                Match? match = matches.FirstOrDefault(existingMatch => existingMatch.Id == fixture.Id);
                if (match == null)
                {
                    continue;
                }

                if (fixture.Status == "Finished")
                {
                    await _betService.TryAutoResolveFinishedMatch(fixture.Id);
                }

                int? currentMinute = null;
                if (fixture.Status == "Live")
                {
                    currentMinute = ToMinute(now, match.Date);
                }

                matchSnapshots.Add(new LiveMatchSnapshotDTO
                {
                    MatchId = fixture.Id,
                    Status = fixture.Status,
                    TeamOneScore = fixture.TeamOneScore,
                    TeamTwoScore = fixture.TeamTwoScore,
                    CurrentMinute = currentMinute
                });
            }

            foreach (Goal goal in goals)
            {
                TeamStats? owningStats = stats.FirstOrDefault(teamStats => teamStats.Id == goal.TeamStatsId);
                Match? match = matches.FirstOrDefault(existingMatch => existingMatch.Id == owningStats?.MatchId);
                if (match == null || owningStats == null)
                {
                    continue;
                }

                recentEvents.Add(new LiveEventSnapshotDTO
                {
                    MatchId = match.Id,
                    EventType = goal.IsOwnGoal != 0 ? "OwnGoal" : "Goal",
                    Minute = ToMinute(goal.TimeScored, match.Date),
                    PlayerName = ToHonestPlayerName(
                        players.FirstOrDefault(player => player.Id == goal.PlayerId)?.Name),
                    TeamName = ResolveTeamName(owningStats.TeamId, teams, countries)
                });
            }

            foreach (Card card in cards)
            {
                TeamStats? owningStats = stats.FirstOrDefault(teamStats => teamStats.Id == card.TeamStatsId);
                Match? match = matches.FirstOrDefault(existingMatch => existingMatch.Id == owningStats?.MatchId);
                if (match == null || owningStats == null)
                {
                    continue;
                }

                recentEvents.Add(new LiveEventSnapshotDTO
                {
                    MatchId = match.Id,
                    EventType = CardTypeName(card.Type),
                    Minute = ToMinute(card.TimeIssued, match.Date),
                    PlayerName = ToHonestPlayerName(
                        players.FirstOrDefault(player => player.Id == card.PlayerId)?.Name),
                    TeamName = ResolveTeamName(owningStats.TeamId, teams, countries)
                });
            }

            recentEvents = recentEvents
                .OrderByDescending(evt => evt.Minute)
                .Take(30)
                .ToList();

            return new LiveSnapshotDTO
            {
                Matches = matchSnapshots,
                RecentEvents = recentEvents
            };
        }

        private List<MatchDTO> BuildMatchDtos(List<Match> matches)
        {
            if (matches.Count == 0)
            {
                return new List<MatchDTO>();
            }

            List<int> matchIds = matches.Select(match => match.Id).ToList();
            List<Team> teams = _repository.Team.GetAllAsync().ToList();
            List<Country> countries = _repository.Country.GetAllAsync().ToList();
            List<Stadium> stadiums = _repository.Stadium.GetAllAsync().ToList();
            List<TeamStats> stats = _repository.TeamStats.Find(teamStats => matchIds.Contains(teamStats.MatchId)).ToList();
            List<int> statIds = stats.Select(teamStats => teamStats.Id).ToList();
            List<Goal> goals = statIds.Count > 0
                ? _repository.Goal.Find(goal => statIds.Contains(goal.TeamStatsId)).ToList()
                : new List<Goal>();

            return matches.Select(match =>
            {
                TeamStats? teamOneStats = match.TeamOneId.HasValue
                    ? stats.FirstOrDefault(teamStats =>
                        teamStats.MatchId == match.Id && teamStats.TeamId == match.TeamOneId.Value)
                    : null;
                TeamStats? teamTwoStats = match.TeamTwoId.HasValue
                    ? stats.FirstOrDefault(teamStats =>
                        teamStats.MatchId == match.Id && teamStats.TeamId == match.TeamTwoId.Value)
                    : null;

                (string status, bool canBet) = ResolveMatchStatus(match);

                return new MatchDTO
                {
                    Id = match.Id,
                    Date = match.Date,
                    Stage = match.Stage,
                    StageName = FormatStageName(match.Stage),
                    StadiumId = match.StadiumId,
                    StadiumName = stadiums.FirstOrDefault(stadium => stadium.Id == match.StadiumId)?.Name,
                    TeamOneId = match.TeamOneId,
                    TeamOneName = match.TeamOneId.HasValue
                        ? ResolveTeamName(match.TeamOneId.Value, teams, countries)
                        : "TBD",
                    TeamTwoId = match.TeamTwoId,
                    TeamTwoName = match.TeamTwoId.HasValue
                        ? ResolveTeamName(match.TeamTwoId.Value, teams, countries)
                        : "TBD",
                    FeederMatchOneId = match.FeederMatchOneId,
                    FeederMatchTwoId = match.FeederMatchTwoId,
                    TeamOneScore = teamOneStats == null ? 0 : goals.Count(goal => goal.TeamStatsId == teamOneStats.Id),
                    TeamTwoScore = teamTwoStats == null ? 0 : goals.Count(goal => goal.TeamStatsId == teamTwoStats.Id),
                    Status = status,
                    CanBet = canBet,
                    ExternalMatchId = match.ExternalMatchId,
                    ExternalStageId = match.ExternalStageId
                };
            }).ToList();
        }

        private async Task EnsureTeamsEligibleForStage(Team teamOne, Team teamTwo, MatchStage stage)
        {
            if (stage == MatchStage.Group)
            {
                if (teamOne.GroupId != teamTwo.GroupId)
                {
                    throw new InvalidOperationException(
                        "Group stage matches must be between teams in the same group.");
                }

                return;
            }

            Group groupOne = await _repository.Group.GetByIdAsync(teamOne.GroupId);
            Group groupTwo = await _repository.Group.GetByIdAsync(teamTwo.GroupId);
            if (groupOne.WorldCupId != groupTwo.WorldCupId)
            {
                throw new InvalidOperationException(
                    "Knockout matches must be between teams in the same World Cup.");
            }
        }

        internal static string FormatStageName(MatchStage stage)
        {
            return stage switch
            {
                MatchStage.Group => "Group",
                MatchStage.RoundOf32 => "Round of 32",
                MatchStage.RoundOf16 => "Round of 16",
                MatchStage.QuarterFinal => "Quarter-final",
                MatchStage.SemiFinal => "Semi-final",
                MatchStage.ThirdPlace => "Third place",
                MatchStage.Final => "Final",
                _ => stage.ToString()
            };
        }

        private static MatchTeamStatsDTO? BuildTeamStatsDto(
            TeamStats? teamStats,
            Match match,
            List<Team> teams,
            List<Country> countries,
            List<Player> players,
            List<Goal> goals,
            List<Card> cards)
        {
            if (teamStats == null)
            {
                return null;
            }

            List<MatchGoalDTO> goalDtos = goals
                .Where(goal => goal.TeamStatsId == teamStats.Id)
                .Select(goal => new MatchGoalDTO
                {
                    Id = goal.Id,
                    PlayerId = goal.PlayerId,
                    PlayerName = ToHonestPlayerName(
                        players.FirstOrDefault(player => player.Id == goal.PlayerId)?.Name),
                    Minute = ToMinute(goal.TimeScored, match.Date),
                    IsOwnGoal = goal.IsOwnGoal != 0
                })
                .OrderBy(goal => goal.Minute)
                .ToList();

            List<MatchCardDTO> cardDtos = cards
                .Where(card => card.TeamStatsId == teamStats.Id)
                .Select(card => new MatchCardDTO
                {
                    Id = card.Id,
                    PlayerId = card.PlayerId,
                    PlayerName = ToHonestPlayerName(
                        players.FirstOrDefault(player => player.Id == card.PlayerId)?.Name),
                    Minute = ToMinute(card.TimeIssued, match.Date),
                    Type = CardTypeName(card.Type)
                })
                .OrderBy(card => card.Minute)
                .ToList();

            return new MatchTeamStatsDTO
            {
                TeamStatsId = teamStats.Id,
                TeamId = teamStats.TeamId,
                TeamName = ResolveTeamName(teamStats.TeamId, teams, countries),
                Possession = teamStats.Possession,
                Shots = teamStats.Shots,
                ShotsOnTarget = teamStats.ShotsOnTarget,
                Score = goalDtos.Count,
                Goals = goalDtos,
                Cards = cardDtos
            };
        }

        /// <summary>
        /// Omits import/sync placeholder names so Recent events and match timelines
        /// never present "Tournament Scorer" as a real player (Phase 11 Task 7).
        /// </summary>
        private static string? ToHonestPlayerName(string? playerName)
        {
            if (string.IsNullOrWhiteSpace(playerName))
            {
                return null;
            }

            if (string.Equals(
                    playerName.Trim(),
                    MatchResultSyncService.PlaceholderScorerName,
                    StringComparison.Ordinal))
            {
                return null;
            }

            return playerName;
        }

        private static string? ResolveTeamName(int teamId, List<Team> teams, List<Country> countries)
        {
            Team? team = teams.FirstOrDefault(existingTeam => existingTeam.Id == teamId);
            if (team == null)
            {
                return null;
            }

            return countries.FirstOrDefault(country => country.Id == team.CountryId)?.Name;
        }

        private static int ToMinute(DateTime eventTime, DateTime kickoff)
        {
            int minute = (int)Math.Floor((eventTime - kickoff).TotalMinutes);
            return minute < 0 ? 0 : minute;
        }

        private bool MatchHasRecordedEvents(List<TeamStats> stats)
        {
            List<int> statIds = stats.Select(teamStats => teamStats.Id).ToList();
            if (statIds.Count == 0)
            {
                return false;
            }

            bool hasGoals = _repository.Goal.Find(goal => statIds.Contains(goal.TeamStatsId)).Any();
            bool hasCards = _repository.Card.Find(card => statIds.Contains(card.TeamStatsId)).Any();
            return hasGoals || hasCards;
        }

        private static string CardTypeName(int? type)
        {
            return type switch
            {
                1 => "Yellow",
                2 => "Red",
                _ => "Unknown"
            };
        }

        private static (string Status, bool CanBet) ResolveMatchStatus(Match match)
        {
            if (!match.TeamOneId.HasValue || !match.TeamTwoId.HasValue)
            {
                return ("Scheduled", false);
            }

            DateTime now = DateTime.UtcNow;
            if (match.Date > now)
            {
                return ("Scheduled", true);
            }

            DateTime fullTime = match.Date.AddMinutes(BetScoringRules.MatchDurationMinutes);
            if (fullTime > now)
            {
                return ("Live", false);
            }

            return ("Finished", false);
        }

        private static (string Status, bool CanBet) ResolveMatchStatus(DateTime kickoff)
        {
            return ResolveMatchStatus(new Match
            {
                Date = kickoff,
                TeamOneId = 1,
                TeamTwoId = 2
            });
        }

        private static DateTime ToUtc(DateTime value)
        {
            return value.Kind == DateTimeKind.Unspecified
                ? DateTime.SpecifyKind(value, DateTimeKind.Utc)
                : value.ToUniversalTime();
        }
    }
}
