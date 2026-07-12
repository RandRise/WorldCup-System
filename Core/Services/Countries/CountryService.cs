using Core.DTOs.Countries;
using Data.Entities;
using Data.Repos;

namespace Core.Services.Countries
{
    public class CountryService : ICountryService
    {
        private readonly IRepositoryManager _repository;
        public CountryService(IRepositoryManager repository)
        {
            _repository = repository;
        }
        public async Task AddCountries(CountryDTO countryDto)
        {
            Country country = new Country
            {
                Name = countryDto.Name
            };
            _repository.Country.Create(country);
            await _repository.SaveAsync();
        }

        public List<CountryDTO> GetAllCountries()
        {
            IQueryable<Country> countries = _repository.Country.GetAllAsync();
            List<CountryDTO> countryDto = countries.Select(e => new CountryDTO
            {
                Id = e.Id,
                Name = e.Name,
            }).ToList();
            return countryDto;
        }
    }
}
