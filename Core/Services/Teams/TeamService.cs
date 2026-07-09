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

            bool countryHasTeam = _repository.Team.Find(team => team.CountryId == teamDto.CountryId).Any();
            if (countryHasTeam)
            {
                throw new InvalidOperationException($"Country with ID {teamDto.CountryId} already has a team.");
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

            if (team.CountryId != teamDto.CountryId)
            {
                Country country = await _repository.Country.GetByIdAsync(teamDto.CountryId);
                bool countryHasTeam = _repository.Team
                    .Find(existingTeam => existingTeam.CountryId == teamDto.CountryId && existingTeam.Id != teamDto.Id)
                    .Any();
                if (countryHasTeam)
                {
                    throw new InvalidOperationException($"Country with ID {teamDto.CountryId} already has a team.");
                }

                team.CountryId = teamDto.CountryId;
                team.Country = country;
            }

            Group group = await _repository.Group.GetByIdAsync(teamDto.GroupId);
            team.GroupId = teamDto.GroupId;
            team.Group = group;

            _repository.Team.Update(team);
            await _repository.SaveAsync();
        }

        public async Task AssignTeamToGroup(AssignTeamToGroupDTO assignDto)
        {
            Team team = await _repository.Team.GetByIdAsync(assignDto.TeamId);
            Group group = await _repository.Group.GetByIdAsync(assignDto.GroupId);

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
    }
}
