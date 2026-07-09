using System.Reflection;
using System.Text;
using Core.DTOs.Seeding;
using Core.Services.Bets;
using Data.Entities;
using Data.Repos;

namespace Core.Services.Seeding
{
    public class DemoSeedService : IDemoSeedService
    {
        private static readonly DateTime WorldCup2026Year = new DateTime(2026, 6, 11, 0, 0, 0, DateTimeKind.Utc);
        private static readonly DateTime GroupStageStart = new DateTime(2026, 6, 11, 16, 0, 0, DateTimeKind.Utc);
        private static readonly string[] GroupNames =
        {
            "A", "B", "C", "D", "E", "F", "G", "H", "I", "J", "K", "L",
        };
        private static readonly string[] ExtraCountries =
        {
            "England", "Scotland", "Australia", "New Zealand", "Cape Verde", "Curaçao",
        };
        private const int ExpectedTeamsPerGroup = 4;
        private const int ExpectedTeamCount = 48;
        private const int ExpectedGroupStageMatchesPerGroup = 6;
        private const string PlaceholderPlayerName = "Demo Scorer";
        private const string PlaceholderPositionName = "Forward";

        private readonly IRepositoryManager _repository;
        private readonly IBetService _betService;

        public DemoSeedService(IRepositoryManager repository, IBetService betService)
        {
            _repository = repository;
            _betService = betService;
        }

        public async Task<DemoSeedResultDTO> SeedWorldCup2026DemoAsync()
        {
            DemoSeedResultDTO result = new DemoSeedResultDTO
            {
                Message = "World Cup 2026 demo data loaded.",
            };

            WorldCup? existingWorldCup = _repository.WorldCup
                .Find(worldCup => worldCup.Year == WorldCup2026Year)
                .FirstOrDefault();

            if (existingWorldCup != null)
            {
                List<int> worldCupGroupIds = _repository.Group
                    .Find(group => group.WorldCupId == existingWorldCup.Id)
                    .Select(group => group.Id)
                    .ToList();

                int teamCount = _repository.Team
                    .Find(team => worldCupGroupIds.Contains(team.GroupId))
                    .Count();
                int matchCount = CountGroupStageMatches(worldCupGroupIds);

                bool isFullySeeded = worldCupGroupIds.Count == GroupNames.Length &&
                    worldCupGroupIds.All(groupId =>
                        _repository.Team.Find(team => team.GroupId == groupId).Count() >= ExpectedTeamsPerGroup) &&
                    teamCount >= ExpectedTeamCount &&
                    matchCount >= GroupNames.Length * ExpectedGroupStageMatchesPerGroup;

                if (isFullySeeded)
                {
                    result.AlreadySeeded = true;
                    result.WorldCupId = existingWorldCup.Id;
                    result.GoalsAdded = await BackfillGoalsForFinishedMatchesAsync(existingWorldCup.Id);
                    result.BetsResolved = await ResolveFinishedBetsAsync(existingWorldCup.Id);
                    result.Message = result.GoalsAdded > 0 || result.BetsResolved > 0
                        ? $"World Cup 2026 demo already seeded; backfilled {result.GoalsAdded} goals and resolved {result.BetsResolved} bets."
                        : "World Cup 2026 demo tournament is already seeded.";
                    return result;
                }
            }

            result.CountriesAdded = await ImportCountriesAsync();
            result.CitiesAdded = await ImportHostCitiesAsync();
            result.StadiumsAdded = await ImportStadiumsAsync();

            WorldCup worldCup = existingWorldCup ?? await CreateWorldCupAsync();
            result.WorldCupId = worldCup.Id;

            (Dictionary<string, int> groupIdsByName, int groupsAdded) = await EnsureGroupsAsync(worldCup.Id);
            result.GroupsAdded = groupsAdded;

            result.TeamsAdded = await ImportTeamsAsync(groupIdsByName);
            await _repository.SaveAsync();

            (int matchesAdded, int goalsAdded) = await ImportGroupStageFixturesAsync(worldCup.Id, groupIdsByName);
            result.MatchesAdded = matchesAdded;
            result.GoalsAdded = goalsAdded;

            result.BetsResolved = await ResolveFinishedBetsAsync(worldCup.Id);

            await _repository.SaveAsync();

            result.Message =
                $"Seeded World Cup 2026 demo: {result.CountriesAdded} countries, {result.CitiesAdded} cities, " +
                $"{result.StadiumsAdded} stadiums, {result.TeamsAdded} teams, {result.MatchesAdded} matches, " +
                $"{result.GoalsAdded} goals, {result.BetsResolved} bets resolved.";

            return result;
        }

