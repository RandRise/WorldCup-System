using Core.DTOs.Seeding;

namespace Core.Services.Seeding
{
    public interface IDemoSeedService
    {
        Task<DemoSeedResultDTO> SeedWorldCup2026DemoAsync();
    }
}
