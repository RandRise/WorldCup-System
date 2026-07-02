using System.Reflection;
using System.Text;
using Core.DTOs.Seeding;
using Data.Entities;
using Data.Repos;

namespace Core.Services.Seeding
{
    public class DemoSeedService : IDemoSeedService
    {
        private static readonly DateTime WorldCup2026Year = new DateTime(2026, 6, 11, 0, 0, 0, DateTimeKind.Utc);
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

        private readonly IRepositoryManager _repository;

        public DemoSeedService(IRepositoryManager repository)
        {
            _repository = repository;
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

                bool isFullySeeded = worldCupGroupIds.Count == GroupNames.Length &&
                    worldCupGroupIds.All(groupId =>
                        _repository.Team.Find(team => team.GroupId == groupId).Count() >= ExpectedTeamsPerGroup) &&
                    _repository.Team.Find(team => worldCupGroupIds.Contains(team.GroupId)).Count() >= ExpectedTeamCount;

                if (isFullySeeded)
                {
                    result.AlreadySeeded = true;
                    result.WorldCupId = existingWorldCup.Id;
                    result.Message = "World Cup 2026 demo tournament is already seeded.";
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

            result.Message =
                $"Seeded World Cup 2026 demo: {result.CountriesAdded} countries, {result.CitiesAdded} cities, " +
                $"{result.StadiumsAdded} stadiums, {result.TeamsAdded} teams.";

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
