using Core.DTOs.Cities;
using Data.Entities;
using Data.Repos;
using Microsoft.AspNetCore.Http;
using System.Text;

namespace Core.Services.Cities
{
    public class CityService : ICityService

    {
        private readonly string[] _permittedExtensions = { ".csv" };
        private const long _maxFileSize = 2 * 1024 * 1024; // 2 MB
        private readonly IRepositoryManager _repository;

        public List<CityDTO> Cities => throw new NotImplementedException();

        public CityService(IRepositoryManager repository)
        {
            _repository = repository;
        }
        public List<CityDTO> GetAllCities()
        {
            var cities = _repository.City.GetAllAsync();
            var cityDto = cities.Select(e => new CityDTO
            {
                Name = e.Name,

            }).ToList();
            return cityDto;
        }

        public async Task AddCities(CityDTO citiesDto)
        {
            var city = new City
            {
                Name = citiesDto.Name,
                CountryId = citiesDto.CountryId

            };
            _repository.City.Create(city);
            await _repository.SaveAsync();
        }

        public async Task LoadCitiesFromCsv(IFormFile file, int CountryId)
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

                    if (values.Length > 0 && !string.IsNullOrWhiteSpace(values[0]))
                    {
                        var cities = _repository.City.Find(e => e.Name == values[0].Trim());
                        if (cities == null || cities.FirstOrDefault() == null)
                            _repository.City.Create(new City { Name= values[0].Trim(), CountryId= CountryId });
                    }

                }
                await _repository.SaveAsync();
            }
        }
    }
}
