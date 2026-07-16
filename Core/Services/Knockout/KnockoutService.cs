using Core.DTOs.Matches;
using Core.DTOs.Standings;
using Core.Services.Bets;
using Core.Services.Matches;
using Core.Services.Standings;
using Data.Entities;
using Data.Repos;

namespace Core.Services.Knockout
{
    public class KnockoutService : IKnockoutService
    {
        private readonly IRepositoryManager _repository;
        private readonly IStandingsService _standingsService;

        public KnockoutService(IRepositoryManager repository, IStandingsService standingsService)
        {
            _repository = repository;
            _standingsService = standingsService;
        }

        public BracketDTO GetBracket(int worldCupId)
        {
            List<Match> matches = GetWorldCupKnockoutMatches(worldCupId);
            List<MatchDTO> matchDtos = BuildMatchDtos(matches)
                .OrderBy(match => StageSortOrder(match.Stage))
                .ThenBy(match => match.Date)
                .ThenBy(match => match.Id)
                .ToList();

            List<BracketRoundDTO> rounds = matchDtos
                .GroupBy(match => match.Stage)
                .OrderBy(group => StageSortOrder(group.Key))
                .Select(group => new BracketRoundDTO
                {
                    Stage = group.Key,
                    StageName = MatchService.FormatStageName(group.Key),
                    Matches = group.ToList()
                })
                .ToList();

            return new BracketDTO
            {
                WorldCupId = worldCupId,
                Rounds = rounds
            };
        }

