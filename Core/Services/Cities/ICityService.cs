using Core.DTOs.Cities;

namespace Core.Services.Cities
{
    public interface ICityService
    {
        public List<CityDTO> GetAllCities();
        Task AddCities(CityDTO citiesDto);
    }
}