        private async Task<int> ImportCountriesAsync()
        {
            int added = 0;

            await foreach (string[] columns in ReadCsvRowsAsync("CitiesCsvData/all_countries.csv", skipHeader: false))
            {
                if (columns.Length < 2)
                {
                    continue;
                }

                string countryName = columns[1].Trim();
                if (string.IsNullOrWhiteSpace(countryName))
                {
                    continue;
                }

                if (EnsureCountryExists(countryName))
                {
                    added++;
                }
            }

            foreach (string countryName in ExtraCountries)
            {
                if (EnsureCountryExists(countryName))
                {
                    added++;
                }
            }

            await _repository.SaveAsync();
            return added;
        }

        private async Task<int> ImportHostCitiesAsync()
        {
            int added = 0;

            await foreach (string[] columns in ReadCsvRowsAsync("CitiesCsvData/WorldCup2026_host_cities.csv"))
            {
                if (columns.Length < 2)
                {
                    continue;
                }

                string cityName = columns[0].Trim();
                string countryName = columns[1].Trim();

                if (string.IsNullOrWhiteSpace(cityName) || string.IsNullOrWhiteSpace(countryName))
                {
                    continue;
                }

                Country? country = _repository.Country.Find(c => c.Name == countryName).FirstOrDefault();
                if (country == null)
                {
                    throw new InvalidOperationException($"Host country '{countryName}' was not found after importing countries.");
                }

                bool cityExists = _repository.City
                    .Find(city => city.Name == cityName && city.CountryId == country.Id)
                    .Any();

                if (!cityExists)
                {
                    _repository.City.Create(new City { Name = cityName, CountryId = country.Id });
                    added++;
                }
            }

            await _repository.SaveAsync();
            return added;
        }

        private async Task<int> ImportStadiumsAsync()
        {
            int added = 0;

            await foreach (string[] columns in ReadCsvRowsAsync("StadiumCsvData/Stadiums.csv"))
            {
                if (columns.Length < 2)
                {
                    continue;
                }

                string stadiumName = columns[0].Trim();
                string cityName = columns[1].Trim();

                if (string.IsNullOrWhiteSpace(stadiumName) || string.IsNullOrWhiteSpace(cityName))
                {
                    continue;
                }

                bool stadiumExists = _repository.Stadium.Find(stadium => stadium.Name == stadiumName).Any();
                if (stadiumExists)
                {
                    continue;
                }

                City? city = _repository.City.Find(city => city.Name == cityName).FirstOrDefault();
                if (city == null)
                {
                    throw new KeyNotFoundException($"City '{cityName}' was not found. Load host cities before stadiums.");
                }

                _repository.Stadium.Create(new Stadium { Name = stadiumName, CityId = city.Id });
                added++;
            }

            await _repository.SaveAsync();
            return added;
        }

        private async Task<WorldCup> CreateWorldCupAsync()
        {
            WorldCup worldCup = new WorldCup { Year = WorldCup2026Year, Groups = new List<Group>() };
            _repository.WorldCup.Create(worldCup);
            await _repository.SaveAsync();
            return worldCup;
        }

        private async Task<(Dictionary<string, int> GroupIdsByName, int GroupsAdded)> EnsureGroupsAsync(int worldCupId)
        {
            Dictionary<string, int> groupIdsByName = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            int groupsAdded = 0;

            foreach (string groupName in GroupNames)
            {
                Group? existingGroup = _repository.Group
                    .Find(group => group.WorldCupId == worldCupId && group.Name == groupName)
                    .FirstOrDefault();

                if (existingGroup != null)
                {
                    groupIdsByName[groupName] = existingGroup.Id;
                    continue;
                }

                Group group = new Group
                {
                    Name = groupName,
                    WorldCupId = worldCupId,
                    Teams = new List<Team>(),
                };

                _repository.Group.Create(group);
                await _repository.SaveAsync();
                groupIdsByName[groupName] = group.Id;
                groupsAdded++;
            }

            return (groupIdsByName, groupsAdded);
        }