        public async Task<GenerateBracketResultDTO> GenerateBracket(GenerateBracketDTO request)
        {
            await _repository.WorldCup.GetByIdAsync(request.WorldCupId);
            await _repository.Stadium.GetByIdAsync(request.StadiumId);

            List<Group> groups = _repository.Group
                .Find(group => group.WorldCupId == request.WorldCupId)
                .OrderBy(group => group.Name)
                .ToList();

            if (groups.Count != 8)
            {
                throw new InvalidOperationException(
                    "Classic knockout generation requires exactly 8 groups (A–H). Schedule Round of 16 matches manually for other formats.");
            }

            if (GetWorldCupKnockoutMatches(request.WorldCupId).Count > 0)
            {
                throw new InvalidOperationException(
                    "Knockout matches already exist for this World Cup. Delete them before regenerating the bracket.");
            }

            List<(int First, int Second)> qualified = new List<(int First, int Second)>();
            foreach (Group group in groups)
            {
                List<StandingDTO> standings = _standingsService.GetGroupStandings(group.Id);
                if (standings.Count < 2)
                {
                    throw new InvalidOperationException(
                        $"Group {group.Name} needs at least two teams with standings to seed the bracket.");
                }

                if (standings[0].Played == 0)
                {
                    throw new InvalidOperationException(
                        $"Group {group.Name} has no finished matches yet. Complete the group stage before generating the bracket.");
                }

                qualified.Add((standings[0].TeamId, standings[1].TeamId));
            }

            List<(int TeamOneId, int TeamTwoId)> roundOf16Pairings = new List<(int, int)>
            {
                (qualified[0].First, qualified[1].Second),
                (qualified[2].First, qualified[3].Second),
                (qualified[1].First, qualified[0].Second),
                (qualified[3].First, qualified[2].Second),
                (qualified[4].First, qualified[5].Second),
                (qualified[6].First, qualified[7].Second),
                (qualified[5].First, qualified[4].Second),
                (qualified[7].First, qualified[6].Second),
            };

            DateTime kickoff = request.FirstKickoff.Kind == DateTimeKind.Unspecified
                ? DateTime.SpecifyKind(request.FirstKickoff, DateTimeKind.Utc)
                : request.FirstKickoff.ToUniversalTime();

            List<Match> roundOf16 = new List<Match>();
            for (int index = 0; index < roundOf16Pairings.Count; index++)
            {
                (int teamOneId, int teamTwoId) = roundOf16Pairings[index];
                Match match = CreateKnockoutMatch(
                    MatchStage.RoundOf16,
                    kickoff.AddDays(index),
                    request.StadiumId,
                    teamOneId,
                    teamTwoId,
                    feederOneId: null,
                    feederTwoId: null,
                    feederOneTakesLoser: false,
                    feederTwoTakesLoser: false);
                roundOf16.Add(match);
                _repository.Match.Create(match);
            }

            await _repository.SaveAsync();

            DateTime quarterStart = kickoff.AddDays(10);
            int[][] quarterFeeders =
            {
                new[] { 0, 1 },
                new[] { 4, 5 },
                new[] { 2, 3 },
                new[] { 6, 7 }
            };
            List<Match> quarters = new List<Match>();
            for (int index = 0; index < quarterFeeders.Length; index++)
            {
                Match match = CreateKnockoutMatch(
                    MatchStage.QuarterFinal,
                    quarterStart.AddDays(index),
                    request.StadiumId,
                    teamOneId: null,
                    teamTwoId: null,
                    feederOneId: roundOf16[quarterFeeders[index][0]].Id,
                    feederTwoId: roundOf16[quarterFeeders[index][1]].Id,
                    feederOneTakesLoser: false,
                    feederTwoTakesLoser: false);
                quarters.Add(match);
                _repository.Match.Create(match);
            }

            await _repository.SaveAsync();

            DateTime semiStart = quarterStart.AddDays(6);
            List<Match> semis = new List<Match>();
            for (int index = 0; index < 2; index++)
            {
                Match match = CreateKnockoutMatch(
                    MatchStage.SemiFinal,
                    semiStart.AddDays(index),
                    request.StadiumId,
                    teamOneId: null,
                    teamTwoId: null,
                    feederOneId: quarters[index * 2].Id,
                    feederTwoId: quarters[index * 2 + 1].Id,
                    feederOneTakesLoser: false,
                    feederTwoTakesLoser: false);
                semis.Add(match);
                _repository.Match.Create(match);
            }

            await _repository.SaveAsync();

            Match thirdPlace = CreateKnockoutMatch(
                MatchStage.ThirdPlace,
                semiStart.AddDays(4),
                request.StadiumId,
                teamOneId: null,
                teamTwoId: null,
                feederOneId: semis[0].Id,
                feederTwoId: semis[1].Id,
                feederOneTakesLoser: true,
                feederTwoTakesLoser: true);
            _repository.Match.Create(thirdPlace);

            Match final = CreateKnockoutMatch(
                MatchStage.Final,
                semiStart.AddDays(5),
                request.StadiumId,
                teamOneId: null,
                teamTwoId: null,
                feederOneId: semis[0].Id,
                feederTwoId: semis[1].Id,
                feederOneTakesLoser: false,
                feederTwoTakesLoser: false);
            _repository.Match.Create(final);

            await _repository.SaveAsync();

            int created = roundOf16.Count + quarters.Count + semis.Count + 2;
            return new GenerateBracketResultDTO
            {
                MatchesCreated = created,
                Message = $"Created {created} knockout matches (Round of 16 through Final).",
                Bracket = GetBracket(request.WorldCupId)
            };
        }

