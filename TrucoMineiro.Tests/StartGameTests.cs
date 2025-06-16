using Microsoft.Extensions.Configuration;
using Moq;
using TrucoMineiro.API.Constants;
using TrucoMineiro.API.Domain.Events;
using TrucoMineiro.API.Domain.Interfaces;
using TrucoMineiro.API.Domain.Models;
using TrucoMineiro.API.Domain.Services;
using TrucoMineiro.API.Services;

namespace TrucoMineiro.Tests
{    public class StartGameTests
    {
        private readonly IConfiguration _configuration;
        private readonly MappingService _mappingService;

        public StartGameTests()
        {
            // Set up test configuration
            var inMemorySettings = new Dictionary<string, string?> {
                {"FeatureFlags:DevMode", "false"},
            };

            _configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(inMemorySettings)
                .Build();

            // Set up MappingService
            var trucoRulesEngine = new TrucoRulesEngine();
            _mappingService = new MappingService(trucoRulesEngine);
        }private IGameStateManager CreateGameStateManager(IConfiguration? config = null)
        {
            var configuration = config ?? _configuration;
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

            // NOTE: ProcessAITurnsAsync is obsolete - AI processing is now event-driven
            // No need to mock this obsolete method as tests should use real event handlers

            return mockGameStateManager.Object;
        }        private GameState CreateValidGameState(string? playerName = null)
        {
            var gameState = new GameState();
            gameState.InitializeGame(playerName ?? "TestPlayer");
            // FirstPlayerSeat is computed automatically based on DealerSeat
            gameState.CurrentPlayerIndex = 0; // Ensure human player is active
            return gameState;
        }        [Fact]
        public async Task CreateGameWithCustomName_ShouldInitializeGameState()
        {
            // Arrange
            var gameStateManager = CreateGameStateManager();
            string playerName = "TestPlayer";

            // Act
            var game = await gameStateManager.CreateGameAsync(playerName);

            // Assert
            Assert.NotNull(game);
            Assert.Equal(4, game.Players.Count);            Assert.Equal(TrucoCallState.None, game.TrucoCallState);
            Assert.Equal(2, game.CurrentStakes);
            Assert.Equal(1, game.CurrentHand);
            Assert.Equal(2, game.TeamScores.Count);
            Assert.Equal(0, game.TeamScores[Team.PlayerTeam]);
            Assert.Equal(0, game.TeamScores[Team.OpponentTeam]);
            
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
        public async Task MapGameStateToStartGameResponse_ShouldMapCorrectly()
        {
            // Arrange
            var gameStateManager = CreateGameStateManager();
            string playerName = "TestPlayer";
            var game = await gameStateManager.CreateGameAsync(playerName);

            // Act
            var response = _mappingService.MapGameStateToStartGameResponse(game);

            // Assert
            Assert.NotNull(response);
            Assert.Equal(game.GameId, response.GameId);
            Assert.Equal(0, response.PlayerSeat); // Single player is always at seat 0
            Assert.Equal(game.DealerSeat, response.DealerSeat);
            Assert.Equal(TrucoConstants.Stakes.Initial, response.Stakes); // Stakes should start at 2
            Assert.Equal(1, response.CurrentHand);
            Assert.Equal(2, response.TeamScores.Count);
            Assert.Equal(0, response.TeamScores[Team.PlayerTeam.ToString()]);
            Assert.Equal(0, response.TeamScores[Team.OpponentTeam.ToString()]);
            Assert.Empty(response.Actions);
            
            // Teams should be mapped correctly
            Assert.Equal(2, response.Teams.Count);
            var playerTeam = response.Teams.First(t => t.Name == Team.PlayerTeam.ToString());
            var opponentTeam = response.Teams.First(t => t.Name == Team.OpponentTeam.ToString());
            Assert.Equal(2, playerTeam.Seats.Count);
            Assert.Equal(2, opponentTeam.Seats.Count);
            Assert.Contains(0, playerTeam.Seats);
            Assert.Contains(2, playerTeam.Seats);
            Assert.Contains(1, opponentTeam.Seats);
            Assert.Contains(3, opponentTeam.Seats);

            // Players should be mapped correctly
            Assert.Equal(4, response.Players.Count);
            Assert.Equal(playerName, response.Players.First(p => p.Seat == 0).Name);
            Assert.Equal("PlayerTeam", response.Players.First(p => p.Seat == 0).Team);
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
        public async Task MapGameStateToStartGameResponse_WithDevMode_ShouldShowCards()
        {
            // Arrange
            var devModeSettings = new Dictionary<string, string?> {
                {"FeatureFlags:DevMode", "true"},
            };            var devConfig = new ConfigurationBuilder()
                .AddInMemoryCollection(devModeSettings)
                .Build();

            var gameStateManager = CreateGameStateManager(devConfig);
            string playerName = "TestPlayer";
            var game = await gameStateManager.CreateGameAsync(playerName);

            // Act - Note we pass true for showAllHands param
            var response = _mappingService.MapGameStateToStartGameResponse(game, 0, true);            
            // Assert
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
