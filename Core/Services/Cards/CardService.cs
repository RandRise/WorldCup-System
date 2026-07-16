using Core.DTOs.Cards;
using Data.Entities;
using Data.Repos;

namespace Core.Services.Cards
{
    public class CardService : ICardService
    {
        private readonly IRepositoryManager _repository;

        public CardService(IRepositoryManager repository)
        {
            _repository = repository;
        }

        public List<CardDTO> GetCardsByMatch(int matchId)
        {
            Match? match = _repository.Match.Find(existingMatch => existingMatch.Id == matchId).FirstOrDefault();
            if (match == null)
            {
                return new List<CardDTO>();
            }

            List<TeamStats> stats = _repository.TeamStats.Find(teamStats => teamStats.MatchId == matchId).ToList();
            List<int> statIds = stats.Select(teamStats => teamStats.Id).ToList();
            if (statIds.Count == 0)
            {
                return new List<CardDTO>();
            }

            List<Card> cards = _repository.Card.Find(card => statIds.Contains(card.TeamStatsId)).ToList();
            List<Player> players = _repository.Player.GetAllAsync().ToList();

            return cards.Select(card =>
            {
                TeamStats? owningStats = stats.FirstOrDefault(teamStats => teamStats.Id == card.TeamStatsId);
                int minute = (int)Math.Floor((card.TimeIssued - match.Date).TotalMinutes);
                return new CardDTO
                {
                    Id = card.Id,
                    MatchId = matchId,
                    TeamId = owningStats?.TeamId ?? 0,
                    PlayerId = card.PlayerId,
                    PlayerName = players.FirstOrDefault(player => player.Id == card.PlayerId)?.Name,
                    Minute = minute < 0 ? 0 : minute,
                    Type = card.Type,
                    TypeName = CardTypeName(card.Type)
                };
            })
            .OrderBy(card => card.Minute)
            .ToList();
        }

        public async Task AddCard(AddCardDTO cardDto)
        {
            Match match = await _repository.Match.GetByIdAsync(cardDto.MatchId);

            if (!match.TeamOneId.HasValue || !match.TeamTwoId.HasValue)
            {
                throw new InvalidOperationException("Cannot record cards until both teams are set for this match.");
            }

            if (cardDto.TeamId != match.TeamOneId && cardDto.TeamId != match.TeamTwoId)
            {
                throw new InvalidOperationException("The specified team is not part of this match.");
            }

            Player player = await _repository.Player.GetByIdAsync(cardDto.PlayerId);
            if (player.TeamId != cardDto.TeamId)
            {
                throw new InvalidOperationException("The carded player must belong to the specified team.");
            }

            TeamStats stats = ResolveStats(cardDto.MatchId, cardDto.TeamId);

            Card card = new Card
            {
                PlayerId = cardDto.PlayerId,
                TeamStatsId = stats.Id,
                Type = cardDto.CardType,
                TimeIssued = match.Date.AddMinutes(cardDto.Minute)
            };

            _repository.Card.Create(card);
            await _repository.SaveAsync();
        }

        public async Task DeleteCard(int id)
        {
            Card card = await _repository.Card.GetByIdAsync(id);
            _repository.Card.Delete(card);
            await _repository.SaveAsync();
        }

        private TeamStats ResolveStats(int matchId, int teamId)
        {
            TeamStats? stats = _repository.TeamStats
                .Find(teamStats => teamStats.MatchId == matchId && teamStats.TeamId == teamId)
                .FirstOrDefault();
            if (stats == null)
            {
                throw new InvalidOperationException(
                    $"No team stats record exists for team {teamId} in match {matchId}.");
            }

            return stats;
        }

        private static string CardTypeName(int? type)
        {
            return type switch
            {
                1 => "Yellow",
                2 => "Red",
                _ => "Unknown"
            };
        }
    }
}
