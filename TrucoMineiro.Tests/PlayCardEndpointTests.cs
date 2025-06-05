using Microsoft.Extensions.Configuration;
using TrucoMineiro.API.Services;
using TrucoMineiro.API.DTOs;
using TrucoMineiro.API.Models;
using TrucoMineiro.API.Domain.Interfaces;
using TrucoMineiro.API.Domain.Services;
using Moq;

namespace TrucoMineiro.Tests
{
    /// <summary>
    /// Tests for the new PlayCard endpoint
    /// </summary>
    public class PlayCardEndpointTests
    {
        private readonly IConfiguration _configuration;        public PlayCardEndpointTests()
        {
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
        public void PlayCardEnhanced_ShouldReturnSuccess_WhenValidMove()
        {
            // Arrange
            var gameService = CreateGameService();
            var game = gameService.CreateGame("TestPlayer");
            var activePlayer = game.Players.First(p => p.IsActive);

            // Act
            var response = gameService.PlayCardEnhanced(game.GameId, activePlayer.Id, 0, false, activePlayer.Seat);

            // Assert
            Assert.True(response.Success);
            Assert.Equal("Card played successfully", response.Message);
            Assert.NotNull(response.GameState);
            Assert.Equal(2, response.Hand.Count); // Player should have 2 cards left
            Assert.Equal(4, response.PlayerHands.Count); // Should have all 4 player hands
        }        [Fact]
        public void PlayCardEnhanced_ShouldHideAICards_WhenNotInDevMode()
        {            // Arrange
            var gameService = CreateGameService();
            var game = gameService.CreateGame("TestPlayer");
            var activePlayer = game.Players.First(p => p.IsActive);

            // Act
            var response = gameService.PlayCardEnhanced(game.GameId, activePlayer.Id, 0, false, 0);

            // Assert
            Assert.True(response.Success);
            
            // Human player's hand should be visible
            var humanPlayerHand = response.PlayerHands.First(h => h.Seat == 0);
            Assert.All(humanPlayerHand.Cards, card => Assert.NotNull(card.Value));
            Assert.All(humanPlayerHand.Cards, card => Assert.NotNull(card.Suit));

            // AI player hands should be hidden
            var aiPlayerHands = response.PlayerHands.Where(h => h.Seat != 0);
            foreach (var aiHand in aiPlayerHands)
            {
                Assert.All(aiHand.Cards, card => Assert.Null(card.Value));
                Assert.All(aiHand.Cards, card => Assert.Null(card.Suit));
            }
        }        [Fact]
        public void PlayCardEnhanced_ShouldShowAllCards_WhenInDevMode()
        {
            // Arrange
            var devModeSettings = new Dictionary<string, string?> {
                {"FeatureFlags:DevMode", "true"},
            };
            var devConfig = new ConfigurationBuilder()
                .AddInMemoryCollection(devModeSettings)
                .Build();

            var gameService = CreateGameService(devConfig);
            var game = gameService.CreateGame("TestPlayer");
            var activePlayer = game.Players.First(p => p.IsActive);

            // Act
            var response = gameService.PlayCardEnhanced(game.GameId, activePlayer.Id, 0, false, 0);

            // Assert
            Assert.True(response.Success);// All player hands should be visible in DevMode
            foreach (var playerHand in response.PlayerHands)
            {
                // Only check hands that have cards
                if (playerHand.Cards.Count > 0)
                {
                    Assert.All(playerHand.Cards, card => Assert.NotNull(card.Value));
                    Assert.All(playerHand.Cards, card => Assert.NotNull(card.Suit));
                }
            }
        }        [Fact]
        public void PlayCardEnhanced_ShouldHandleFold_WhenFoldRequested()
        {
            // Arrange
            var gameService = CreateGameService();
            var game = gameService.CreateGame("TestPlayer");
            var activePlayer = game.Players.First(p => p.IsActive);

            // Act
            var response = gameService.PlayCardEnhanced(game.GameId, activePlayer.Id, 0, true, activePlayer.Seat);

            // Assert
            Assert.True(response.Success);
            Assert.Equal("Hand folded successfully", response.Message);
            Assert.NotNull(response.GameState);
        }        [Fact]
        public void PlayCardEnhanced_ShouldReturnError_WhenGameNotFound()
        {
            // Arrange
            var gameService = CreateGameService();

            // Act
            var response = gameService.PlayCardEnhanced("invalid-game-id", "player1", 0, false, 0);

            // Assert
            Assert.False(response.Success);
            Assert.Equal("Game not found", response.Message);
        }        [Fact]
        public void PlayCardEnhanced_ShouldReturnError_WhenInvalidCardIndex()
        {
            // Arrange
            var gameService = CreateGameService();
            var game = gameService.CreateGame("TestPlayer");
            var activePlayer = game.Players.First(p => p.IsActive);

            // Act
            var response = gameService.PlayCardEnhanced(game.GameId, activePlayer.Id, 99, false, activePlayer.Seat);

            // Assert
            Assert.False(response.Success);
            Assert.Equal("Invalid card play", response.Message);
        }

        [Fact]
        public void PlayCardEnhanced_ShouldHandleAITurns_InDevMode()
        {
            // Arrange
            var devModeSettings = new Dictionary<string, string?> {
                {"FeatureFlags:DevMode", "true"},
            };            var devConfig = new ConfigurationBuilder()
                .AddInMemoryCollection(devModeSettings)
                .Build();            var gameService = CreateGameService(devConfig);
            var game = gameService.CreateGame("TestPlayer");
            var activePlayer = game.Players.First(p => p.IsActive);

            // Act
            var response = gameService.PlayCardEnhanced(game.GameId, activePlayer.Id, 0, false, 0);

            // Assert
            Assert.True(response.Success);
            
            // Check that AI players have played their cards
            var playedCards = response.GameState.PlayedCards.Where(pc => pc.Card != null).Count();
            Assert.True(playedCards > 1); // Should be more than just the active player's card
        }        [Fact]
        public void MapGameStateToPlayCardResponse_ShouldMapCorrectly()
        {
            // Arrange
            var gameService = CreateGameService();
            var game = gameService.CreateGame("TestPlayer");
            
            // Act
            var response = MappingService.MapGameStateToPlayCardResponse(game, 0, false, true, "Test message");

            // Assert
            Assert.True(response.Success);
            Assert.Equal("Test message", response.Message);
            Assert.NotNull(response.GameState);
            Assert.Equal(3, response.Hand.Count); // Player should have 3 cards initially
            Assert.Equal(4, response.PlayerHands.Count); // Should have all 4 player hands

            // Check card visibility
            var humanPlayerHand = response.PlayerHands.First(h => h.Seat == 0);
            Assert.All(humanPlayerHand.Cards, card => Assert.NotNull(card.Value));
            
            var aiPlayerHand = response.PlayerHands.First(h => h.Seat == 1);
            Assert.All(aiPlayerHand.Cards, card => Assert.Null(card.Value));
        }        [Fact]
        public void Debug_PlayCardEnhanced_CardVisibility()
        {
            // Arrange - Test without DevMode first
            var gameService = CreateGameService();
            var game = gameService.CreateGame("TestPlayer");
            var activePlayer = game.Players.First(p => p.IsActive);

            Console.WriteLine($"Initial active player (seat {activePlayer.Seat}) hand count: {activePlayer.Hand.Count}");
            Console.WriteLine($"Initial human player (seat 0) hand count: {game.Players.First(p => p.Seat == 0).Hand.Count}");
            Console.WriteLine($"Initial AI player hands:");
            foreach (var player in game.Players.Where(p => p.Seat != 0))
            {
                Console.WriteLine($"  Player {player.Seat}: {player.Hand.Count} cards, IsActive: {player.IsActive}");
            }

            // Act
            var response = gameService.PlayCardEnhanced(game.GameId, activePlayer.Id, 0, false, 0);            Console.WriteLine($"\nAfter play card:");
            Console.WriteLine($"Success: {response.Success}");
            Console.WriteLine($"Message: {response.Message}");
            Console.WriteLine($"Active player hand count in response: {response.Hand.Count}");
            Console.WriteLine($"PlayerHands count: {response.PlayerHands.Count}");
            
            foreach (var playerHand in response.PlayerHands)
            {
                Console.WriteLine($"Player {playerHand.Seat}: {playerHand.Cards.Count} cards");
                if (playerHand.Cards.Count > 0)
                {
                    var firstCard = playerHand.Cards.First();
                    Console.WriteLine($"  First card - Value: '{firstCard.Value}', Suit: '{firstCard.Suit}'");
                }
            }

            // The basic assertion should pass
            Assert.True(response.Success);
        }
    }
}
