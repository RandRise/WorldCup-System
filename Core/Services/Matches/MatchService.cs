using Core.DTOs.Matches;
using Data.Entities;
using Data.Repos;

namespace Core.Services.Matches
{
    public class MatchService : IMatchService
    {
        private readonly IRepositoryManager _repository;

        public MatchService(IRepositoryManager repository)
        {
            _repository = repository;
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

            TeamStats? teamOneStats = stats.FirstOrDefault(teamStats => teamStats.TeamId == match.TeamOneId);
            TeamStats? teamTwoStats = stats.FirstOrDefault(teamStats => teamStats.TeamId == match.TeamTwoId);

            return new MatchDetailDTO
            {
                Id = match.Id,
                Date = match.Date,
                StadiumId = match.StadiumId,
                StadiumName = stadium.Name,
                TeamOneId = match.TeamOneId,
                TeamOneName = ResolveTeamName(match.TeamOneId, teams, countries),
                TeamTwoId = match.TeamTwoId,
                TeamTwoName = ResolveTeamName(match.TeamTwoId, teams, countries),
                TeamOneScore = teamOneStats == null ? 0 : goals.Count(goal => goal.TeamStatsId == teamOneStats.Id),
                TeamTwoScore = teamTwoStats == null ? 0 : goals.Count(goal => goal.TeamStatsId == teamTwoStats.Id),
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

            await _repository.Team.GetByIdAsync(matchDto.TeamOneId);
            await _repository.Team.GetByIdAsync(matchDto.TeamTwoId);
            await _repository.Stadium.GetByIdAsync(matchDto.StadiumId);

            Match match = new Match
            {
                Date = matchDto.Date,
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
            if (matchDto.TeamOneId == matchDto.TeamTwoId)
            {
                throw new InvalidOperationException("A match must be between two different teams.");
            }

            Match match = await _repository.Match.GetByIdAsync(matchDto.Id);
            await _repository.Stadium.GetByIdAsync(matchDto.StadiumId);

            List<TeamStats> stats = _repository.TeamStats.Find(teamStats => teamStats.MatchId == match.Id).ToList();
            bool hasRecordedEvents = MatchHasRecordedEvents(stats);

            bool teamsChanged = match.TeamOneId != matchDto.TeamOneId || match.TeamTwoId != matchDto.TeamTwoId;
            if (teamsChanged)
            {
                await _repository.Team.GetByIdAsync(matchDto.TeamOneId);
                await _repository.Team.GetByIdAsync(matchDto.TeamTwoId);

                if (hasRecordedEvents)
                {
                    throw new InvalidOperationException(
                        "Cannot change the participating teams after goals or cards have been recorded.");
                }

                TeamStats? teamOneStats = stats.FirstOrDefault(teamStats => teamStats.TeamId == match.TeamOneId);
                if (teamOneStats != null)
                {
                    teamOneStats.TeamId = matchDto.TeamOneId;
                    _repository.TeamStats.Update(teamOneStats);
                }

                TeamStats? teamTwoStats = stats.FirstOrDefault(teamStats => teamStats.TeamId == match.TeamTwoId);
                if (teamTwoStats != null)
                {
                    teamTwoStats.TeamId = matchDto.TeamTwoId;
                    _repository.TeamStats.Update(teamTwoStats);
                }

                match.TeamOneId = matchDto.TeamOneId;
                match.TeamTwoId = matchDto.TeamTwoId;
            }

            bool dateChanged = match.Date != matchDto.Date;
            if (dateChanged && hasRecordedEvents)
            {
                throw new InvalidOperationException(
                    "Cannot reschedule a match after goals or cards have been recorded.");
            }

            match.Date = matchDto.Date;
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

            List<Match> matches = _repository.Match
                .Find(match => teamIds.Contains(match.TeamOneId) && teamIds.Contains(match.TeamTwoId))
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
                .Find(match => teamIds.Contains(match.TeamOneId) && teamIds.Contains(match.TeamTwoId))
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
                TeamStats? teamOneStats = stats.FirstOrDefault(teamStats =>
                    teamStats.MatchId == match.Id && teamStats.TeamId == match.TeamOneId);
                TeamStats? teamTwoStats = stats.FirstOrDefault(teamStats =>
                    teamStats.MatchId == match.Id && teamStats.TeamId == match.TeamTwoId);

                return new MatchDTO
                {
                    Id = match.Id,
                    Date = match.Date,
                    StadiumId = match.StadiumId,
                    StadiumName = stadiums.FirstOrDefault(stadium => stadium.Id == match.StadiumId)?.Name,
                    TeamOneId = match.TeamOneId,
                    TeamOneName = ResolveTeamName(match.TeamOneId, teams, countries),
                    TeamTwoId = match.TeamTwoId,
                    TeamTwoName = ResolveTeamName(match.TeamTwoId, teams, countries),
                    TeamOneScore = teamOneStats == null ? 0 : goals.Count(goal => goal.TeamStatsId == teamOneStats.Id),
                    TeamTwoScore = teamTwoStats == null ? 0 : goals.Count(goal => goal.TeamStatsId == teamTwoStats.Id)
                };
            }).ToList();
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
                    PlayerName = players.FirstOrDefault(player => player.Id == goal.PlayerId)?.Name,
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
                    PlayerName = players.FirstOrDefault(player => player.Id == card.PlayerId)?.Name,
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
    }
}
