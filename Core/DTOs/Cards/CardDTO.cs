using System.ComponentModel.DataAnnotations;

namespace Core.DTOs.Cards
{
    public class CardDTO
    {
        public int Id { get; set; }
        public int MatchId { get; set; }
        public int TeamId { get; set; }
        public int PlayerId { get; set; }
        public string? PlayerName { get; set; }
        public int Minute { get; set; }
        public int? Type { get; set; }
        public string? TypeName { get; set; }
    }

    public class AddCardDTO
    {
        [Range(1, int.MaxValue, ErrorMessage = "MatchId must be a positive value.")]
        public int MatchId { get; set; }

        [Range(1, int.MaxValue, ErrorMessage = "TeamId must be a positive value.")]
        public int TeamId { get; set; }

        [Range(1, int.MaxValue, ErrorMessage = "PlayerId must be a positive value.")]
        public int PlayerId { get; set; }

        [Range(0, 130, ErrorMessage = "Minute must be between 0 and 130.")]
        public int Minute { get; set; }

        [Range(1, 2, ErrorMessage = "CardType must be 1 (Yellow) or 2 (Red).")]
        public int CardType { get; set; }
    }
}
