using Core.DTOs.Cities;
using Core.DTOs.Countries;
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
            var cities = _cityService.GetAllCities();
            return cities;
        }

        [HttpPost]
        public async Task<IActionResult> AddCities([FromForm] UploadCityDto uploadCity)
        {

            try
            {

                await _cityService.LoadCitiesFromCsv(uploadCity.File, uploadCity.CountryId);
                return Ok("File uploaded and processed successfully.");
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }
    }
}
