using Microsoft.Extensions.Configuration;
using System.Collections.Generic;
using TrucoMineiro.API.Constants;
using TrucoMineiro.API.Services;
using TrucoMineiro.API.Domain.Interfaces;
using TrucoMineiro.API.Domain.Services;
using Moq;
using Xunit;
using TrucoMineiro.API.Domain.Models;

namespace TrucoMineiro.Tests
{    public class StartGameTests
    {
        private readonly IConfiguration _configuration;        public StartGameTests()
        {
            // Set up test configuration
            var inMemorySettings = new Dictionary<string, string?> {
                {"FeatureFlags:DevMode", "false"},
            };

            _configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(inMemorySettings)
                .Build();
        }        private GameService CreateGameService(IConfiguration? config = null)
        {
            var configuration = config ?? _configuration;
              // Create a dictionary to store created games (simulating repository storage)
            var gameStorage = new Dictionary<string, GameState>();
            
            // Create mock services
            var mockGameStateManager = new Mock<IGameStateManager>();
            var mockGameRepository = new Mock<IGameRepository>();
            var mockGameFlowService = new Mock<IGameFlowService>();
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
                .Returns(false);            // Configure mock TrucoRulesEngine
            mockTrucoRulesEngine.Setup(x => x.CalculateHandPoints(It.IsAny<GameState>()))
                .Returns(1);

            // Configure mock GameFlowService
            mockGameFlowService.Setup(x => x.PlayCard(It.IsAny<GameState>(), It.IsAny<int>(), It.IsAny<int>()))
                .Returns((GameState gameState, int playerSeat, int cardIndex) => {
                    var player = gameState.Players[playerSeat];
                    if (cardIndex < 0 || cardIndex >= player.Hand.Count)
                        return false;
                    
                    // Play the card
                    var card = player.Hand[cardIndex];
                    player.Hand.RemoveAt(cardIndex);
                    
                    // Add to played cards
                    gameState.PlayedCards.Add(new PlayedCard(playerSeat, card));
                    gameState.CurrentPlayerIndex = (playerSeat + 1) % 4;
                    return true;
                });

            mockGameFlowService.Setup(x => x.ProcessAITurnsAsync(It.IsAny<GameState>(), It.IsAny<int>()))
                .Returns((GameState gameState, int aiPlayDelayMs) => {
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
                .Returns((GameState gameState, int newHandDelayMs) => {
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
                .Callback((GameState gameState) => {
                    // Reset for new hand
                    gameState.PlayedCards.Clear();
                    gameState.CurrentPlayerIndex = gameState.FirstPlayerSeat;
                });            // Create mock GameFlowReactionService
            var mockGameFlowReactionService = new Mock<IGameFlowReactionService>();
            mockGameFlowReactionService.Setup(x => x.ProcessCardPlayReactionsAsync(
                It.IsAny<GameState>(), It.IsAny<bool>(), It.IsAny<int>(), It.IsAny<int>()))
                .Returns(Task.CompletedTask);
                
            return new GameService(
                mockGameStateManager.Object,
                mockGameRepository.Object,
                mockGameFlowService.Object,
                mockTrucoRulesEngine.Object,
                mockAIPlayerService.Object,
                mockScoreCalculationService.Object,
                mockGameFlowReactionService.Object,
                configuration);
        }        private GameState CreateValidGameState(string? playerName = null)
        {
            var gameState = new GameState();
            gameState.InitializeGame(playerName ?? "TestPlayer");
            gameState.FirstPlayerSeat = 0; // Ensure human player starts
            gameState.CurrentPlayerIndex = 0; // Ensure human player is active
            return gameState;
        }[Fact]
        public void CreateGameWithCustomName_ShouldInitializeGameState()
        {
            // Arrange
            var gameService = CreateGameService();
            string playerName = "TestPlayer";

            // Act
            var game = gameService.CreateGame(playerName);

            // Assert
            Assert.NotNull(game);
            Assert.Equal(4, game.Players.Count);
            Assert.Equal(TrucoConstants.Stakes.Initial, game.Stakes); // Stakes should start at 2 as per Truco Mineiro rules
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
            
            // Player at seat 0 should have the custom name
            Assert.Equal(playerName, game.Players.First(p => p.Seat == 0).Name);
            
            // Other players should have default names
            Assert.Equal("AI 1", game.Players.First(p => p.Seat == 1).Name);
            Assert.Equal("Partner", game.Players.First(p => p.Seat == 2).Name);
            Assert.Equal("AI 2", game.Players.First(p => p.Seat == 3).Name);
        }        [Fact]
        public void MapGameStateToStartGameResponse_ShouldMapCorrectly()
        {
            // Arrange
            var gameService = CreateGameService();
            string playerName = "TestPlayer";
            var game = gameService.CreateGame(playerName);

            // Act
            var response = MappingService.MapGameStateToStartGameResponse(game);

            // Assert
            Assert.NotNull(response);
            Assert.Equal(game.GameId, response.GameId);
            Assert.Equal(0, response.PlayerSeat); // Single player is always at seat 0
            Assert.Equal(game.DealerSeat, response.DealerSeat);
            Assert.Equal(TrucoConstants.Stakes.Initial, response.Stakes); // Stakes should start at 2
            Assert.Equal(1, response.CurrentHand);
            Assert.Equal(2, response.TeamScores.Count);
            Assert.Equal(0, response.TeamScores["Player's Team"]);
            Assert.Equal(0, response.TeamScores["Opponent Team"]);
            Assert.Empty(response.Actions);
            
            // Teams should be mapped correctly
            Assert.Equal(2, response.Teams.Count);
            var playerTeam = response.Teams.First(t => t.Name == "Player's Team");
            var opponentTeam = response.Teams.First(t => t.Name == "Opponent Team");
            Assert.Equal(2, playerTeam.Seats.Count);
            Assert.Equal(2, opponentTeam.Seats.Count);
            Assert.Contains(0, playerTeam.Seats);
            Assert.Contains(2, playerTeam.Seats);
            Assert.Contains(1, opponentTeam.Seats);
            Assert.Contains(3, opponentTeam.Seats);

            // Players should be mapped correctly
            Assert.Equal(4, response.Players.Count);
            Assert.Equal(playerName, response.Players.First(p => p.Seat == 0).Name);
            Assert.Equal("Player's Team", response.Players.First(p => p.Seat == 0).Team);
              // Hand should have 3 cards for player at seat 0
            Assert.Equal(3, response.Hand.Count);
            
            // PlayerHands should have entries for all 4 players
            Assert.Equal(4, response.PlayerHands.Count);
            
            // Check that AI player hands have empty cards (no values/suits)
            var aiPlayerHand = response.PlayerHands.First(h => h.Seat == 1);
            Assert.Equal(3, aiPlayerHand.Cards.Count);
            Assert.All(aiPlayerHand.Cards, card => Assert.Null(card.Value));
            Assert.All(aiPlayerHand.Cards, card => Assert.Null(card.Suit));
        }
        
        [Fact]
        public void MapGameStateToStartGameResponse_WithDevMode_ShouldShowCards()
        {
            // Arrange
            var devModeSettings = new Dictionary<string, string?> {
                {"FeatureFlags:DevMode", "true"},
            };            var devConfig = new ConfigurationBuilder()
                .AddInMemoryCollection(devModeSettings)
                .Build();

            var gameService = CreateGameService(devConfig);
            string playerName = "TestPlayer";
            var game = gameService.CreateGame(playerName);

            // Act - Note we pass true for showAllHands param
            var response = MappingService.MapGameStateToStartGameResponse(game, 0, true);            // Assert
            Assert.NotNull(response);
            Assert.Equal(3, response.Hand.Count); // Player's cards should be visible
            
            // In DevMode, all player hands should have visible cards
            Assert.Equal(4, response.PlayerHands.Count);
            
            // Check that AI player hands have actual card values in DevMode
            var aiPlayerHand = response.PlayerHands.First(h => h.Seat == 1);
            Assert.Equal(3, aiPlayerHand.Cards.Count);
            Assert.Contains(aiPlayerHand.Cards, card => card.Value != null);
            Assert.Contains(aiPlayerHand.Cards, card => card.Suit != null);
        }
    }
}
