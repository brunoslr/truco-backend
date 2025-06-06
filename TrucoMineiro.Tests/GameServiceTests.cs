using Microsoft.Extensions.Configuration;
using System.Collections.Generic;
using TrucoMineiro.API.Constants;
using TrucoMineiro.API.Models;
using TrucoMineiro.API.Services;
using TrucoMineiro.API.Domain.Interfaces;
using TrucoMineiro.API.Domain.Services;
using Moq;
using Xunit;

namespace TrucoMineiro.Tests
{
    /// <summary>
    /// Tests for the GameService
    /// </summary>
    public class GameServiceTests
    {
        private readonly IConfiguration _configuration;    public GameServiceTests()
    {
        // Set up test configuration
        var inMemorySettings = new Dictionary<string, string?> {
            {"FeatureFlags:DevMode", "false"},
        };

        _configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(inMemorySettings)
            .Build();
    }    private GameService CreateGameService(IConfiguration? config = null)
    {
        var configuration = config ?? _configuration;
          // Create a dictionary to store created games (simulating repository storage)
        var gameStorage = new Dictionary<string, GameState>();
        
        // Create mock services
        var mockGameStateManager = new Mock<IGameStateManager>();
        var mockGameRepository = new Mock<IGameRepository>();
        var mockHandResolutionService = new Mock<IHandResolutionService>();
        var mockTrucoRulesEngine = new Mock<ITrucoRulesEngine>();
        var mockAIPlayerService = new Mock<IAIPlayerService>();
        var mockScoreCalculationService = new Mock<IScoreCalculationService>();

        // Configure mock GameStateManager to return a valid GameState and store it
        mockGameStateManager.Setup(x => x.CreateGameAsync(It.IsAny<string>()))
            .ReturnsAsync((string playerName) => {
                var gameState = CreateValidGameState(playerName);
                gameStorage[gameState.GameId] = gameState;
                return gameState;
            });
        
        mockGameStateManager.Setup(x => x.CreateGameAsync(null))
            .ReturnsAsync(() => {
                var gameState = CreateValidGameState();
                gameStorage[gameState.GameId] = gameState;
                return gameState;
            });

        // Configure mock GameRepository to return games from storage
        mockGameRepository.Setup(x => x.GetGameAsync(It.IsAny<string>()))
            .ReturnsAsync((string gameId) => gameStorage.ContainsKey(gameId) ? gameStorage[gameId] : null);

        mockGameRepository.Setup(x => x.SaveGameAsync(It.IsAny<GameState>()))
            .ReturnsAsync((GameState gameState) => {
                gameStorage[gameState.GameId] = gameState;
                return true;
            });

        // Configure mock ScoreCalculationService
        mockScoreCalculationService.Setup(x => x.IsGameComplete(It.IsAny<GameState>()))
            .Returns(false);

        // Configure mock TrucoRulesEngine
        mockTrucoRulesEngine.Setup(x => x.CalculateHandPoints(It.IsAny<GameState>()))
            .Returns(1);

        return new GameService(
            mockGameStateManager.Object,
            mockGameRepository.Object,
            mockHandResolutionService.Object,
            mockTrucoRulesEngine.Object,
            mockAIPlayerService.Object,
            mockScoreCalculationService.Object,
            configuration);
    }

    private GameState CreateValidGameState(string? playerName = null)
    {
        var gameState = new GameState();
        gameState.InitializeGame(playerName ?? "TestPlayer");
        return gameState;
    }[Fact]
        public void CreateGame_ShouldInitializeGameState()
        {
            // Arrange
            var gameService = CreateGameService();

            // Act
            var game = gameService.CreateGame();

            // Assert
            Assert.NotNull(game);
            Assert.Equal(4, game.Players.Count);
            Assert.Equal(TrucoConstants.Stakes.Initial, game.Stakes);
            Assert.False(game.IsTrucoCalled);
            Assert.True(game.IsRaiseEnabled);
            Assert.Equal(1, game.CurrentHand);            Assert.Equal(2, game.TeamScores.Count);
            Assert.Equal(0, game.TeamScores[TrucoConstants.Teams.PlayerTeam]);
            Assert.Equal(0, game.TeamScores[TrucoConstants.Teams.OpponentTeam]);
            
            // Each player should have 3 cards
            foreach (var player in game.Players)
            {
                Assert.Equal(3, player.Hand.Count);
            }
              // One player should be active
            Assert.Single(game.Players, p => p.IsActive);
            
            // One player should be the dealer
            Assert.Single(game.Players, p => p.IsDealer);
        }        [Fact]
        public void GetGame_ShouldReturnGame_WhenGameExists()
        {
            // Arrange
            var gameService = CreateGameService();
            var game = gameService.CreateGame();
            var gameId = game.GameId;

            // Act
            var retrievedGame = gameService.GetGame(gameId);

            // Assert
            Assert.NotNull(retrievedGame);
            Assert.Equal(gameId, retrievedGame.GameId);
        }        [Fact]
        public void GetGame_ShouldReturnNull_WhenGameDoesNotExist()
        {
            // Arrange
            var gameService = CreateGameService();
            var nonExistentGameId = "non-existent-id";

            // Act
            var retrievedGame = gameService.GetGame(nonExistentGameId);

            // Assert
            Assert.Null(retrievedGame);
        }        [Fact]
        public void PlayCard_ShouldReturnTrue_WhenValidMove()
        {
            // Arrange
            var gameService = CreateGameService();
            var game = gameService.CreateGame();
            
            // Find the active player
            var activePlayer = game.Players.First(p => p.IsActive);
            
            // Act
            var result = gameService.PlayCard(game.GameId, activePlayer.Seat, 0);

            // Assert
            Assert.True(result);
            
            // The player should have one less card
            Assert.Equal(2, activePlayer.Hand.Count);
              // A card should have been played
            var playedCard = game.PlayedCards.First(pc => pc.PlayerSeat == activePlayer.Seat);
            Assert.NotNull(playedCard.Card);
            
            // An action should have been logged
            Assert.Contains(game.ActionLog, a => a.Type == "card-played" && a.PlayerSeat == activePlayer.Seat);
        }
    }
}
