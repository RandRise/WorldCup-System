using Core.DTOs.Seeding;

namespace Core.Services.Seeding
{
    public interface ITournamentDataResetService
    {
        Task<TournamentDataResetResultDTO> ClearTournamentDataAsync();
    }
}
