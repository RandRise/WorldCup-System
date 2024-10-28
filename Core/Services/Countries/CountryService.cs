using Core.DTOs.Countries;
using Data.Entities;
using Data.Repos;
using OfficeOpenXml;

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
            var country = new Country
            {
                Name = countryDto.Name
            };
            _repository.Country.Create(country);
            await _repository.SaveAsync();
        }

        public List<CountryDTO> GetAllCountries()
        {
            var countries = _repository.Country.GetAllAsync();
            var countryDto = countries.Select(e => new CountryDTO
            {
                Id = e.Id,
                Name = e.Name,
            }).ToList();
            return countryDto;
        }

        public async Task LoadCountriesFromExcel(string filePath)
        {
            var countriesList = new List<CountryDTO>();

            using (var reader = new StreamReader(filePath))
            {
                await reader.ReadLineAsync();

                string line;
                while ((line = await reader.ReadLineAsync()) != null)
                {
                    var values = line.Split(','); 

                    if (values.Length > 0 && !string.IsNullOrWhiteSpace(values[1]))
                    {
                        var countryDto = new CountryDTO
                        {
                            Name = values[1].Trim()
                        };
                        countriesList.Add(countryDto);
                    }
                }
            }

            foreach (var country in countriesList)
            {
                await AddCountries(country);
            }
        }
    }
}
