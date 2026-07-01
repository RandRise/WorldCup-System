using Core.DTOs.Cards;
using Core.Services.Cards;
using Microsoft.AspNetCore.Mvc;
using Moq;
using WorldCup_System.Controllers;

namespace WorldCup_System.Tests.Controllers
{
    public class CardControllerTests
    {
        private readonly Mock<ICardService> _cardServiceMock;
        private readonly CardController _cardController;

        public CardControllerTests()
        {
            _cardServiceMock = new Mock<ICardService>();
            _cardController = new CardController(_cardServiceMock.Object);
        }

        [Fact]
        public void GetCardsByMatch_ReturnsCardsFromService()
        {
            List<CardDTO> cards = new List<CardDTO>
            {
                new CardDTO { Id = 1, MatchId = 5, TeamId = 10, PlayerId = 1, PlayerName = "Neymar", Minute = 33, Type = 1, TypeName = "Yellow" }
            };

            _cardServiceMock.Setup(cardService => cardService.GetCardsByMatch(5)).Returns(cards);

            List<CardDTO> result = _cardController.GetCardsByMatch(5);

            Assert.Single(result);
            Assert.Equal("Yellow", result[0].TypeName);
            _cardServiceMock.Verify(cardService => cardService.GetCardsByMatch(5), Times.Once);
        }

        [Fact]
        public async Task AddCard_WhenServiceSucceeds_ReturnsOk()
        {
            AddCardDTO addCardDto = new AddCardDTO { MatchId = 1, TeamId = 10, PlayerId = 1, Minute = 33, CardType = 1 };

            _cardServiceMock.Setup(cardService => cardService.AddCard(addCardDto)).Returns(Task.CompletedTask);

            IActionResult actionResult = await _cardController.AddCard(addCardDto);

            OkObjectResult okResult = Assert.IsType<OkObjectResult>(actionResult);
            Assert.Equal("Card recorded successfully.", okResult.Value);
        }

        [Fact]
        public async Task AddCard_WhenPlayerNotOnTeam_ReturnsBadRequest()
        {
            AddCardDTO addCardDto = new AddCardDTO { MatchId = 1, TeamId = 10, PlayerId = 1, Minute = 10, CardType = 2 };

            _cardServiceMock
                .Setup(cardService => cardService.AddCard(addCardDto))
                .ThrowsAsync(new InvalidOperationException("The carded player must belong to the specified team."));

            IActionResult actionResult = await _cardController.AddCard(addCardDto);

            BadRequestObjectResult badRequestResult = Assert.IsType<BadRequestObjectResult>(actionResult);
            Assert.Contains("carded player must belong", badRequestResult.Value?.ToString());
        }

        [Fact]
        public async Task DeleteCard_WhenServiceSucceeds_ReturnsOk()
        {
            _cardServiceMock.Setup(cardService => cardService.DeleteCard(1)).Returns(Task.CompletedTask);

            IActionResult actionResult = await _cardController.DeleteCard(1);

            OkObjectResult okResult = Assert.IsType<OkObjectResult>(actionResult);
            Assert.Equal("Card deleted successfully.", okResult.Value);
        }

        [Fact]
        public async Task DeleteCard_WhenCardNotFound_ReturnsNotFound()
        {
            _cardServiceMock
                .Setup(cardService => cardService.DeleteCard(99))
                .ThrowsAsync(new KeyNotFoundException("Entity of type Card with id 99 was not found."));

            IActionResult actionResult = await _cardController.DeleteCard(99);

            NotFoundObjectResult notFoundResult = Assert.IsType<NotFoundObjectResult>(actionResult);
            Assert.Contains("99", notFoundResult.Value?.ToString());
        }
    }
}
