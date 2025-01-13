using Core.DTOs.Teams;
using Data.Entities;
using Data.Repos;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.Services.Teams
{
    public class TeamService : ITeamService
    {
        private readonly IRepositoryManager _repository;

        public TeamService(IRepositoryManager repository)
        {
            _repository = repository;

        }
        public async Task AddNewTeam(AddTeamDto addTeamDto)
        {
            var country = await _repository.Country.GetByIdAsync(addTeamDto.CountryId);
            if (country == null)
            {
                Console.WriteLine($"Country with ID {addTeamDto.CountryId} does not exist.");
                throw new ArgumentException($"Country with ID {addTeamDto.CountryId} does not exist.");
            }

            var group = await _repository.Group.GetByIdAsync(addTeamDto.GroupId);
            if (group == null)
            {
                Console.WriteLine($"Group with ID {addTeamDto.GroupId} does not exist.");
                throw new ArgumentException($"Group with ID {addTeamDto.GroupId} does not exist.");
            }

            var team = new Team
            {
                CountryId = addTeamDto.CountryId,
                GroupId = addTeamDto.GroupId,
                Country = country,
                Group = group,
                Coach = new List<Coach>()
            };
            _repository.Team.Create(team);
            await _repository.SaveAsync();
        }

        public List<TeamDTO> GetTeams()
        {
            var teams = _repository.Team.GetAllAsync();
            var teamDto = teams.Select(e => new TeamDTO
            {
                Id = e.Id,
                Name = e.Country.Name,
                GroupId = e.Group.Id
            }).ToList();
            return teamDto;
        }
    }
}
