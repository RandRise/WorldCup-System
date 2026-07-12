using Core.DTOs.Cities;
using Core.Services.Cities;
using Microsoft.AspNetCore.Mvc;

namespace WorldCup_System.Controllers
{
    [ApiController]
    [Route("[controller]/[action]")]
    public class CityController : Controller
    {
        private readonly ICityService _cityService;

        public CityController(ICityService cityService)
        {
            _cityService = cityService;
        }

        [HttpGet]
        public List<CityDTO> GetAllCities()
        {
            List<CityDTO> cities = _cityService.GetAllCities();
            return cities;
        }
    }
}
