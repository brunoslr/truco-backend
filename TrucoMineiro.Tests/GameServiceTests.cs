using Microsoft.Extensions.Configuration;
using Moq;
using TrucoMineiro.API.Constants;
using TrucoMineiro.API.Domain.Events;
using TrucoMineiro.API.Domain.Interfaces;
using TrucoMineiro.API.Domain.Models;
using TrucoMineiro.API.Services;

namespace TrucoMineiro.Tests
{
    /// <summary>
    /// Tests for the GameService
    /// </summary>
    public class GameServiceTests
    {
        private readonly IConfiguration _configuration; public GameServiceTests()
        {
            // Set up test configuration
            var inMemorySettings = new Dictionary<string, string?> {
            {"FeatureFlags:DevMode", "false"},
        };

            _configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(inMemorySettings)
                .Build();
        }
        private GameService CreateGameService(IConfiguration? config = null)
        {
            var configuration = config ?? _configuration;
            // Create a dictionary to store created games (simulating repository storage)
            var gameStorage = new Dictionary<string, GameState>();            // Create mock services
            var mockGameStateManager = new Mock<IGameStateManager>();
            var mockGameRepository = new Mock<IGameRepository>();
            var mockGameFlowService = new Mock<IGameFlowService>();
            var mockTrucoRulesEngine = new Mock<ITrucoRulesEngine>();
            var mockAIPlayerService = new Mock<IAIPlayerService>();
            var mockScoreCalculationService = new Mock<IScoreCalculationService>();
            var mockEventPublisher = new Mock<IEventPublisher>();

            // Configure mock GameStateManager to return a valid GameState and store it
            mockGameStateManager.Setup(x => x.CreateGameAsync(It.IsAny<string>()))
                .ReturnsAsync((string playerName) =>
                {
                    var gameState = CreateValidGameState(playerName);
                    gameStorage[gameState.GameId] = gameState;
                    return gameState;
                });

            mockGameStateManager.Setup(x => x.CreateGameAsync(null))
                .ReturnsAsync(() =>
                {
                    var gameState = CreateValidGameState();
                    gameStorage[gameState.GameId] = gameState;
                    return gameState;
                });

            // Configure mock GameRepository to return games from storage
            mockGameRepository.Setup(x => x.GetGameAsync(It.IsAny<string>()))
                .ReturnsAsync((string gameId) => gameStorage.ContainsKey(gameId) ? gameStorage[gameId] : null);

            mockGameRepository.Setup(x => x.SaveGameAsync(It.IsAny<GameState>()))
                .ReturnsAsync((GameState gameState) =>
                {
                    gameStorage[gameState.GameId] = gameState;
                    return true;
                });

            // Configure mock ScoreCalculationService
            mockScoreCalculationService.Setup(x => x.IsGameComplete(It.IsAny<GameState>()))
                .Returns(false);        // Configure mock TrucoRulesEngine
            mockTrucoRulesEngine.Setup(x => x.CalculateHandPoints(It.IsAny<GameState>()))
                .Returns(1);        // Configure mock GameFlowService
            mockGameFlowService.Setup(x => x.PlayCard(It.IsAny<GameState>(), It.IsAny<int>(), It.IsAny<int>()))
                .Returns((GameState gameState, int playerSeat, int cardIndex) =>
                {
                    var player = gameState.Players[playerSeat];
                    if (cardIndex < 0 || cardIndex >= player.Hand.Count)
                        return false;

                    // Play the card
                    var card = player.Hand[cardIndex];
                    player.Hand.RemoveAt(cardIndex);

                    // Add to played cards
                    gameState.PlayedCards.Add(new PlayedCard(playerSeat, card));

                    // Add to the action log (like the real GameFlowService does)
                    gameState.ActionLog.Add(new ActionLogEntry("card-played")
                    {
                        PlayerSeat = playerSeat,
                        Card = $"{card.Value} of {card.Suit}"
                    });

                    gameState.CurrentPlayerIndex = (playerSeat + 1) % 4;
                    return true;
                });

            mockGameFlowService.Setup(x => x.ProcessAITurnsAsync(It.IsAny<GameState>(), It.IsAny<int>()))
                .Returns((GameState gameState, int aiPlayDelayMs) =>
                {
                    // Simple AI logic for testing - make AI players play their first card
                    while (gameState.CurrentPlayerIndex != 0 && gameState.Players[gameState.CurrentPlayerIndex].Hand.Count > 0)
                    {
                        var aiPlayer = gameState.Players[gameState.CurrentPlayerIndex];
                        if (aiPlayer.Hand.Count > 0)
                        {
                            var card = aiPlayer.Hand[0];
                            aiPlayer.Hand.RemoveAt(0);
                            gameState.PlayedCards.Add(new PlayedCard(gameState.CurrentPlayerIndex, card));
                            gameState.CurrentPlayerIndex = (gameState.CurrentPlayerIndex + 1) % 4;
                        }
                    }
                    return Task.CompletedTask;
                });

            mockGameFlowService.Setup(x => x.ProcessHandCompletionAsync(It.IsAny<GameState>(), It.IsAny<int>()))
                .Returns((GameState gameState, int newHandDelayMs) =>
                {
                    // Check if all players have played
                    bool allPlayersPlayed = gameState.PlayedCards.Count >= 4;
                    if (allPlayersPlayed)
                    {
                        // Clear played cards for next round
                        gameState.PlayedCards.Clear();
                        gameState.CurrentPlayerIndex = gameState.FirstPlayerSeat;
                    }
                    return Task.CompletedTask;
                });

            mockGameFlowService.Setup(x => x.StartNewHand(It.IsAny<GameState>()))
                .Callback((GameState gameState) =>
                {
                    // Reset for new hand
                    gameState.PlayedCards.Clear();
                    gameState.CurrentPlayerIndex = gameState.FirstPlayerSeat;
        });        // Create mock GameService with required dependencies (no GameFlowReactionService needed)
            return new GameService(
                mockGameStateManager.Object,
                mockGameRepository.Object,
                mockGameFlowService.Object,
                mockTrucoRulesEngine.Object,
                mockAIPlayerService.Object,
                mockScoreCalculationService.Object,
                mockEventPublisher.Object,
                configuration);
        }
        private GameState CreateValidGameState(string? playerName = null)
        {
            var gameState = new GameState();
            gameState.InitializeGame(playerName ?? "TestPlayer");
            gameState.FirstPlayerSeat = 0; // Ensure human player starts
            gameState.CurrentPlayerIndex = 0; // Ensure human player is active
            return gameState;
        }
        [Fact]
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
            Assert.Equal(1, game.CurrentHand); Assert.Equal(2, game.TeamScores.Count);
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
        }
        [Fact]
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
        }
        [Fact]
        public void GetGame_ShouldReturnNull_WhenGameDoesNotExist()
        {
            // Arrange
            var gameService = CreateGameService();
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
            var gameService = CreateGameService();
            var game = gameService.CreateGame();

            // Find the active player
            var activePlayer = game.Players.First(p => p.IsActive);
            // Act
            var response = gameService.PlayCard(game.GameId, activePlayer.Seat, 0);

            // Assert
            Assert.True(response.Success);

            // Get the updated game state
            var updatedGame = gameService.GetGame(game.GameId);
            Assert.NotNull(updatedGame);

            var updatedPlayer = updatedGame.Players.First(p => p.Seat == activePlayer.Seat);

            // The player should have one less card
            Assert.Equal(2, updatedPlayer.Hand.Count);

            // A card should have been played
            var playedCard = updatedGame.PlayedCards.First(pc => pc.PlayerSeat == activePlayer.Seat);
            Assert.NotNull(playedCard.Card);

            // An action should have been logged
            Assert.Contains(updatedGame.ActionLog, a => a.Type == "card-played" && a.PlayerSeat == activePlayer.Seat);
        }
    }
}
