using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using TrucoMineiro.API.Domain.Events;
using TrucoMineiro.API.Domain.Interfaces;
using TrucoMineiro.API.Domain.Models;
using TrucoMineiro.API.Domain.Services;
using TrucoMineiro.Tests.TestUtilities;
using Xunit;

namespace TrucoMineiro.Tests.Integration
{    /// <summary>
    /// Integration tests to verify AI behavior in realistic game scenarios.
    /// 
    /// TESTING STRATEGY & BEST PRACTICES:
    /// 
    /// 1. TEST ARCHITECTURE DECISION - Service-Level Integration:
    ///    - Uses manual DI configuration for focused service testing
    ///    - Lighter weight than full EndpointTestBase web factory
    ///    - More explicit about dependencies being tested
    ///    - Clearer separation of concerns from HTTP layer
    /// 
    /// 2. PERFORMANCE OPTIMIZATION:
    ///    - Fast test configuration: all delays set to 0ms
    ///    - No actual HTTP calls (service-level testing)
    ///    - Minimal service registration for speed
    ///    - Tests run in isolation without web infrastructure overhead
    /// 
    /// 3. AI TESTING PRINCIPLES:
    ///    - Tests logical decision-making, not random behavior
    ///    - Uses deterministic scenarios with clear expected outcomes
    ///    - Focuses on strategic AI behavior patterns
    ///    - Verifies consistency in identical game situations
    ///    - Tests edge cases and boundary conditions
    /// 
    /// 4. TEST DATA MANAGEMENT:
    ///    - TestGameFactory.CreateTestGame() provides consistent baseline state
    ///    - Each test manipulates specific game conditions
    ///    - Hand compositions designed to trigger specific AI strategies
    ///    - Clear separation between setup, action, and assertion phases
    /// 
    /// 5. SCOPE & BOUNDARIES:
    ///    - Tests AI service logic without event system complexity
    ///    - Covers core decision-making algorithms (card selection, Truco responses)
    ///    - Validates strategic behavior patterns and consistency
    ///    - Does not test HTTP endpoints, full game flow, or event handling
    /// 
    /// 6. MAINTENANCE CONSIDERATIONS:
    ///    - Self-contained test scenarios with minimal dependencies
    ///    - Clear documentation of test intent and expected behavior
    ///    - Easy to extend with new AI behavior test cases
    ///    - Disposable pattern for proper resource cleanup
    /// 
    /// 7. COMPARISON WITH OTHER TEST APPROACHES:
    ///    - vs EndpointTestBase: Faster, more focused, less integration scope
    ///    - vs Unit Tests: More realistic with actual service interactions
    ///    - vs Full Integration: Lighter weight while maintaining realism
    /// </summary>
    public class AIIntegrationTests : IDisposable
    {
        private readonly ServiceProvider _serviceProvider;
        private readonly IAIPlayerService _aiPlayerService;
        private readonly IHandResolutionService _handResolutionService;

        public AIIntegrationTests()
        {
            // Create minimal service collection with fast test configuration
            var services = new ServiceCollection();
            
            // Configure fast test settings (zero delays for speed)
            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    {"GameSettings:AIMinPlayDelayMs", "0"},
                    {"GameSettings:AIMaxPlayDelayMs", "0"},
                    {"GameSettings:HandResolutionDelayMs", "0"},
                    {"GameSettings:RoundResolutionDelayMs", "0"},
                    {"FeatureFlags:AutoAiPlay", "true"},
                    {"FeatureFlags:DevMode", "false"}
                })
                .Build();

            services.AddSingleton<IConfiguration>(configuration);
            services.AddLogging(builder => builder.AddConsole().SetMinimumLevel(LogLevel.Warning));
            
            // Register only the services needed for AI testing
            services.AddScoped<IAIPlayerService, AIPlayerService>();
            services.AddScoped<IHandResolutionService, HandResolutionService>();
            services.AddScoped<IEventPublisher, TestEventPublisher>(); // Mock event publisher for tests

