using Core.DTOs.Coaches;
using Data.Entities;
using Data.Repos;

namespace Core.Services.Coaches
{
    public class CoachService : ICoachService
    {
        private readonly IRepositoryManager _repository;
        public CoachService(IRepositoryManager repository)
        {
            _repository = repository;
        }
        public async Task AddNewCoach(AddCoachDto coachDTO)
        {
            var team = await _repository.Team.GetByIdAsync(coachDTO.TeamId);
            if (team == null)
            {
                Console.WriteLine($"Team with ID {coachDTO.TeamId} does not exist");
            }
            var coach = new Coach
            {
                Name = coachDTO.Name,
                TeamId = coachDTO.TeamId
            };
            _repository.Coach.Create(coach);
            await _repository.SaveAsync();
        }

        public List<CoachDTO> GetCoachList()
        {
            var coaches = _repository.Coach.GetAllAsync();
            var coachDto = coaches.Select(e => new CoachDTO
            {
                Name = e.Name,
                TeamId = e.TeamId
            }).ToList();
            return coachDto;
        }
    }
}
