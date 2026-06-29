using Core.DTOs.Teams;

namespace Core.Services.Teams
{
    public interface ITeamService
    {
        List<TeamDTO> GetTeams();
        Task<TeamDTO> GetTeamById(int id);
        Task AddTeam(AddTeamDTO teamDto);
        Task UpdateTeam(UpdateTeamDTO teamDto);
        Task AssignTeamToGroup(AssignTeamToGroupDTO assignDto);
        Task DeleteTeam(int id);
    }
}
