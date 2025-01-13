using Core.DTOs.Coaches;

namespace Core.Services.Coaches
{
    public interface ICoachService
    {
        public List<CoachDTO> GetCoachList();
        Task AddNewCoach(AddCoachDto addCoachDTO);

    }
}