        public async Task TryAdvanceFromMatch(int matchId)
        {
            Match source = await _repository.Match.GetByIdAsync(matchId);
            if (source.Stage == MatchStage.Group)
            {
                return;
            }

            if (!source.TeamOneId.HasValue || !source.TeamTwoId.HasValue)
            {
                return;
            }

            DateTime fullTime = source.Date.AddMinutes(BetScoringRules.MatchDurationMinutes);
            if (fullTime > DateTime.UtcNow)
            {
                return;
            }

            (int? winnerId, int? loserId) = ResolveWinnerAndLoser(source);
            if (!winnerId.HasValue || !loserId.HasValue)
            {
                return;
            }

            List<Match> destinations = _repository.Match
                .Find(match => match.FeederMatchOneId == matchId || match.FeederMatchTwoId == matchId)
                .ToList();

            bool changed = false;
            foreach (Match destination in destinations)
            {
                bool destinationChanged = false;
                if (destination.FeederMatchOneId == matchId)
                {
                    int advancedTeamId = destination.FeederOneTakesLoser ? loserId.Value : winnerId.Value;
                    if (ApplyTeamSlot(destination, isTeamOne: true, advancedTeamId))
                    {
                        destinationChanged = true;
                    }
                }

                if (destination.FeederMatchTwoId == matchId)
                {
                    int advancedTeamId = destination.FeederTwoTakesLoser ? loserId.Value : winnerId.Value;
                    if (ApplyTeamSlot(destination, isTeamOne: false, advancedTeamId))
                    {
                        destinationChanged = true;
                    }
                }

                if (destinationChanged)
                {
                    _repository.Match.Update(destination);
                    changed = true;
                }
            }

            if (changed)
            {
                await _repository.SaveAsync();
            }
        }

        private List<Match> GetWorldCupKnockoutMatches(int worldCupId)
        {
            List<int> groupIds = _repository.Group
                .Find(group => group.WorldCupId == worldCupId)
                .Select(group => group.Id)
                .ToList();
            if (groupIds.Count == 0)
            {
                return new List<Match>();
            }

            List<int> teamIds = _repository.Team
                .Find(team => groupIds.Contains(team.GroupId))
                .Select(team => team.Id)
                .ToList();

            HashSet<int> matchIds = CollectWorldCupMatchIds(teamIds);
            return _repository.Match
                .Find(match => matchIds.Contains(match.Id) && match.Stage != MatchStage.Group)
                .ToList();
        }

