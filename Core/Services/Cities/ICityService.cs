using Core.DTOs.Cities;
using Microsoft.AspNetCore.Http;

namespace Core.Services.Cities
{
    public interface ICityService
    {
        public List<CityDTO> GetAllCities();
        Task AddCities(CityDTO citiesDto);
        
        Task LoadCitiesFromCsv(IFormFile file, int CountryId);
    }
}
