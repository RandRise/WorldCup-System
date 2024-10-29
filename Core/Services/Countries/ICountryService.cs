using Core.DTOs.Countries;
using Microsoft.AspNetCore.Http;

namespace Core.Services.Countries
{
    public interface ICountryService
    {
        public List<CountryDTO> GetAllCountries();
        Task AddCountries(CountryDTO countryDto);
        Task LoadCountriesFromExcel(IFormFile file);
    }
}
