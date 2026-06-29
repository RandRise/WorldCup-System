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

        public List<CoachDTO> GetCoaches()
        {
            return _repository.Coach.GetAllAsync()
                .Select(coach => new CoachDTO
                {
                    Id = coach.Id,
                    Name = coach.Name,
                    TeamId = coach.TeamId
                })
                .ToList();
        }

        public List<CoachDTO> GetCoachesByTeam(int teamId)
        {
            return _repository.Coach.Find(coach => coach.TeamId == teamId)
                .Select(coach => new CoachDTO
                {
                    Id = coach.Id,
                    Name = coach.Name,
                    TeamId = coach.TeamId
                })
                .ToList();
        }

        public async Task AddCoach(AddCoachDTO coachDto)
        {
            await _repository.Team.GetByIdAsync(coachDto.TeamId);

            Coach coach = new Coach
            {
                Name = coachDto.Name,
                TeamId = coachDto.TeamId
            };

            _repository.Coach.Create(coach);
            await _repository.SaveAsync();
        }

        public async Task UpdateCoach(UpdateCoachDTO coachDto)
        {
            Coach coach = await _repository.Coach.GetByIdAsync(coachDto.Id);
            await _repository.Team.GetByIdAsync(coachDto.TeamId);

            coach.Name = coachDto.Name;
            coach.TeamId = coachDto.TeamId;

            _repository.Coach.Update(coach);
            await _repository.SaveAsync();
        }

        public async Task DeleteCoach(int id)
        {
            Coach coach = await _repository.Coach.GetByIdAsync(id);
            _repository.Coach.Delete(coach);
            await _repository.SaveAsync();
        }
    }
}
