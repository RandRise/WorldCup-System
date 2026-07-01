using Core.DTOs.Cards;

namespace Core.Services.Cards
{
    public interface ICardService
    {
        List<CardDTO> GetCardsByMatch(int matchId);
        Task AddCard(AddCardDTO cardDto);
        Task DeleteCard(int id);
    }
}
