using Core.DTOs.WorldCups;
using Data.Repos;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.Services.WorldCups
{
    public class WorldCupService : IWorldCupService
    {
        private readonly IRepositoryManager _repository;
        public WorldCupService(IRepositoryManager repository)
        {
            _repository=repository;
        }
        public List<WorldCupDTO> GetAllWorldCups()
        {
            var worldCups = _repository.WorldCup.GetAllAsync();
            var worldCupDto = worldCups.Select(e => new WorldCupDTO
            {
                Year = e.Year,
            }).ToList();
            return worldCupDto;
        }
        public async Task CreateNewWorldCup(WorldCupDTO worldcup)
        {
            _repository.WorldCup.Create(new Data.Entities.WorldCup { Year = worldcup.Year });
            await _repository.SaveAsync();
        }
    }
}
