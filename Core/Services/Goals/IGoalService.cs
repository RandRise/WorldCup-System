using Core.DTOs.Goals;

namespace Core.Services.Goals
{
    public interface IGoalService
    {
        List<GoalDTO> GetGoalsByMatch(int matchId);
        Task<string?> AddGoal(AddGoalDTO goalDto);
        Task<string?> DeleteGoal(int id);
    }
}
