using Microsoft.Extensions.Configuration;
using TrucoMineiro.API.Services;
using TrucoMineiro.API.Controllers;
using Microsoft.AspNetCore.Mvc;
using TrucoMineiro.API.DTOs;
using TrucoMineiro.API.Domain.Events;
using TrucoMineiro.API.Domain.Interfaces;
using TrucoMineiro.API.Domain.Services;
using Moq;
using System.Collections.Generic;
using Xunit;
using TrucoMineiro.API.Domain.Models;

namespace TrucoMineiro.Tests
{
    /// <summary>
    /// Tests for the GetGameState endpoint with player-specific visibility
    /// </summary>
    public class GetGameStateEndpointTests
    {
        private readonly GameService _gameService;
        private readonly TrucoGameController _controller;        public GetGameStateEndpointTests()
        {
            // Set up test configuration with DevMode disabled
            var inMemorySettings = new Dictionary<string, string?> {
                {"FeatureFlags:DevMode", "false"},
            };

            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(inMemorySettings)
                .Build();

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
                .Callback((GameState gameState) => {                // Reset for new hand
                    gameState.PlayedCards.Clear();
                    gameState.CurrentPlayerIndex = gameState.FirstPlayerSeat;
                });
                  _gameService = new GameService(
                mockGameStateManager.Object,
                mockGameRepository.Object,
                mockGameFlowService.Object,
                mockTrucoRulesEngine.Object,
                mockAIPlayerService.Object,
                mockScoreCalculationService.Object,
                mockEventPublisher.Object,
                configuration);
            _controller = new TrucoGameController(_gameService);
        }private GameState CreateValidGameState(string? playerName = null)
        {
            var gameState = new GameState();
            gameState.InitializeGame(playerName ?? "TestPlayer");
            gameState.FirstPlayerSeat = 0; // Ensure human player starts
            gameState.CurrentPlayerIndex = 0; // Ensure human player is active
            return gameState;
        }[Fact]
        public void GetGameState_WithoutPlayerSeat_ShouldShowOnlyHumanPlayerCards()
        {
            // Arrange
            var game = _gameService.CreateGame("TestPlayer");
            var humanPlayer = game.Players.First(p => p.Seat == 0);
            
            // Act
            var result = _controller.GetGameState(game.GameId);
            
            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var gameState = Assert.IsType<GameStateDto>(okResult.Value);
            
            // Human player (seat 0) should have visible cards
            var humanPlayerDto = gameState.Players.First(p => p.Seat == 0);
            Assert.All(humanPlayerDto.Hand, card => 
            {
                Assert.NotNull(card.Value);
                Assert.NotNull(card.Suit);
            });
            
            // AI players (seats 1, 2, 3) should have hidden cards
            var aiPlayers = gameState.Players.Where(p => p.Seat != 0);
            foreach (var aiPlayer in aiPlayers)
            {
                Assert.All(aiPlayer.Hand, card => 
                {
                    Assert.Null(card.Value);
                    Assert.Null(card.Suit);
                });
            }
        }        [Fact]
        public void GetGameState_WithPlayerSeat_ShouldShowOnlyRequestingPlayerCards()
        {
            // Arrange
            var game = _gameService.CreateGame("TestPlayer");
            var aiPlayer = game.Players.First(p => p.Seat == 1);
              // Act - Request as AI player at seat 1
            var result = _controller.GetGameState(game.GameId, aiPlayer.Seat);
            
            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var gameState = Assert.IsType<GameStateDto>(okResult.Value);
            
            // AI player at seat 1 should have visible cards
            var requestingPlayerDto = gameState.Players.First(p => p.Seat == 1);
            Assert.All(requestingPlayerDto.Hand, card => 
            {
                Assert.NotNull(card.Value);
                Assert.NotNull(card.Suit);
            });
            
            // All other players should have hidden cards
            var otherPlayers = gameState.Players.Where(p => p.Seat != 1);
            foreach (var otherPlayer in otherPlayers)
            {
                Assert.All(otherPlayer.Hand, card => 
                {
                    Assert.Null(card.Value);
                    Assert.Null(card.Suit);
                });
            }
        }        [Fact]
        public void GetGameState_WithInvalidPlayerSeat_ShouldReturnBadRequest()
        {
            // Arrange
            var game = _gameService.CreateGame("TestPlayer");
            var invalidPlayerSeat = 999; // Using a seat number that doesn't exist
            
            // Act
            var result = _controller.GetGameState(game.GameId, invalidPlayerSeat);
              // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result.Result);
            Assert.Contains("Player seat must be between 0 and 3", badRequestResult.Value?.ToString());
        }

