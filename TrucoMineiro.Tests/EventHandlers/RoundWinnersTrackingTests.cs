using Moq;
using TrucoMineiro.API.Domain.EventHandlers;
using TrucoMineiro.API.Domain.Events.GameEvents;
using TrucoMineiro.API.Domain.Interfaces;
using TrucoMineiro.API.Domain.Models;

namespace TrucoMineiro.Tests.EventHandlers
{
    public class RoundWinnersTrackingTests
    {
        private readonly Mock<IGameRepository> _mockRepository;
        private readonly RoundCleanupEventHandler _handler;

        public RoundWinnersTrackingTests()
        {
            _mockRepository = new Mock<IGameRepository>();
            _handler = new RoundCleanupEventHandler(_mockRepository.Object);
        }

        [Fact]
        public async Task HandleAsync_ShouldUpdateRoundWinners_WhenRoundCompleted()
        {
            // Arrange
            var game = new GameState();
            game.InitializeGame("TestPlayer");
            game.CurrentRound = 1;
            
            // Create a winning player from Team 1 (seat 0)
            var winningPlayer = game.Players[0]; // Seat 0 = Team 1
            
            var roundCompletedEvent = new RoundCompletedEvent(
                Guid.NewGuid(),
                1, // round
                1, // hand
                winningPlayer,
                new List<Card>(),
                new Dictionary<Guid, int>(),
                game,
                false // not a draw
            );

            // Act
            await _handler.HandleAsync(roundCompletedEvent);

            // Assert
            Assert.Single(game.RoundWinners);
            Assert.Equal(1, game.RoundWinners[0]); // Team 1 should have won
            
            // Verify the repository was called to save the game
            _mockRepository.Verify(r => r.SaveGameAsync(game), Times.Once);
        }

        [Fact]
        public async Task HandleAsync_ShouldUpdateRoundWinners_ForTeam2Winner()
        {
            // Arrange
            var game = new GameState();
            game.InitializeGame("TestPlayer");
            game.CurrentRound = 1;
            
            // Create a winning player from Team 2 (seat 1)
            var winningPlayer = game.Players[1]; // Seat 1 = Team 2
            
            var roundCompletedEvent = new RoundCompletedEvent(
                Guid.NewGuid(),
                1, // round
                1, // hand
                winningPlayer,
                new List<Card>(),
                new Dictionary<Guid, int>(),
                game,
                false // not a draw
            );

            // Act
            await _handler.HandleAsync(roundCompletedEvent);

            // Assert
            Assert.Single(game.RoundWinners);
            Assert.Equal(2, game.RoundWinners[0]); // Team 2 should have won
        }

        [Fact]
        public async Task HandleAsync_ShouldNotAddRoundWinner_WhenRoundIsDraw()
        {
            // Arrange
            var game = new GameState();
            game.InitializeGame("TestPlayer");
            game.CurrentRound = 1;
            
            var roundCompletedEvent = new RoundCompletedEvent(
                Guid.NewGuid(),
                1, // round
                1, // hand
                null, // no winner (draw)
                new List<Card>(),
                new Dictionary<Guid, int>(),
                game,
                true // is a draw
            );

            // Act
            await _handler.HandleAsync(roundCompletedEvent);

            // Assert
            Assert.Empty(game.RoundWinners); // No winner should be added for a draw
        }

        [Fact]
        public async Task HandleAsync_ShouldTrackMultipleRoundWinners()
        {
            // Arrange
            var game = new GameState();
            game.InitializeGame("TestPlayer");
            game.CurrentRound = 1;
            
            // First round - Team 1 wins
            var firstRoundEvent = new RoundCompletedEvent(
                Guid.NewGuid(),
                1,
                1,
                game.Players[0], // Team 1
                new List<Card>(),
                new Dictionary<Guid, int>(),
                game,
                false
            );

            await _handler.HandleAsync(firstRoundEvent);

            // Second round - Team 2 wins
            game.CurrentRound = 2;
            var secondRoundEvent = new RoundCompletedEvent(
                Guid.NewGuid(),
                2,
                1,
                game.Players[1], // Team 2
                new List<Card>(),
                new Dictionary<Guid, int>(),
                game,
                false
            );

            // Act
            await _handler.HandleAsync(secondRoundEvent);

            // Assert
            Assert.Equal(2, game.RoundWinners.Count);
            Assert.Equal(1, game.RoundWinners[0]); // Team 1 won round 1
            Assert.Equal(2, game.RoundWinners[1]); // Team 2 won round 2
        }
    }
}
