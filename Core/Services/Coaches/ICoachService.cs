using Core.DTOs.Coaches;

namespace Core.Services.Coaches
{
    public interface ICoachService
    {
        List<CoachDTO> GetCoaches();
        List<CoachDTO> GetCoachesByTeam(int teamId);
        Task AddCoach(AddCoachDTO coachDto);
        Task UpdateCoach(UpdateCoachDTO coachDto);
        Task DeleteCoach(int id);
    }
}
