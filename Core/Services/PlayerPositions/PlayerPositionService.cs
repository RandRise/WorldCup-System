using Core.DTOs.PlayerPositions;
using Data.Repos;

namespace Core.Services.PlayerPositions
{
    public class PlayerPositionService : IPlayerPositionService
    {
        private readonly IRepositoryManager _repository;

        public PlayerPositionService(IRepositoryManager repository)
        {
            _repository = repository;
        }

        public List<PlayerPositionDTO> GetPlayerPositions()
        {
            return _repository.PlayerPosition.GetAllAsync()
                .Select(position => new PlayerPositionDTO
                {
                    Id = position.Id,
                    Name = position.Name
                })
                .ToList();
        }
    }
}
