using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using TrucoMineiro.API.Domain.EventHandlers;
using TrucoMineiro.API.Domain.Events;
using TrucoMineiro.API.Domain.Events.GameEvents;
using TrucoMineiro.API.Domain.Interfaces;
using TrucoMineiro.API.Domain.Models;
using TrucoMineiro.Tests.TestUtilities;

namespace TrucoMineiro.Tests.EventHandlers
{
    public class AutomaticHandStartTests
    {
        private readonly Mock<IGameRepository> _mockGameRepository;
        private readonly Mock<IEventPublisher> _mockEventPublisher;
        private readonly Mock<IGameStateManager> _mockGameStateManager;
        private readonly Mock<IHandResolutionService> _mockHandResolutionService;
        private readonly Mock<IConfiguration> _mockConfiguration;
        private readonly Mock<ILogger<RoundFlowEventHandler>> _mockLogger;
        private readonly RoundFlowEventHandler _handler;

        public AutomaticHandStartTests()
        {
            _mockGameRepository = new Mock<IGameRepository>();
            _mockEventPublisher = new Mock<IEventPublisher>();
            _mockGameStateManager = new Mock<IGameStateManager>();
            _mockHandResolutionService = new Mock<IHandResolutionService>();
            _mockConfiguration = new Mock<IConfiguration>();
            _mockLogger = new Mock<ILogger<RoundFlowEventHandler>>();

            _handler = new RoundFlowEventHandler(
                _mockGameRepository.Object,
                _mockEventPublisher.Object,
                _mockGameStateManager.Object,
                _mockHandResolutionService.Object,
                _mockConfiguration.Object,
                _mockLogger.Object);
        }

        [Fact]
        public async Task HandleAsync_WhenAllCardsPlayedAndHandComplete_ShouldPublishHandCompletedEvent()
        {
            // Arrange
            var gameId = Guid.NewGuid();
            var game = CreateGameWithAllCardsPlayed();
            var cardPlayedEvent = CreateCardPlayedEvent(gameId, game);

            _mockGameRepository.Setup(x => x.GetGameAsync(gameId.ToString()))
                .ReturnsAsync(game);

            _mockGameStateManager.Setup(x => x.IsRoundComplete(game))
                .Returns(true);

            _mockHandResolutionService.Setup(x => x.DetermineRoundWinner(It.IsAny<List<PlayedCard>>(), It.IsAny<List<Player>>()))
                .Returns(game.Players[0]); // Team 1 wins

            _mockHandResolutionService.Setup(x => x.GetHandWinner(game))
                .Returns(Team.PlayerTeam);

            // Setup RoundWinners to indicate Team 1 won 2 rounds (hand complete)
            game.RoundWinners.AddRange(new[] { 1, 1 }); // Team 1 won rounds 1 and 2

            // Act
            await _handler.HandleAsync(cardPlayedEvent, CancellationToken.None);

            // Assert
            _mockEventPublisher.Verify(x => x.PublishAsync(
                It.Is<HandCompletedEvent>(e => 
                    e.GameId == gameId &&
                    e.Hand == game.CurrentHand &&
                    e.WinningTeam == Team.PlayerTeam &&
                    e.PointsAwarded == game.Stakes &&
                    e.GameState == game),
                It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task HandleAsync_WhenAllCardsPlayedButHandNotComplete_ShouldNotPublishHandCompletedEvent()
        {
            // Arrange
            var gameId = Guid.NewGuid();
            var game = CreateGameWithAllCardsPlayed();
            var cardPlayedEvent = CreateCardPlayedEvent(gameId, game);

            _mockGameRepository.Setup(x => x.GetGameAsync(gameId.ToString()))
                .ReturnsAsync(game);

            _mockGameStateManager.Setup(x => x.IsRoundComplete(game))
                .Returns(true);

            _mockHandResolutionService.Setup(x => x.DetermineRoundWinner(It.IsAny<List<PlayedCard>>(), It.IsAny<List<Player>>()))
                .Returns(game.Players[0]);

            _mockHandResolutionService.Setup(x => x.GetHandWinner(game))
                .Returns(() => null); // Explicitly use a lambda to resolve ambiguity

            // Only 1 round won, hand not complete
            game.RoundWinners.Add(1);

            // Act
            await _handler.HandleAsync(cardPlayedEvent, CancellationToken.None);

            // Assert - Should not publish HandCompletedEvent
            _mockEventPublisher.Verify(x => x.PublishAsync(
                It.IsAny<HandCompletedEvent>(),
                It.IsAny<CancellationToken>()), Times.Never);
        }        [Fact]
        public async Task HandleAsync_WhenHandCompleteWithNoWinner_ShouldLogError()
        {
            // Arrange
            var gameId = Guid.NewGuid();
            var game = CreateGameWithAllCardsPlayed();
            var cardPlayedEvent = CreateCardPlayedEvent(gameId, game);

            _mockGameRepository.Setup(x => x.GetGameAsync(gameId.ToString()))
                .ReturnsAsync(game);

            _mockGameStateManager.Setup(x => x.IsRoundComplete(game))
                .Returns(true);

            _mockHandResolutionService.Setup(x => x.DetermineRoundWinner(It.IsAny<List<PlayedCard>>(), It.IsAny<List<Player>>()))
                .Returns(game.Players[0]);

            _mockHandResolutionService.Setup(x => x.GetHandWinner(game))
                .Returns(() => null); // No winner determined

            game.RoundWinners.AddRange(new[] { 1, 1 }); // Should be complete but returns null

            // Act
            await _handler.HandleAsync(cardPlayedEvent, CancellationToken.None);

            // Assert - Should not publish HandCompletedEvent
            _mockEventPublisher.Verify(x => x.PublishAsync(
                It.IsAny<HandCompletedEvent>(),
                It.IsAny<CancellationToken>()), Times.Never);            // Verify error was logged
            _mockLogger.Verify(
                x => x.Log(
                    LogLevel.Error,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("no winning team could be determined")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once);
        }        private static GameState CreateGameWithAllCardsPlayed()
        {
            return TestGameFactory.CreateGameWithAllCardsPlayed();
        }

        private static GameState CreateGameWithSomeCardsRemaining()
        {
            return TestGameFactory.CreateGameWithSomeCardsRemaining();
        }

        private static CardPlayedEvent CreateCardPlayedEvent(Guid gameId, GameState game)
        {
            var player = game.Players[0];
            var card = new Card("A", "Spades");
            
            return new CardPlayedEvent(
                gameId,
                player.Id,
                card,                player,
                game.CurrentRound,
                game.CurrentHand,
                false,
                game);
        }
    }
}
