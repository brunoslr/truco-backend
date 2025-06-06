using Microsoft.Extensions.Configuration;
using TrucoMineiro.API.Services;
using TrucoMineiro.API.DTOs;
using TrucoMineiro.API.Domain.Interfaces;
using TrucoMineiro.API.Domain.Services;
using Moq;
using TrucoMineiro.API.Domain.Models;

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
                {"FeatureFlags:AutoAiPlay", "true"}, // Enable AI auto-play by default
            };

            _configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(inMemorySettings)
                .Build();
        }private GameService CreateGameService(IConfiguration? config = null)
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
                .Callback((GameState gameState, bool autoAiPlay, int aiPlayDelayMs, int newHandDelayMs) => {
                    // If autoAiPlay is true, process AI turns
                    if (autoAiPlay)
                    {
                        // Use the already mocked ProcessAITurnsAsync to process AI turns
                        mockGameFlowService.Object.ProcessAITurnsAsync(gameState, aiPlayDelayMs).GetAwaiter().GetResult();
                    }
                })
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
        }    [Fact]
        public void PlayCard_ShouldReturnSuccess_WhenValidMove()
        {
            // Arrange
            var gameService = CreateGameService();
            var game = gameService.CreateGame("TestPlayer");
            var activePlayer = game.Players.First(p => p.IsActive);            // Act
            var response = gameService.PlayCard(game.GameId, activePlayer.Seat, 0, false, activePlayer.Seat);

            // Assert
            Assert.True(response.Success);
            Assert.Equal("Card played successfully", response.Message);
            Assert.NotNull(response.GameState);
            Assert.Equal(2, response.Hand.Count); // Player should have 2 cards left
            Assert.Equal(4, response.PlayerHands.Count); // Should have all 4 player hands
        }    [Fact]
        public void PlayCard_ShouldHideAICards_WhenNotInDevMode()
        {            // Arrange
            var gameService = CreateGameService();
            var game = gameService.CreateGame("TestPlayer");
            var activePlayer = game.Players.First(p => p.IsActive);

            // Act
            var response = gameService.PlayCard(game.GameId, activePlayer.Seat, 0, false, 0);

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
        }    [Fact]
        public void PlayCard_ShouldShowAllCards_WhenInDevMode()
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
            var response = gameService.PlayCard(game.GameId, activePlayer.Seat, 0, false, 0);

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
        }    [Fact]
        public void PlayCard_ShouldHandleFold_WhenFoldRequested()
        {
            // Arrange
            var gameService = CreateGameService();
            var game = gameService.CreateGame("TestPlayer");
            var activePlayer = game.Players.First(p => p.IsActive);

            // Act
            var response = gameService.PlayCard(game.GameId, activePlayer.Seat, 0, true, activePlayer.Seat);

            // Assert
            Assert.True(response.Success);
            Assert.Equal("Card played successfully", response.Message);
            Assert.NotNull(response.GameState);
            
            // Verify a special fold card was created (value=0, empty suit)
            var updatedGame = gameService.GetGame(game.GameId);
            var playedCard = updatedGame.PlayedCards.FirstOrDefault(pc => pc.PlayerSeat == activePlayer.Seat);
            Assert.NotNull(playedCard);
            Assert.NotNull(playedCard.Card);
            Assert.Equal("0", playedCard.Card.Value);
            Assert.Equal("", playedCard.Card.Suit);
        }[Fact]
        public void PlayCard_ShouldReturnError_WhenGameNotFound()
        {
            // Arrange
            var gameService = CreateGameService();            // Act
            var response = gameService.PlayCard("invalid-game-id", 0, 0, false, 0);

            // Assert
            Assert.False(response.Success);
            Assert.Equal("Game not found", response.Message);
        }[Fact]
        public void PlayCard_ShouldReturnError_WhenInvalidCardIndex()
        {
            // Arrange
            var gameService = CreateGameService();
            var game = gameService.CreateGame("TestPlayer");
            var activePlayer = game.Players.First(p => p.IsActive);

            // Act
            var response = gameService.PlayCard(game.GameId, activePlayer.Seat, 99, false, activePlayer.Seat);

            // Assert
            Assert.False(response.Success);
            Assert.Equal("Invalid card play", response.Message);
        }[Fact]
        public void PlayCard_ShouldHandleAITurns_WhenAutoAiPlayEnabled()
        {
            // Arrange
            var autoAiPlaySettings = new Dictionary<string, string?> {
                {"FeatureFlags:DevMode", "false"},
                {"FeatureFlags:AutoAiPlay", "true"},
            };

            var autoAiPlayConfig = new ConfigurationBuilder()
                .AddInMemoryCollection(autoAiPlaySettings)
                .Build();

            var gameService = CreateGameService(autoAiPlayConfig);
            var game = gameService.CreateGame("TestPlayer");
            var activePlayer = game.Players.First(p => p.IsActive);

            // Act
            var response = gameService.PlayCard(game.GameId, activePlayer.Seat, 0, false, 0);

            // Assert
            Assert.True(response.Success);
            
            // Check that AI players have played their cards
            var playedCards = response.GameState.PlayedCards.Where(pc => pc.Card != null).Count();
            Assert.True(playedCards > 1); // Should be more than just the active player's card
        }[Fact]
        public void PlayCard_ShouldNotHandleAITurns_WhenAutoAiPlayDisabled()
        {
            // Arrange
            var autoAiPlayDisabledSettings = new Dictionary<string, string?> {
                {"FeatureFlags:DevMode", "false"},
                {"FeatureFlags:AutoAiPlay", "false"},
            };

            var autoAiPlayDisabledConfig = new ConfigurationBuilder()
                .AddInMemoryCollection(autoAiPlayDisabledSettings)
                .Build();

            var gameService = CreateGameService(autoAiPlayDisabledConfig);
            var game = gameService.CreateGame("TestPlayer");
            var activePlayer = game.Players.First(p => p.IsActive);

            // Act
            var response = gameService.PlayCard(game.GameId, activePlayer.Seat, 0, false, 0);

            // Assert
            Assert.True(response.Success);
            
            // Check that only the human player has played a card (AI should not have played automatically)
            var playedCards = response.GameState.PlayedCards.Where(pc => pc.Card != null).Count();
            Assert.Equal(1, playedCards); // Should be only the human player's card
        }[Fact]
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
        public void Debug_PlayCard_CardVisibility()
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
            var response = gameService.PlayCard(game.GameId, activePlayer.Seat, 0, false, 0);            Console.WriteLine($"\nAfter play card:");
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
