using Core.DTOs.Stadiums;
using Data.Repos;
using Microsoft.AspNetCore.Http;
using Data.Entities;
using System.Text;

namespace Core.Services.Stadiums
{
    public class StadiumService : IStadiumService
    {
        private readonly string[] _permittedExtensions = { ".csv" };
        private const long _maxFileSize = 2 * 1024 * 1024;
        private readonly IRepositoryManager _repository;

        public StadiumService(IRepositoryManager repository)
        {
            _repository = repository;
        }
        public async Task AddStadium(StadiumDTO stadiumDto)
        {
            var stadium = new Stadium
            {
                Name = stadiumDto.Name,
                CityId = stadiumDto.CityId,
            };
            _repository.Stadium.Create(stadium);
            await _repository.SaveAsync();
        }
        public async Task UpdateStadium(UpdateStadiumDto updatedStadium)
        {
            if (updatedStadium == null)
            {
                throw new ArgumentNullException(nameof(updatedStadium), "Updated stadium data cannot be null.");
            }
            if (updatedStadium.Id <= 0)
            {
                throw new Exception("ID cannot be less than 1");
            }


            var stadium = _repository.Stadium.Find(e => e.Id == updatedStadium.Id).FirstOrDefault();
            if (stadium == null)
            {
                throw new KeyNotFoundException($"Stadium with ID {updatedStadium.Id} was not found.");
            }

            var cityExists = _repository.City.Find(e => e.Id == updatedStadium.CityId).Any();
            if (!cityExists)
            {
                throw new KeyNotFoundException($"City with ID {updatedStadium.CityId} was not found.");
            }

            stadium.Name = updatedStadium.Name;
            stadium.CityId = updatedStadium.CityId;

            _repository.Stadium.Update(stadium);
            await _repository.SaveAsync();
        }
        public List<StadiumDTO> GetStadiums()
        {
            var stadiums = _repository.Stadium.GetAllAsync();
            var stadiumDto = stadiums.Select(e => new StadiumDTO
            {
                Id = e.Id,
                Name = e.Name,
                CityId = e.CityId,
            }).ToList();
            return stadiumDto;
        }

        public async Task LoadStadiumsFromCsv(IFormFile file)
        {
            if (file == null || file.Length == 0)
            {
                throw new ArgumentException("File is empty.");
            }

            string extension = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (string.IsNullOrEmpty(extension) || !_permittedExtensions.Contains(extension))
            {
                throw new ArgumentException("Unsupported file type. Only .csv files are allowed.");
            }

            if (file.Length > _maxFileSize)
            {
                throw new ArgumentException("File size exceeds the 2 MB limit.");
            }

            using (StreamReader reader = new StreamReader(file.OpenReadStream(), Encoding.UTF8))
            {
                await reader.ReadLineAsync();
                HashSet<string> importedStadiumNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

                string? line;
                while ((line = await reader.ReadLineAsync()) != null)
                {
                    string[] values = line.Split(',');
                    if (values.Length < 2)
                    {
                        continue;
                    }

                    string stadiumName = values[0].Trim();
                    string cityName = values[1].Trim();

                    if (string.IsNullOrWhiteSpace(stadiumName) || string.IsNullOrWhiteSpace(cityName))
                    {
                        continue;
                    }

                    if (!importedStadiumNames.Add(stadiumName))
                    {
                        continue;
                    }

                    City? city = _repository.City.Find(c => c.Name == cityName).FirstOrDefault();
                    if (city == null)
                    {
                        throw new KeyNotFoundException($"City '{cityName}' was not found. Load cities before importing stadiums.");
                    }

                    bool stadiumExists = _repository.Stadium.Find(s => s.Name == stadiumName).Any();
                    if (!stadiumExists)
                    {
                        _repository.Stadium.Create(new Stadium { Name = stadiumName, CityId = city.Id });
                    }
                }

                await _repository.SaveAsync();
            }
        }
    }
}