        private HashSet<int> CollectWorldCupMatchIds(List<int> teamIds)
        {
            List<Match> allMatches = _repository.Match.GetAllAsync().ToList();
            HashSet<int> ids = allMatches
                .Where(match =>
                    (match.TeamOneId.HasValue && teamIds.Contains(match.TeamOneId.Value))
                    || (match.TeamTwoId.HasValue && teamIds.Contains(match.TeamTwoId.Value)))
                .Select(match => match.Id)
                .ToHashSet();

            bool expanded;
            do
            {
                expanded = false;
                foreach (Match match in allMatches)
                {
                    if (ids.Contains(match.Id))
                    {
                        continue;
                    }

                    bool linked =
                        (match.FeederMatchOneId.HasValue && ids.Contains(match.FeederMatchOneId.Value))
                        || (match.FeederMatchTwoId.HasValue && ids.Contains(match.FeederMatchTwoId.Value));
                    if (linked)
                    {
                        ids.Add(match.Id);
                        expanded = true;
                    }
                }
            }
            while (expanded);

            return ids;
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
                    StageName = MatchService.FormatStageName(match.Stage),
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
                    CanBet = canBet
                };
            }).ToList();
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

        private bool ApplyTeamSlot(Match destination, bool isTeamOne, int teamId)
        {
            int? currentId = isTeamOne ? destination.TeamOneId : destination.TeamTwoId;
            if (currentId == teamId)
            {
                EnsureTeamStats(destination.Id, teamId);
                return false;
            }

            List<TeamStats> stats = _repository.TeamStats
                .Find(teamStats => teamStats.MatchId == destination.Id)
                .ToList();
            if (HasRecordedEvents(stats))
            {
                throw new InvalidOperationException(
                    $"Cannot advance into match {destination.Id}: goals or cards are already recorded.");
            }

            if (currentId.HasValue)
            {
                TeamStats? existingStats = stats.FirstOrDefault(teamStats => teamStats.TeamId == currentId.Value);
                if (existingStats != null)
                {
                    existingStats.TeamId = teamId;
                    _repository.TeamStats.Update(existingStats);
                }
                else
                {
                    _repository.TeamStats.Create(new TeamStats { MatchId = destination.Id, TeamId = teamId });
                }
            }
            else
            {
                _repository.TeamStats.Create(new TeamStats { MatchId = destination.Id, TeamId = teamId });
            }

            if (isTeamOne)
            {
                destination.TeamOneId = teamId;
            }
            else
            {
                destination.TeamTwoId = teamId;
            }

            return true;
        }

        private void EnsureTeamStats(int matchId, int teamId)
        {
            bool exists = _repository.TeamStats
                .Find(teamStats => teamStats.MatchId == matchId && teamStats.TeamId == teamId)
                .Any();
            if (!exists)
            {
                _repository.TeamStats.Create(new TeamStats { MatchId = matchId, TeamId = teamId });
            }
        }

        private (int? WinnerId, int? LoserId) ResolveWinnerAndLoser(Match match)
        {
            List<TeamStats> stats = _repository.TeamStats
                .Find(teamStats => teamStats.MatchId == match.Id)
                .ToList();
            TeamStats? oneStats = stats.FirstOrDefault(teamStats => teamStats.TeamId == match.TeamOneId);
            TeamStats? twoStats = stats.FirstOrDefault(teamStats => teamStats.TeamId == match.TeamTwoId);
            if (oneStats == null || twoStats == null)
            {
                return (null, null);
            }

            List<int> statIds = new List<int> { oneStats.Id, twoStats.Id };
            List<Goal> goals = _repository.Goal.Find(goal => statIds.Contains(goal.TeamStatsId)).ToList();
            int oneScore = goals.Count(goal => goal.TeamStatsId == oneStats.Id);
            int twoScore = goals.Count(goal => goal.TeamStatsId == twoStats.Id);

            if (oneScore == twoScore)
            {
                return (null, null);
            }

            if (oneScore > twoScore)
            {
                return (match.TeamOneId, match.TeamTwoId);
            }

            return (match.TeamTwoId, match.TeamOneId);
        }

        private bool HasRecordedEvents(List<TeamStats> stats)
        {
            List<int> statIds = stats.Select(teamStats => teamStats.Id).ToList();
            if (statIds.Count == 0)
            {
                return false;
            }

            return _repository.Goal.Find(goal => statIds.Contains(goal.TeamStatsId)).Any()
                || _repository.Card.Find(card => statIds.Contains(card.TeamStatsId)).Any();
        }

        private static Match CreateKnockoutMatch(
            MatchStage stage,
            DateTime date,
            int stadiumId,
            int? teamOneId,
            int? teamTwoId,
            int? feederOneId,
            int? feederTwoId,
            bool feederOneTakesLoser,
            bool feederTwoTakesLoser)
        {
            Match match = new Match
            {
                Date = date,
                Stage = stage,
                StadiumId = stadiumId,
                TeamOneId = teamOneId,
                TeamTwoId = teamTwoId,
                FeederMatchOneId = feederOneId,
                FeederMatchTwoId = feederTwoId,
                FeederOneTakesLoser = feederOneTakesLoser,
                FeederTwoTakesLoser = feederTwoTakesLoser,
                TeamStats = new List<TeamStats>()
            };

            if (teamOneId.HasValue)
            {
                match.TeamStats.Add(new TeamStats { TeamId = teamOneId.Value });
            }

            if (teamTwoId.HasValue)
            {
                match.TeamStats.Add(new TeamStats { TeamId = teamTwoId.Value });
            }

            return match;
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

        /// <summary>
        /// Tournament order (RoundOf32 is enum value 6 to avoid renumbering older stages).
        /// </summary>
        private static int StageSortOrder(MatchStage stage)
        {
            return stage switch
            {
                MatchStage.Group => 0,
                MatchStage.RoundOf32 => 1,
                MatchStage.RoundOf16 => 2,
                MatchStage.QuarterFinal => 3,
                MatchStage.SemiFinal => 4,
                MatchStage.ThirdPlace => 5,
                MatchStage.Final => 6,
                _ => 99
            };
        }
    }
}