        private async Task<int> ImportTeamsAsync(Dictionary<string, int> groupIdsByName)
        {
            int added = 0;

            await foreach (string[] columns in ReadCsvRowsAsync("DemoSeedData/WorldCup2026_teams.csv"))
            {
                if (columns.Length < 2)
                {
                    continue;
                }

                string groupName = columns[0].Trim();
                string countryName = columns[1].Trim();

                if (string.IsNullOrWhiteSpace(groupName) || string.IsNullOrWhiteSpace(countryName))
                {
                    continue;
                }

                if (!groupIdsByName.TryGetValue(groupName, out int groupId))
                {
                    throw new KeyNotFoundException($"Group '{groupName}' was not found for World Cup 2026.");
                }

                Country? country = _repository.Country.Find(c => c.Name == countryName).FirstOrDefault();
                if (country == null)
                {
                    throw new KeyNotFoundException($"Country '{countryName}' was not found. Import countries first.");
                }

                bool teamExists = _repository.Team.Find(team => team.CountryId == country.Id).Any();
                if (teamExists)
                {
                    continue;
                }

                Group group = await _repository.Group.GetByIdAsync(groupId);

                Team team = new Team
                {
                    CountryId = country.Id,
                    GroupId = groupId,
                    Country = country,
                    Group = group,
                    Coach = new List<Coach>(),
                };

                _repository.Team.Create(team);
                added++;
            }

            return added;
        }

        private async Task<(int MatchesAdded, int GoalsAdded)> ImportGroupStageFixturesAsync(
            int worldCupId,
            Dictionary<string, int> groupIdsByName)
        {
            List<Stadium> stadiums = _repository.Stadium.GetAllAsync().ToList();
            if (stadiums.Count == 0)
            {
                throw new InvalidOperationException("No stadiums available for fixture seeding.");
            }

            PlayerPosition forwardPosition = _repository.PlayerPosition
                .Find(position => position.Name == PlaceholderPositionName)
                .FirstOrDefault()
                ?? throw new InvalidOperationException($"Player position '{PlaceholderPositionName}' was not found.");

            int matchesAdded = 0;
            int goalsAdded = 0;
            int stadiumIndex = 0;
            DateTime now = DateTime.UtcNow;

            foreach (string groupName in GroupNames)
            {
                if (!groupIdsByName.TryGetValue(groupName, out int groupId))
                {
                    continue;
                }

                List<Team> teams = _repository.Team
                    .Find(team => team.GroupId == groupId)
                    .OrderBy(team => team.Id)
                    .ToList();
                if (teams.Count < ExpectedTeamsPerGroup)
                {
                    continue;
                }

                (int teamOneId, int teamTwoId)[] pairings =
                {
                    (teams[0].Id, teams[1].Id),
                    (teams[2].Id, teams[3].Id),
                    (teams[0].Id, teams[2].Id),
                    (teams[1].Id, teams[3].Id),
                    (teams[0].Id, teams[3].Id),
                    (teams[1].Id, teams[2].Id),
                };

                for (int matchIndex = 0; matchIndex < pairings.Length; matchIndex++)
                {
                    (int teamOneId, int teamTwoId) pairing = pairings[matchIndex];
                    if (MatchExists(pairing.teamOneId, pairing.teamTwoId))
                    {
                        continue;
                    }

                    int round = matchIndex / 2;
                    DateTime kickoff = GroupStageStart
                        .AddDays(round * 4 + Array.IndexOf(GroupNames, groupName))
                        .AddHours((matchIndex % 2) * 3);

                    Stadium stadium = stadiums[stadiumIndex % stadiums.Count];
                    stadiumIndex++;

                    Match match = new Match
                    {
                        Date = kickoff,
                        StadiumId = stadium.Id,
                        TeamOneId = pairing.teamOneId,
                        TeamTwoId = pairing.teamTwoId,
                        TeamStats = new List<TeamStats>
                        {
                            new TeamStats { TeamId = pairing.teamOneId },
                            new TeamStats { TeamId = pairing.teamTwoId },
                        },
                    };

                    _repository.Match.Create(match);
                    await _repository.SaveAsync();
                    matchesAdded++;

                    if (kickoff.AddMinutes(BetScoringRules.MatchDurationMinutes) > now)
                    {
                        continue;
                    }

                    (int teamOneScore, int teamTwoScore) = GenerateDemoScore(pairing.teamOneId, pairing.teamTwoId);
                    goalsAdded += await SeedMatchGoalsAsync(
                        match,
                        teamOneScore,
                        teamTwoScore,
                        forwardPosition.Id);
                }
            }

            return (matchesAdded, goalsAdded);
        }