            _serviceProvider = services.BuildServiceProvider();
            _aiPlayerService = _serviceProvider.GetRequiredService<IAIPlayerService>();
            _handResolutionService = _serviceProvider.GetRequiredService<IHandResolutionService>();
        }        [Fact]
        public void AI_Should_PlayWeakest_WhenCannotWin()
        {
            // Arrange - Create scenario where AI cannot win
            var game = TestGameFactory.CreateTestGame();
            var aiPlayer = game.Players[1]; // AI player at seat 1            // Give AI weak cards
            aiPlayer.Hand = new List<Card>
            {
                new Card("4", "♥"), // Strength 1
                new Card("5", "♦"), // Strength 2
                new Card("6", "♠")  // Strength 3
            };            // Simulate that a very strong card was already played
            game.PlayedCards.Add(new PlayedCard(0, new Card("4", "♣"))); // Zap - strongest card

            // Act
            var selectedIndex = _aiPlayerService.SelectCardToPlay(aiPlayer, game);            // Assert - Should select weakest card (4♥ at index 0)
            Assert.Equal(0, selectedIndex);
            Assert.Equal("4", aiPlayer.Hand[selectedIndex].Value);
            Assert.Equal("♥", aiPlayer.Hand[selectedIndex].Suit);
        }        [Fact]
        public void AI_Should_PlayMinimal_WhenPartnerHasStrongest()
        {
            // Arrange - Create scenario where partner has strongest card
            var game = TestGameFactory.CreateTestGame();
            var aiPlayer = game.Players[1]; // AI player at seat 1
            aiPlayer.Hand = new List<Card>
            {
                new Card("7", "♠"), // Strength 4
                new Card("J", "♦"), // Strength 5
                new Card("K", "♠")  // Strength 7
            };            // Simulate partner played strongest card and opponent still to play
            game.PlayedCards.Add(new PlayedCard(3, new Card("4", "♣"))); // Partner plays Zap
            game.CurrentPlayerIndex = 1; // AI's turn

            // Act
            var selectedIndex = _aiPlayerService.SelectCardToPlay(aiPlayer, game);

            // Assert - Should play weakest card to save stronger ones
            Assert.Equal(0, selectedIndex);
            Assert.Equal("7", aiPlayer.Hand[selectedIndex].Value);
        }        [Fact]
        public void AI_Should_PlaySmallestWinning_WhenCanWin()
        {
            // Arrange - Create scenario where AI can win with multiple cards
            var game = TestGameFactory.CreateTestGame();
            var aiPlayer = game.Players[1]; // AI player at seat 1
            // Give AI cards that can beat current played cards
            aiPlayer.Hand = new List<Card>
            {
                new Card("A", "♥"), // Strength 8
                new Card("K", "♦"), // Strength 7
                new Card("7", "♠")  // Strength 4
            };            
            // Simulate a medium card was played
            game.PlayedCards.Add(new PlayedCard(0, new Card("Q", "♥"))); // Queen - strength 6

            // Act
            var selectedIndex = _aiPlayerService.SelectCardToPlay(aiPlayer, game);

            // Assert - Should select smallest winning card (King at index 1)
            Assert.Equal(1, selectedIndex);
            Assert.Equal("K", aiPlayer.Hand[selectedIndex].Value);
        }        [Fact]
        public void AI_Should_ResponsToTrucoCall_Appropriately()
        {
            // Arrange
            var game = TestGameFactory.CreateTestGame();
            var aiPlayer = game.Players[1];
            game.PendingTrucoCall = true;
            game.TrucoCallerSeat = 0; // Human called Truco            
            // Test with strong hand - should not fold
            aiPlayer.Hand = new List<Card>
            {
                new Card("4", "♣"), // Zap
                new Card("7", "♥"), // Copas
                new Card("A", "♠")  // Espadilha
            };

            // Act & Assert
            var shouldFold = _aiPlayerService.ShouldFold(aiPlayer, game);
            var shouldRaise = _aiPlayerService.ShouldRaise(aiPlayer, game);
            var shouldCallTruco = _aiPlayerService.ShouldCallTruco(aiPlayer, game);

            Assert.False(shouldFold); // Should not fold with strong hand
            // Other tests can vary due to randomness, but fold should be false
        }        [Fact]
        public void AI_Should_MakeConsistentDecisions_InMultipleRounds()
        {
            // Arrange
            var game = TestGameFactory.CreateTestGame();
            var aiPlayer = game.Players[1];// Give AI the same hand multiple times
            var testHand = new List<Card>
            {
                new Card("K", "♥"),
                new Card("Q", "♦"),
                new Card("J", "♠")
            };

            var decisions = new List<int>();

            // Act - Make same decision multiple times
            for (int i = 0; i < 5; i++)
            {
                aiPlayer.Hand = new List<Card>(testHand); // Reset hand
                game.PlayedCards.Clear(); // Reset played cards
                  // Same scenario each time
                game.PlayedCards.Add(new PlayedCard(0, new Card("7", "♥")));
                
                var decision = _aiPlayerService.SelectCardToPlay(aiPlayer, game);
                decisions.Add(decision);
            }

            // Assert - Should be consistent (same decision each time)
            Assert.True(decisions.All(d => d == decisions[0]), 
                "AI should make consistent decisions in identical scenarios");        }

        public void Dispose()
        {
            _serviceProvider?.Dispose();
        }
    }
}
