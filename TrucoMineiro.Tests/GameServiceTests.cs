using TrucoMineiro.API.Models;
using TrucoMineiro.API.Services;
using Xunit;

namespace TrucoMineiro.Tests
{
    /// <summary>
    /// Tests for the GameService
    /// </summary>
    public class GameServiceTests
    {
        [Fact]
        public void CreateGame_ShouldInitializeGameState()
        {
            // Arrange
            var gameService = new GameService();

            // Act
            var game = gameService.CreateGame();

            // Assert
            Assert.NotNull(game);
            Assert.Equal(4, game.Players.Count);
            Assert.Equal(1, game.Stakes);
            Assert.False(game.IsTrucoCalled);
            Assert.True(game.IsRaiseEnabled);
            Assert.Equal(1, game.CurrentHand);
            Assert.Equal(2, game.TeamScores.Count);
            Assert.Equal(0, game.TeamScores["Player's Team"]);
            Assert.Equal(0, game.TeamScores["Opponent Team"]);
            
            // Each player should have 3 cards
            foreach (var player in game.Players)
            {
                Assert.Equal(3, player.Hand.Count);
            }
            
            // One player should be active
            Assert.Single(game.Players.Where(p => p.IsActive));
            
            // One player should be the dealer
            Assert.Single(game.Players.Where(p => p.IsDealer));
        }

        [Fact]
        public void GetGame_ShouldReturnGame_WhenGameExists()
        {
            // Arrange
            var gameService = new GameService();
            var game = gameService.CreateGame();
            var gameId = game.GameId;

            // Act
            var retrievedGame = gameService.GetGame(gameId);

            // Assert
            Assert.NotNull(retrievedGame);
            Assert.Equal(gameId, retrievedGame.GameId);
        }

        [Fact]
        public void GetGame_ShouldReturnNull_WhenGameDoesNotExist()
        {
            // Arrange
            var gameService = new GameService();
            var nonExistentGameId = "non-existent-id";

            // Act
            var retrievedGame = gameService.GetGame(nonExistentGameId);

            // Assert
            Assert.Null(retrievedGame);
        }

        [Fact]
        public void PlayCard_ShouldReturnTrue_WhenValidMove()
        {
            // Arrange
            var gameService = new GameService();
            var game = gameService.CreateGame();
            
            // Find the active player
            var activePlayer = game.Players.First(p => p.IsActive);
            
            // Act
            var result = gameService.PlayCard(game.GameId, activePlayer.Id, 0);

            // Assert
            Assert.True(result);
            
            // The player should have one less card
            Assert.Equal(2, activePlayer.Hand.Count);
            
            // A card should have been played
            var playedCard = game.PlayedCards.First(pc => pc.PlayerId == activePlayer.Id);
            Assert.NotNull(playedCard.Card);
            
            // An action should have been logged
            Assert.Contains(game.ActionLog, a => a.Type == "card-played" && a.PlayerId == activePlayer.Id);
        }
    }
}