        private async Task<int> SeedMatchGoalsAsync(
            Match match,
            int teamOneScore,
            int teamTwoScore,
            int forwardPositionId)
        {
            int goalsAdded = 0;
            List<TeamStats> stats = _repository.TeamStats
                .Find(teamStats => teamStats.MatchId == match.Id)
                .ToList();
            TeamStats? teamOneStats = stats.FirstOrDefault(teamStats => teamStats.TeamId == match.TeamOneId);
            TeamStats? teamTwoStats = stats.FirstOrDefault(teamStats => teamStats.TeamId == match.TeamTwoId);
            if (teamOneStats == null || teamTwoStats == null)
            {
                return 0;
            }

            Player teamOneScorer = await EnsurePlaceholderPlayerAsync(match.TeamOneId, forwardPositionId);
            Player teamTwoScorer = await EnsurePlaceholderPlayerAsync(match.TeamTwoId, forwardPositionId);

            for (int goalIndex = 0; goalIndex < teamOneScore; goalIndex++)
            {
                _repository.Goal.Create(new Goal
                {
                    PlayerId = teamOneScorer.Id,
                    TeamStatsId = teamOneStats.Id,
                    TimeScored = match.Date.AddMinutes(12 + (goalIndex * 18)),
                    IsOwnGoal = 0,
                });
                goalsAdded++;
            }

            for (int goalIndex = 0; goalIndex < teamTwoScore; goalIndex++)
            {
                _repository.Goal.Create(new Goal
                {
                    PlayerId = teamTwoScorer.Id,
                    TeamStatsId = teamTwoStats.Id,
                    TimeScored = match.Date.AddMinutes(20 + (goalIndex * 18)),
                    IsOwnGoal = 0,
                });
                goalsAdded++;
            }

            await _repository.SaveAsync();
            return goalsAdded;
        }

        private async Task<Player> EnsurePlaceholderPlayerAsync(int teamId, int forwardPositionId)
        {
            Player? existingPlayer = _repository.Player
                .Find(player => player.TeamId == teamId && player.Name == PlaceholderPlayerName)
                .FirstOrDefault();
            if (existingPlayer != null)
            {
                return existingPlayer;
            }

            Player player = new Player
            {
                Name = PlaceholderPlayerName,
                Number = 99,
                TeamId = teamId,
                PositionId = forwardPositionId,
            };
            _repository.Player.Create(player);
            await _repository.SaveAsync();
            return player;
        }

        private async Task<int> BackfillGoalsForFinishedMatchesAsync(int worldCupId)
        {
            List<Match> finishedMatches = GetTournamentMatches(worldCupId)
                .Where(match => match.Date.AddMinutes(BetScoringRules.MatchDurationMinutes) <= DateTime.UtcNow)
                .ToList();
            if (finishedMatches.Count == 0)
            {
                return 0;
            }

            PlayerPosition forwardPosition = _repository.PlayerPosition
                .Find(position => position.Name == PlaceholderPositionName)
                .FirstOrDefault()
                ?? throw new InvalidOperationException($"Player position '{PlaceholderPositionName}' was not found.");

            int goalsAdded = 0;
            foreach (Match match in finishedMatches)
            {
                List<TeamStats> stats = _repository.TeamStats
                    .Find(teamStats => teamStats.MatchId == match.Id)
                    .ToList();
                if (stats.Count < 2)
                {
                    continue;
                }

                List<int> statsIds = stats.Select(teamStats => teamStats.Id).ToList();
                bool hasGoals = _repository.Goal
                    .Find(goal => statsIds.Contains(goal.TeamStatsId))
                    .Any();
                if (hasGoals)
                {
                    continue;
                }

                (int teamOneScore, int teamTwoScore) = GenerateDemoScore(match.TeamOneId, match.TeamTwoId);
                goalsAdded += await SeedMatchGoalsAsync(
                    match,
                    teamOneScore,
                    teamTwoScore,
                    forwardPosition.Id);
            }

            return goalsAdded;
        }

