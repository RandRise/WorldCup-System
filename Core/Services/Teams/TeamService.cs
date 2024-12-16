using Core.DTOs.Teams;
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


        public Task AddNewTeam(AddTeamDto addTeamDto)
        {
            throw new NotImplementedException();
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
