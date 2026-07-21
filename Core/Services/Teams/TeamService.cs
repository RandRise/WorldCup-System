using Core.DTOs.Teams;
using Data.Entities;
using Data.Repos;

namespace Core.Services.Teams
{
    public class TeamService : ITeamService
    {
        private readonly IRepositoryManager _repository;

        public TeamService(IRepositoryManager repository)
        {
            _repository = repository;
        }

        public List<TeamDTO> GetTeams()
        {
            List<Team> teams = _repository.Team.GetAllAsync().ToList();
            List<Country> countries = _repository.Country.GetAllAsync().ToList();
            List<Group> groups = _repository.Group.GetAllAsync().ToList();

            return teams.Select(team => new TeamDTO
            {
                Id = team.Id,
                CountryId = team.CountryId,
                CountryName = countries.FirstOrDefault(country => country.Id == team.CountryId)?.Name,
                GroupId = team.GroupId,
                GroupName = groups.FirstOrDefault(group => group.Id == team.GroupId)?.Name
            }).ToList();
        }

        public async Task<TeamDTO> GetTeamById(int id)
        {
            Team team = await _repository.Team.GetByIdAsync(id);
            Country country = await _repository.Country.GetByIdAsync(team.CountryId);
            Group group = await _repository.Group.GetByIdAsync(team.GroupId);

            return new TeamDTO
            {
                Id = team.Id,
                CountryId = team.CountryId,
                CountryName = country.Name,
                GroupId = team.GroupId,
                GroupName = group.Name
            };
        }

        public async Task AddTeam(AddTeamDTO teamDto)
        {
            Country country = await _repository.Country.GetByIdAsync(teamDto.CountryId);
            Group group = await _repository.Group.GetByIdAsync(teamDto.GroupId);

            if (CountryAlreadyHasTeamInWorldCup(teamDto.CountryId, group.WorldCupId, excludeTeamId: null))
            {
                throw new InvalidOperationException(
                    $"Country with ID {teamDto.CountryId} already has a team in World Cup {group.WorldCupId}.");
            }

            Team team = new Team
            {
                CountryId = teamDto.CountryId,
                GroupId = teamDto.GroupId,
                Country = country,
                Group = group,
                Coach = new List<Coach>()
            };

            _repository.Team.Create(team);
            await _repository.SaveAsync();
        }

        public async Task UpdateTeam(UpdateTeamDTO teamDto)
        {
            Team team = await _repository.Team.GetByIdAsync(teamDto.Id);
            Group group = await _repository.Group.GetByIdAsync(teamDto.GroupId);
            Group currentGroup = await _repository.Group.GetByIdAsync(team.GroupId);

            EnsureSameWorldCup(team.Id, currentGroup.WorldCupId, group.WorldCupId);

            if (team.CountryId != teamDto.CountryId
                || team.GroupId != teamDto.GroupId)
            {
                if (CountryAlreadyHasTeamInWorldCup(teamDto.CountryId, group.WorldCupId, excludeTeamId: teamDto.Id))
                {
                    throw new InvalidOperationException(
                        $"Country with ID {teamDto.CountryId} already has a team in World Cup {group.WorldCupId}.");
                }
            }

            if (team.CountryId != teamDto.CountryId)
            {
                Country country = await _repository.Country.GetByIdAsync(teamDto.CountryId);
                team.CountryId = teamDto.CountryId;
                team.Country = country;
            }

            team.GroupId = teamDto.GroupId;
            team.Group = group;

            _repository.Team.Update(team);
            await _repository.SaveAsync();
        }

        public async Task AssignTeamToGroup(AssignTeamToGroupDTO assignDto)
        {
            Team team = await _repository.Team.GetByIdAsync(assignDto.TeamId);
            Group group = await _repository.Group.GetByIdAsync(assignDto.GroupId);
            Group currentGroup = await _repository.Group.GetByIdAsync(team.GroupId);

            EnsureSameWorldCup(team.Id, currentGroup.WorldCupId, group.WorldCupId);

            if (team.GroupId != assignDto.GroupId
                && CountryAlreadyHasTeamInWorldCup(team.CountryId, group.WorldCupId, excludeTeamId: team.Id))
            {
                throw new InvalidOperationException(
                    $"Country with ID {team.CountryId} already has a team in World Cup {group.WorldCupId}.");
            }

            team.GroupId = assignDto.GroupId;
            team.Group = group;

            _repository.Team.Update(team);
            await _repository.SaveAsync();
        }

        public async Task DeleteTeam(int id)
        {
            Team team = await _repository.Team.GetByIdAsync(id);

            bool hasCoaches = _repository.Coach.Find(coach => coach.TeamId == id).Any();
            bool hasPlayers = _repository.Player.Find(player => player.TeamId == id).Any();
            if (hasCoaches || hasPlayers)
            {
                throw new InvalidOperationException(
                    "Cannot delete team while coaches or players are still assigned. Remove them first.");
            }

            bool hasMatches = _repository.Match
                .Find(match => match.TeamOneId == id || match.TeamTwoId == id)
                .Any();
            if (hasMatches)
            {
                throw new InvalidOperationException(
                    "Cannot delete team while it is scheduled in one or more matches. Delete those matches first.");
            }

            bool hasBets = _repository.Bet.Find(bet => bet.TeamId == id).Any();
            if (hasBets)
            {
                throw new InvalidOperationException(
                    "Cannot delete team while bets reference it. Remove related bets first.");
            }

            bool hasTeamStats = _repository.TeamStats.Find(teamStats => teamStats.TeamId == id).Any();
            if (hasTeamStats)
            {
                throw new InvalidOperationException(
                    "Cannot delete team while match statistics reference it. Remove related match data first.");
            }

            _repository.Team.Delete(team);
            await _repository.SaveAsync();
        }

        /// <summary>
        /// Multi-cup: a team stays inside one World Cup; never reassign across cups.
        /// </summary>
        private static void EnsureSameWorldCup(int teamId, int currentWorldCupId, int targetWorldCupId)
        {
            if (currentWorldCupId != targetWorldCupId)
            {
                throw new InvalidOperationException(
                    $"Cannot move team {teamId} from World Cup {currentWorldCupId} to World Cup {targetWorldCupId}.");
            }
        }

        /// <summary>
        /// Same country may appear in multiple World Cups, but only once per cup.
        /// </summary>
        private bool CountryAlreadyHasTeamInWorldCup(int countryId, int worldCupId, int? excludeTeamId)
        {
            List<Team> countryTeams = _repository.Team
                .Find(existingTeam => existingTeam.CountryId == countryId)
                .Where(existingTeam => !excludeTeamId.HasValue || existingTeam.Id != excludeTeamId.Value)
                .ToList();

            if (countryTeams.Count == 0)
            {
                return false;
            }

            HashSet<int> groupIds = countryTeams.Select(existingTeam => existingTeam.GroupId).ToHashSet();
            return _repository.Group
                .Find(group => groupIds.Contains(group.Id) && group.WorldCupId == worldCupId)
                .Any();
        }
    }
}
