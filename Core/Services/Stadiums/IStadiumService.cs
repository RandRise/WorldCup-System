using Core.DTOs.Stadiums;

namespace Core.Services.Stadiums
{
    public interface IStadiumService
    {
        public List<StadiumDTO> GetStadiums();
        Task AddStadium(StadiumDTO stadium);
        Task UpdateStadium(UpdateStadiumDto stadium);
    }
}
