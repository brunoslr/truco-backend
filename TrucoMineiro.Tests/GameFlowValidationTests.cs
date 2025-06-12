using Microsoft.Extensions.Configuration;
using TrucoMineiro.API.Services;
using TrucoMineiro.API.DTOs;
using TrucoMineiro.API.Domain.Events;
using TrucoMineiro.API.Domain.Interfaces;
using TrucoMineiro.API.Domain.Services;
using Moq;
using TrucoMineiro.API.Domain.Models;

namespace TrucoMineiro.Tests
{
    /// <summary>
    /// Tests for validating game flow and state consistency
    /// </summary>
    public class GameFlowValidationTests
    {
        private readonly IConfiguration _configuration;

        public GameFlowValidationTests()
        {
            var inMemorySettings = new Dictionary<string, string?> {
                {"FeatureFlags:DevMode", "true"},
                {"FeatureFlags:AutoAiPlay", "false"}, // Disable auto AI play for manual control
                {"GameSettings:AIPlayDelayMs", "0"},
                {"GameSettings:NewHandDelayMs", "0"}
            };

            _configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(inMemorySettings)
                .Build();
        }

        private (IGameStateManager, IGameRepository) CreateGameServices()
        {
            // Create a dictionary to store created games (simulating repository storage)
            var gameStorage = new Dictionary<string, GameState>();
              // Create mock services
            var mockGameStateManager = new Mock<IGameStateManager>();
            var mockGameRepository = new Mock<IGameRepository>();
            var mockTrucoRulesEngine = new Mock<ITrucoRulesEngine>();
            var mockAIPlayerService = new Mock<IAIPlayerService>();
            var mockScoreCalculationService = new Mock<IScoreCalculationService>();
            var mockEventPublisher = new Mock<IEventPublisher>();
            var mockPlayCardService = new Mock<IPlayCardService>();
            
            // Setup IPlayCardService.ProcessPlayCardRequestAsync to return successful responses
            mockPlayCardService.Setup(x => x.ProcessPlayCardRequestAsync(It.IsAny<PlayCardRequestDto>()))
                .ReturnsAsync((PlayCardRequestDto request) => {
                    var game = gameStorage.ContainsKey(request.GameId) ? gameStorage[request.GameId] : null;                    if (game == null)
                    {
                        return new PlayCardResponseDto
                        {
                            Success = false,
                            Error = "Game not found"
                        };
                    }
                    
                    var player = game.Players.FirstOrDefault(p => p.Seat == request.PlayerSeat);
                    if (player == null || request.CardIndex < 0 || request.CardIndex >= player.Hand.Count)
                    {
                        return new PlayCardResponseDto
                        {
                            Success = false,
                            Error = "Invalid move"
                        };
                    }
                    
                    // Simulate card play
                    var cardToPlay = player.Hand[request.CardIndex];
                    player.Hand.RemoveAt(request.CardIndex);
                    
                    // Add to played cards
                    var existingPlayedCard = game.PlayedCards.FirstOrDefault(pc => pc.PlayerSeat == request.PlayerSeat);
                    if (existingPlayedCard != null)
                    {
                        existingPlayedCard.Card = cardToPlay;
                    }
                    else
                    {
                        game.PlayedCards.Add(new PlayedCard(request.PlayerSeat, cardToPlay));
                    }                    
                    return new PlayCardResponseDto
                    {
                        Success = true,
                        Message = "Card played successfully"
                    };
                });

            // Configure mock GameStateManager to return a valid GameState and store it
            mockGameStateManager.Setup(x => x.CreateGameAsync(It.IsAny<string>()))
                .ReturnsAsync((string playerName) => {
                    var gameState = CreateValidGameState(playerName);
                    gameStorage[gameState.GameId] = gameState;
                    return gameState;
                });

            // Configure mock GameRepository to return games from storage and update storage when saved
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
            
            // NOTE: ProcessAITurnsAsync is obsolete - AI processing is now event-driven
            // No need to mock this obsolete method as tests should use real event handlers
                  return (mockGameStateManager.Object, mockGameRepository.Object);
        }

        private GameState CreateValidGameState(string? playerName = null)
        {            var gameState = new GameState();
            gameState.InitializeGame(playerName ?? "TestPlayer");
            // FirstPlayerSeat is computed automatically based on DealerSeat
            gameState.CurrentPlayerIndex = 0; // Ensure human player is active
            return gameState;
        }        
        
        [Fact]
        public async Task GameFlow_ShouldCreateGameSuccessfully()
        {
            // Step 1: Create a new game directly using GameStateManager
            var (gameStateManager, gameRepository) = CreateGameServices();
            var game = await gameStateManager.CreateGameAsync("TestPlayer");
            
            // Verify initial state
            Assert.NotNull(game);
            Assert.Equal("TestPlayer", game.Players[0].Name);
            Assert.Equal(4, game.Players.Count); // Should have 4 players
            Assert.All(game.Players, player => Assert.Equal(3, player.Hand.Count)); // Each player should have 3 cards
        }
    }
}
