using Core.DTOs.Stadiums;
using Data.Repos;
using Microsoft.AspNetCore.Http;
using Data.Entities;
using System.Text;

namespace Core.Services.Stadiums
{
    public class StadiumService : IStadiumService
    {
        private readonly IRepositoryManager _repository;

        public StadiumService(IRepositoryManager repository)
        {
            _repository = repository;
        }
        public async Task AddStadium(StadiumDTO stadiumDto)
        {
            var stadium = new Stadium
            {
                Name = stadiumDto.Name,
                CityId = stadiumDto.CityId,
            };
            _repository.Stadium.Create(stadium);
            await _repository.SaveAsync();
        }
        public async Task UpdateStadium(UpdateStadiumDto updatedStadium)
        {
            if (updatedStadium == null)
            {
                throw new ArgumentNullException(nameof(updatedStadium), "Updated stadium data cannot be null.");
            }
            if (updatedStadium.Id <= 0)
            {
                throw new Exception("ID cannot be less than 1");
            }


            var stadium = _repository.Stadium.Find(e => e.Id == updatedStadium.Id).FirstOrDefault();
            if (stadium == null)
            {
                throw new KeyNotFoundException($"Stadium with ID {updatedStadium.Id} was not found.");
            }

            var cityExists = _repository.City.Find(e => e.Id == updatedStadium.CityId).Any();
            if (!cityExists)
            {
                throw new KeyNotFoundException($"City with ID {updatedStadium.CityId} was not found.");
            }

            stadium.Name = updatedStadium.Name;
            stadium.CityId = updatedStadium.CityId;

            _repository.Stadium.Update(stadium);
            await _repository.SaveAsync();
        }
        public List<StadiumDTO> GetStadiums()
        {
            var stadiums = _repository.Stadium.GetAllAsync();
            var stadiumDto = stadiums.Select(e => new StadiumDTO
            {
                Name = e.Name,
                CityId = e.CityId,
            }).ToList();
            return stadiumDto;
        }
    }
}
