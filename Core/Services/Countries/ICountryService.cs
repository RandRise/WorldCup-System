using Core.DTOs.Countries;
using Data.Entities;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace Core.Services.Countries
{
    public interface ICountryService
    {
        public List<CountryDTO> GetAllCountries();
      
        Task AddCountries(CountryDTO countryDto);
        Task LoadCountriesFromExcel(IFormFile file);
    }
}
