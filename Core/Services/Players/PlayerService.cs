using Core.DTOs.Players;
using Data.Entities;
using Data.Repos;

namespace Core.Services.Players
{
    public class PlayerService : IPlayerService
    {
        private readonly IRepositoryManager _repository;

        public PlayerService(IRepositoryManager repository)
        {
            _repository = repository;
        }

        public List<PlayerDTO> GetPlayers()
        {
            List<PlayerPosition> positions = _repository.PlayerPosition.GetAllAsync().ToList();

            return _repository.Player.GetAllAsync()
                .Select(player => MapToDto(player, positions))
                .ToList();
        }

        public List<PlayerDTO> GetPlayersByTeam(int teamId)
        {
            List<PlayerPosition> positions = _repository.PlayerPosition.GetAllAsync().ToList();

            return _repository.Player.Find(player => player.TeamId == teamId)
                .Select(player => MapToDto(player, positions))
                .ToList();
        }

        public async Task AddPlayer(AddPlayerDTO playerDto)
        {
            await _repository.Team.GetByIdAsync(playerDto.TeamId);
            await _repository.PlayerPosition.GetByIdAsync(playerDto.PositionId);

            bool numberTaken = _repository.Player
                .Find(player => player.TeamId == playerDto.TeamId && player.Number == playerDto.Number)
                .Any();
            if (numberTaken)
            {
                throw new InvalidOperationException($"Jersey number {playerDto.Number} is already assigned on this team.");
            }

            Player player = new Player
            {
                Name = playerDto.Name,
                Number = playerDto.Number,
                TeamId = playerDto.TeamId,
                PositionId = playerDto.PositionId
            };

            _repository.Player.Create(player);
            await _repository.SaveAsync();
        }

        public async Task UpdatePlayer(UpdatePlayerDTO playerDto)
        {
            Player player = await _repository.Player.GetByIdAsync(playerDto.Id);
            await _repository.Team.GetByIdAsync(playerDto.TeamId);
            await _repository.PlayerPosition.GetByIdAsync(playerDto.PositionId);

            bool numberTaken = _repository.Player
                .Find(existingPlayer =>
                    existingPlayer.TeamId == playerDto.TeamId &&
                    existingPlayer.Number == playerDto.Number &&
                    existingPlayer.Id != playerDto.Id)
                .Any();
            if (numberTaken)
            {
                throw new InvalidOperationException($"Jersey number {playerDto.Number} is already assigned on this team.");
            }

            player.Name = playerDto.Name;
            player.Number = playerDto.Number;
            player.TeamId = playerDto.TeamId;
            player.PositionId = playerDto.PositionId;

            _repository.Player.Update(player);
            await _repository.SaveAsync();
        }

        public async Task DeletePlayer(int id)
        {
            Player player = await _repository.Player.GetByIdAsync(id);
            _repository.Player.Delete(player);
            await _repository.SaveAsync();
        }

        private static PlayerDTO MapToDto(Player player, List<PlayerPosition> positions)
        {
            return new PlayerDTO
            {
                Id = player.Id,
                Name = player.Name,
                Number = player.Number,
                TeamId = player.TeamId,
                PositionId = player.PositionId,
                PositionName = positions.FirstOrDefault(position => position.Id == player.PositionId)?.Name,
                ExternalPlayerId = player.ExternalPlayerId
            };
        }
    }
}