        [Fact]
        public void GetGameState_WithInvalidGameId_ShouldReturnNotFound()
        {
            // Arrange
            var invalidGameId = "non-existent-game";
            
            // Act
            var result = _controller.GetGameState(invalidGameId);
            
            // Assert
            var notFoundResult = Assert.IsType<NotFoundObjectResult>(result.Result);
            Assert.Equal("Game not found", notFoundResult.Value);
        }        [Fact]
        public void GetGameState_WithDevMode_ShouldShowAllCards()
        {
            // Arrange - Set up configuration with DevMode enabled
            var devModeSettings = new Dictionary<string, string?> {
                {"FeatureFlags:DevMode", "true"},
            };
            var devConfig = new ConfigurationBuilder()
                .AddInMemoryCollection(devModeSettings)
                .Build();

            // Create a dictionary to store created games (simulating repository storage)
            var gameStorage = new Dictionary<string, GameState>();            // Create mock services for dev mode test
            var mockGameStateManager = new Mock<IGameStateManager>();
            var mockGameRepository = new Mock<IGameRepository>();
            var mockGameFlowService = new Mock<IGameFlowService>();
            var mockTrucoRulesEngine = new Mock<ITrucoRulesEngine>();
            var mockAIPlayerService = new Mock<IAIPlayerService>();
            var mockScoreCalculationService = new Mock<IScoreCalculationService>();
            var mockEventPublisher = new Mock<IEventPublisher>();

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
                .ReturnsAsync((string gameId) => gameStorage.ContainsKey(gameId) ? gameStorage[gameId] : null);            mockGameRepository.Setup(x => x.SaveGameAsync(It.IsAny<GameState>()))
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
                  var devGameService = new GameService(
                mockGameStateManager.Object,
                mockGameRepository.Object,
                mockGameFlowService.Object,
                mockTrucoRulesEngine.Object,
                mockAIPlayerService.Object,
                mockScoreCalculationService.Object,
                mockEventPublisher.Object,
                devConfig);
            var devController = new TrucoGameController(devGameService);
            var game = devGameService.CreateGame("TestPlayer");
            
            // Act
            var result = devController.GetGameState(game.GameId);
            
            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var gameState = Assert.IsType<GameStateDto>(okResult.Value);
            
            // All players should have visible cards in DevMode
            foreach (var player in gameState.Players)
            {
                Assert.All(player.Hand, card => 
                {
                    Assert.NotNull(card.Value);
                    Assert.NotNull(card.Suit);
                });
            }
        }

        [Fact]
        public void GetGameState_CardCount_ShouldAlwaysBeCorrect()
        {
            // Arrange
            var game = _gameService.CreateGame("TestPlayer");
            var humanPlayer = game.Players.First(p => p.Seat == 0);
            
            // Act
            var result = _controller.GetGameState(game.GameId);
            
            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var gameState = Assert.IsType<GameStateDto>(okResult.Value);
            
            // All players should have 3 cards each (standard Truco hand)
            foreach (var player in gameState.Players)
            {
                Assert.Equal(3, player.Hand.Count);
            }
            
            // Verify the actual game state has the correct number of cards
            foreach (var gamePlayer in game.Players)
            {
                Assert.Equal(3, gamePlayer.Hand.Count);
            }
        }

