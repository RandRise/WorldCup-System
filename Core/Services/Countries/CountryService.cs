using Core.DTOs.Countries;
using Data.Entities;
using Data.Repos;
using Microsoft.AspNetCore.Http;
using OfficeOpenXml;
using System.Text;

namespace Core.Services.Countries
{
    public class CountryService : ICountryService
    {
        private readonly string[] _permittedExtensions = { ".csv" };
        private const long _maxFileSize = 2 * 1024 * 1024; // 2 MB
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

        public async Task LoadCountriesFromExcel(IFormFile file)

        {

            if (file == null || file.Length == 0)
            {
                throw new Exception("File is empty.");
            }

            var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (string.IsNullOrEmpty(ext) || !_permittedExtensions.Contains(ext))
            {
                throw new Exception("Unsupported file type.");
            }

            if (file.Length > _maxFileSize)
            {
                throw new Exception("File size exceeds limit.");
            }

            using (var reader = new StreamReader(file.OpenReadStream(), Encoding.UTF8))
            {
                await reader.ReadLineAsync();

                string line;
                while ((line = await reader.ReadLineAsync()) != null)
                {
                    var values = line.Split(',');

                    if (values.Length > 0 && !string.IsNullOrWhiteSpace(values[1]))
                    {
                        var countries = _repository.Country.Find(e => e.Name == values[1].Trim());
                        if (countries == null || countries.FirstOrDefault() == null)
                            _repository.Country.Create(new Country { Name= values[1].Trim() });
                    }

                }
                await _repository.SaveAsync();
            }


        }
    }
}
