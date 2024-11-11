using Core.DTOs.WorldCups;

namespace Core.Services.WorldCups
{
    public interface IWorldCupService
    {
        public List<WorldCupDTO> GetAllWorldCups();
        Task CreateNewWorldCup(WorldCupDTO worldCup);
           
    }
}
