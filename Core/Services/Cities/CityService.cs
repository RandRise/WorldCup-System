using Core.DTOs.Cities;
using Data.Entities;
using Data.Repos;

namespace Core.Services.Cities
{
    public class CityService : ICityService
    {
        private readonly IRepositoryManager _repository;

        public CityService(IRepositoryManager repository)
        {
            _repository = repository;
        }
        public List<CityDTO> GetAllCities()
        {
            IQueryable<City> cities = _repository.City.GetAllAsync();
            List<CityDTO> cityDto = cities.Select(e => new CityDTO
            {
                Id = e.Id,
                Name = e.Name,
                CountryId = e.CountryId
            }).ToList();
            return cityDto;
        }

        public async Task AddCities(CityDTO citiesDto)
        {
            City city = new City
            {
                Name = citiesDto.Name,
                CountryId = citiesDto.CountryId
            };
            _repository.City.Create(city);
            await _repository.SaveAsync();
        }
    }
}