        [Fact]
        public void GetGameState_MultipleRequests_ShouldBeConsistent()
        {
            // Arrange
            var game = _gameService.CreateGame("TestPlayer");
            var humanPlayer = game.Players.First(p => p.Seat == 0);
            var aiPlayer = game.Players.First(p => p.Seat == 1);
              // Act - Make multiple requests as different players
            var humanRequest = _controller.GetGameState(game.GameId, humanPlayer.Seat);
            var aiRequest = _controller.GetGameState(game.GameId, aiPlayer.Seat);
            var defaultRequest = _controller.GetGameState(game.GameId);
            
            // Assert
            var humanResult = Assert.IsType<OkObjectResult>(humanRequest.Result);
            var aiResult = Assert.IsType<OkObjectResult>(aiRequest.Result);
            var defaultResult = Assert.IsType<OkObjectResult>(defaultRequest.Result);
            
            var humanGameState = Assert.IsType<GameStateDto>(humanResult.Value);
            var aiGameState = Assert.IsType<GameStateDto>(aiResult.Value);
            var defaultGameState = Assert.IsType<GameStateDto>(defaultResult.Value);
            
            // All responses should have the same basic game info
            Assert.Equal(humanGameState.Stakes, aiGameState.Stakes);
            Assert.Equal(humanGameState.CurrentHand, aiGameState.CurrentHand);
            Assert.Equal(humanGameState.Players.Count, aiGameState.Players.Count);
            
            // But card visibility should be different
            var humanPlayerInHumanView = humanGameState.Players.First(p => p.Seat == 0);
            var humanPlayerInAiView = aiGameState.Players.First(p => p.Seat == 0);
            
            // Human should see their own cards in both views, but AI should not see human cards in AI view
            Assert.All(humanPlayerInHumanView.Hand, card => Assert.NotNull(card.Value));
            Assert.All(humanPlayerInAiView.Hand, card => Assert.Null(card.Value));
        }

        [Fact]
        public void GetGameState_IntegrationTest_ShouldWorkEndToEnd()
        {
            // Arrange
            var game = _gameService.CreateGame("IntegrationTestPlayer");
            var humanPlayer = game.Players.First(p => p.Seat == 0);
            var aiPlayer = game.Players.First(p => p.Seat == 1);            // Act & Assert - Test without playerSeat (should default to human player at seat 0)
            var defaultResult = _controller.GetGameState(game.GameId);
            var defaultOk = Assert.IsType<OkObjectResult>(defaultResult.Result);
            var defaultGameState = Assert.IsType<GameStateDto>(defaultOk.Value);
            
            // Human cards should be visible, AI cards should be hidden
            var humanInDefault = defaultGameState.Players.First(p => p.Seat == 0);
            var aiInDefault = defaultGameState.Players.First(p => p.Seat == 1);
            
            Assert.True(humanInDefault.Hand.All(c => c.Value != null && c.Suit != null), "Human cards should be visible in default request");
            Assert.True(aiInDefault.Hand.All(c => c.Value == null && c.Suit == null), "AI cards should be hidden in default request");            // Act & Assert - Test with specific AI player seat
            var aiResult = _controller.GetGameState(game.GameId, aiPlayer.Seat);
            var aiOk = Assert.IsType<OkObjectResult>(aiResult.Result);
            var aiGameState = Assert.IsType<GameStateDto>(aiOk.Value);
            
            // AI cards should be visible, human cards should be hidden
            var humanInAi = aiGameState.Players.First(p => p.Seat == 0);
            var aiInAi = aiGameState.Players.First(p => p.Seat == 1);
            
            Assert.True(humanInAi.Hand.All(c => c.Value == null && c.Suit == null), "Human cards should be hidden when requesting as AI");
            Assert.True(aiInAi.Hand.All(c => c.Value != null && c.Suit != null), "AI cards should be visible when requesting as AI");

            // Verify game state consistency
            Assert.Equal(defaultGameState.Stakes, aiGameState.Stakes);
            Assert.Equal(defaultGameState.CurrentHand, aiGameState.CurrentHand);
            Assert.Equal(defaultGameState.Players.Count, aiGameState.Players.Count);
        }
    }
}
