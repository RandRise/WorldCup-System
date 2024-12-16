using Core.DTOs.Teams;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.Services.Teams
{
    public interface ITeamService
    {
        public List<TeamDTO> GetTeams();
        Task AddNewTeam(AddTeamDto addTeamDto);

    }
}
