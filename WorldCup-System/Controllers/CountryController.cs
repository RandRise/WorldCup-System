using Core.DTOs.Countries;
using Core.Services.Countries;
using Microsoft.AspNetCore.Mvc;

namespace WorldCup_System.Controllers
{
    [ApiController]
    [Route("[controller]/[action]")]
    public class CountryController : Controller
    {
        private readonly ICountryService _countryService;


        public CountryController(ICountryService countryService)
        {
            _countryService = countryService;
        }

        [HttpGet]
        public List<CountryDTO> GetAllCountries()
        {
            var countries = _countryService.GetAllCountries();
            return countries;
        }


        [HttpPost]
        public async Task<IActionResult> AddCountries([FromForm] UploadCountryDto uploadCountry)
        {

            try
            {

                await _countryService.LoadCountriesFromExcel(uploadCountry.File);
                return Ok("File uploaded and processed successfully.");
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }
    }
}
