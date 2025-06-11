using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using TrucoMineiro.API.Domain.Interfaces;
using TrucoMineiro.API.Domain.Models;
using TrucoMineiro.API.Domain.Services;
using TrucoMineiro.API.Domain.Events;
using Xunit;

namespace TrucoMineiro.Tests.Integration
{
    /// <summary>
    /// Integration tests to verify AI behavior in realistic game scenarios
    /// </summary>
    public class AIIntegrationTests
    {
        private readonly IAIPlayerService _aiPlayerService;
        private readonly IHandResolutionService _handResolutionService;        public AIIntegrationTests()
        {
            var services = new ServiceCollection();
            services.AddLogging(builder => builder.AddConsole());
            
            // Add required dependencies for HandResolutionService
            services.AddSingleton<IEventPublisher, InMemoryEventPublisher>();
            services.AddScoped<IHandResolutionService, HandResolutionService>();
            services.AddScoped<IAIPlayerService, AIPlayerService>();

            var serviceProvider = services.BuildServiceProvider();
            _aiPlayerService = serviceProvider.GetRequiredService<IAIPlayerService>();
            _handResolutionService = serviceProvider.GetRequiredService<IHandResolutionService>();
        }

        [Fact]
        public void AI_Should_PlayWeakest_WhenCannotWin()
        {
            // Arrange - Create scenario where AI cannot win
            var game = CreateTestGame();
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
        }

        [Fact]
        public void AI_Should_PlayMinimal_WhenPartnerHasStrongest()
        {
            // Arrange - Create scenario where partner has strongest card
            var game = CreateTestGame();
            var aiPlayer = game.Players[1]; // AI player at seat 1
            var partnerPlayer = game.Players[3]; // Partner at seat 3            // Give AI medium cards
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
        }

        [Fact]
        public void AI_Should_PlaySmallestWinning_WhenCanWin()
        {
            // Arrange - Create scenario where AI can win with multiple cards
            var game = CreateTestGame();
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
        }

        [Fact]
        public void AI_Should_ResponsToTrucoCall_Appropriately()
        {
            // Arrange
            var game = CreateTestGame();
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
        }

        [Fact]
        public void AI_Should_MakeConsistentDecisions_InMultipleRounds()
        {
            // Arrange
            var game = CreateTestGame();
            var aiPlayer = game.Players[1];            // Give AI the same hand multiple times
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
                "AI should make consistent decisions in identical scenarios");
        }

        private GameState CreateTestGame()
        {
            var game = new GameState();
            game.InitializeGame("TestPlayer");
            
            // Ensure AI players are properly marked
            game.Players[1].IsAI = true;
            game.Players[2].IsAI = false; // Partner
            game.Players[3].IsAI = true;

            return game;
        }
    }
}