        private async Task<int> ResolveFinishedBetsAsync(int worldCupId)
        {
            List<Match> finishedMatches = GetTournamentMatches(worldCupId)
                .Where(match => match.Date.AddMinutes(BetScoringRules.MatchDurationMinutes) <= DateTime.UtcNow)
                .ToList();

            int resolvedCount = 0;
            foreach (Match match in finishedMatches)
            {
                try
                {
                    resolvedCount += await _betService.ResolveBetsForMatch(match.Id);
                }
                catch (InvalidOperationException)
                {
                    // Match may lack complete stats; skip until goals are recorded.
                }
            }

            return resolvedCount;
        }

        private List<Match> GetTournamentMatches(int worldCupId)
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
            if (teamIds.Count == 0)
            {
                return new List<Match>();
            }

            return _repository.Match
                .Find(match => teamIds.Contains(match.TeamOneId) && teamIds.Contains(match.TeamTwoId))
                .ToList();
        }

        private bool MatchExists(int teamOneId, int teamTwoId)
        {
            return _repository.Match
                .Find(match =>
                    (match.TeamOneId == teamOneId && match.TeamTwoId == teamTwoId) ||
                    (match.TeamOneId == teamTwoId && match.TeamTwoId == teamOneId))
                .Any();
        }

        private int CountGroupStageMatches(List<int> groupIds)
        {
            if (groupIds.Count == 0)
            {
                return 0;
            }

            List<int> teamIds = _repository.Team
                .Find(team => groupIds.Contains(team.GroupId))
                .Select(team => team.Id)
                .ToList();
            if (teamIds.Count == 0)
            {
                return 0;
            }

            return _repository.Match
                .Find(match => teamIds.Contains(match.TeamOneId) && teamIds.Contains(match.TeamTwoId))
                .Count();
        }

        private static (int TeamOneScore, int TeamTwoScore) GenerateDemoScore(int teamOneId, int teamTwoId)
        {
            int teamOneScore = (teamOneId + teamTwoId) % 3;
            int teamTwoScore = (teamOneId * 2 + teamTwoId) % 3;
            if (teamOneScore == 0 && teamTwoScore == 0)
            {
                teamOneScore = 1;
            }

            return (teamOneScore, teamTwoScore);
        }

        private bool EnsureCountryExists(string countryName)
        {
            bool exists = _repository.Country.Find(country => country.Name == countryName).Any();
            if (exists)
            {
                return false;
            }

            _repository.Country.Create(new Country { Name = countryName });
            return true;
        }

        private static async IAsyncEnumerable<string[]> ReadCsvRowsAsync(string relativePath, bool skipHeader = true)
        {
            await using Stream stream = OpenSeedFile(relativePath);
            using StreamReader reader = new StreamReader(stream, Encoding.UTF8);

            if (skipHeader)
            {
                await reader.ReadLineAsync();
            }

            string? line;
            while ((line = await reader.ReadLineAsync()) != null)
            {
                if (string.IsNullOrWhiteSpace(line))
                {
                    continue;
                }

                yield return line.Split(',');
            }
        }

        private static Stream OpenSeedFile(string relativePath)
        {
            Assembly assembly = typeof(DemoSeedService).Assembly;
            string resourceName = $"Core.{relativePath.Replace('/', '.').Replace('\\', '.')}";
            Stream? embeddedStream = assembly.GetManifestResourceStream(resourceName);
            if (embeddedStream != null)
            {
                return embeddedStream;
            }

            string diskPath = Path.Combine(AppContext.BaseDirectory, relativePath);
            if (File.Exists(diskPath))
            {
                return File.OpenRead(diskPath);
            }

            throw new FileNotFoundException($"Seed file '{relativePath}' was not found as embedded resource or on disk.");
        }
    }
}
