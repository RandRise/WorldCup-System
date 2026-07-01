using Core.DTOs.Goals;

namespace Core.Services.Goals
{
    public interface IGoalService
    {
        List<GoalDTO> GetGoalsByMatch(int matchId);
        Task AddGoal(AddGoalDTO goalDto);
        Task DeleteGoal(int id);
    }
}
