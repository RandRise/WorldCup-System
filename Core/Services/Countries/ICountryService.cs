using Core.DTOs.Countries;

namespace Core.Services.Countries
{
    public interface ICountryService
    {
        public List<CountryDTO> GetAllCountries();
        Task AddCountries(CountryDTO countryDto);
        Task LoadCountriesFromExcel(string v);
    }
}
