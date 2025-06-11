using TrucoMineiro.API.DTOs;
using TrucoMineiro.Tests.TestUtilities;
using Xunit;

namespace TrucoMineiro.Tests
{
    /// <summary>
    /// Modernized endpoint tests for GetGameState functionality
    /// Uses real HTTP requests and shared test infrastructure from EndpointTestBase
    /// Tests player-specific card visibility and DevMode behavior
    /// 
    /// ARCHITECTURE NOTES:
    /// - Uses TestWebApplicationFactory for real endpoint testing
    /// - Tests complete HTTP request/response flow
    /// - No mocks - tests real component interactions
    /// - Validates card visibility rules and configuration behavior
    /// </summary>
    public class GetGameStateEndpointTests : EndpointTestBase
    {
        [Fact]
        public async Task GetGameState_WithoutPlayerSeat_ShouldShowOnlyHumanPlayerCards()
        {
            // Arrange
            var gameResponse = await CreateGameAsync("TestPlayer");
            
            // Act
            var gameState = await GetGameStateAsync(gameResponse.GameId);
            
            // Assert
            // Human player (seat 0) should have visible cards
            var humanPlayer = gameState.Players.First(p => p.Seat == 0);
            Assert.All(humanPlayer.Hand, card => 
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
        }

        [Fact]
        public async Task GetGameState_WithPlayerSeat_ShouldShowOnlyRequestingPlayerCards()
        {
            // Arrange
            var gameResponse = await CreateGameAsync("TestPlayer");
            const int requestingPlayerSeat = 1; // AI player
            
            // Act - Request as AI player at seat 1
            var gameState = await GetGameStateAsync(gameResponse.GameId, requestingPlayerSeat);
            
            // Assert
            // AI player at seat 1 should have visible cards
            var requestingPlayer = gameState.Players.First(p => p.Seat == requestingPlayerSeat);
            Assert.All(requestingPlayer.Hand, card => 
            {
                Assert.NotNull(card.Value);
                Assert.NotNull(card.Suit);
            });
            
            // All other players should have hidden cards
            var otherPlayers = gameState.Players.Where(p => p.Seat != requestingPlayerSeat);
            foreach (var otherPlayer in otherPlayers)
            {
                Assert.All(otherPlayer.Hand, card => 
                {
                    Assert.Null(card.Value);
                    Assert.Null(card.Suit);
                });
            }
        }

        [Fact]
        public async Task GetGameState_WithInvalidPlayerSeat_ShouldReturnBadRequest()
        {
            // Arrange
            var gameResponse = await CreateGameAsync("TestPlayer");
            const int invalidPlayerSeat = 999; // Using a seat number that doesn't exist
            
            // Act & Assert
            var exception = await Assert.ThrowsAsync<HttpRequestException>(
                async () => await GetGameStateAsync(gameResponse.GameId, invalidPlayerSeat));
            
            // The API should return a 400 Bad Request for invalid seat numbers
            Assert.Contains("400", exception.Message);
        }

        [Fact]
        public async Task GetGameState_WithInvalidGameId_ShouldReturnNotFound()
        {
            // Arrange
            const string invalidGameId = "non-existent-game";
            
            // Act & Assert
            var exception = await Assert.ThrowsAsync<HttpRequestException>(
                async () => await GetGameStateAsync(invalidGameId));
            
            // The API should return a 404 Not Found for non-existent games
            Assert.Contains("404", exception.Message);
        }        [Fact]
        public async Task GetGameState_WithDevMode_ShouldShowAllCards()
        {
            // Arrange - Create test instance with DevMode configuration
            using var devModeTest = new DevModeTestHelper();
            
            // Act
            var gameResponse = await devModeTest.CreateGameAsync("TestPlayer");
            var gameState = await devModeTest.GetGameStateAsync(gameResponse.GameId);
            
            // Assert - All players should have visible cards in DevMode
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
        public async Task GetGameState_CardCount_ShouldAlwaysBeCorrect()
        {
            // Arrange
            var gameResponse = await CreateGameAsync("TestPlayer");
            
            // Act
            var gameState = await GetGameStateAsync(gameResponse.GameId);
            
            // Assert
            // All players should have 3 cards each (standard Truco hand)
            foreach (var player in gameState.Players)
            {
                Assert.Equal(3, player.Hand.Count);
            }
        }

        [Fact]
        public async Task GetGameState_MultipleRequests_ShouldBeConsistent()
        {
            // Arrange
            var gameResponse = await CreateGameAsync("TestPlayer");
            const int humanSeat = 0;
            const int aiSeat = 1;
            
            // Act - Make multiple requests as different players
            var humanGameState = await GetGameStateAsync(gameResponse.GameId, humanSeat);
            var aiGameState = await GetGameStateAsync(gameResponse.GameId, aiSeat);
            var defaultGameState = await GetGameStateAsync(gameResponse.GameId);
            
            // Assert
            // All responses should have the same basic game info
            Assert.Equal(humanGameState.Stakes, aiGameState.Stakes);
            Assert.Equal(humanGameState.CurrentHand, aiGameState.CurrentHand);
            Assert.Equal(humanGameState.Players.Count, aiGameState.Players.Count);
            
            // But card visibility should be different
            var humanPlayerInHumanView = humanGameState.Players.First(p => p.Seat == humanSeat);
            var humanPlayerInAiView = aiGameState.Players.First(p => p.Seat == humanSeat);
            
            // Human should see their own cards in human view, but not in AI view
            Assert.All(humanPlayerInHumanView.Hand, card => Assert.NotNull(card.Value));
            Assert.All(humanPlayerInAiView.Hand, card => Assert.Null(card.Value));
        }

        [Fact]
        public async Task GetGameState_IntegrationTest_ShouldWorkEndToEnd()
        {
            // Arrange
            var gameResponse = await CreateGameAsync("IntegrationTestPlayer");
            const int humanSeat = 0;
            const int aiSeat = 1;
            
            // Act & Assert - Test without playerSeat (should default to human player at seat 0)
            var defaultGameState = await GetGameStateAsync(gameResponse.GameId);
            
            // Human cards should be visible, AI cards should be hidden
            var humanInDefault = defaultGameState.Players.First(p => p.Seat == humanSeat);
            var aiInDefault = defaultGameState.Players.First(p => p.Seat == aiSeat);
            
            Assert.True(humanInDefault.Hand.All(c => c.Value != null && c.Suit != null), 
                "Human cards should be visible in default request");
            Assert.True(aiInDefault.Hand.All(c => c.Value == null && c.Suit == null), 
                "AI cards should be hidden in default request");
            
            // Act & Assert - Test with specific AI player seat
            var aiGameState = await GetGameStateAsync(gameResponse.GameId, aiSeat);
            
            // AI cards should be visible, human cards should be hidden
            var humanInAi = aiGameState.Players.First(p => p.Seat == humanSeat);
            var aiInAi = aiGameState.Players.First(p => p.Seat == aiSeat);
            
            Assert.True(humanInAi.Hand.All(c => c.Value == null && c.Suit == null), 
                "Human cards should be hidden when requesting as AI");
            Assert.True(aiInAi.Hand.All(c => c.Value != null && c.Suit != null), 
                "AI cards should be visible when requesting as AI");

            // Verify game state consistency
            Assert.Equal(defaultGameState.Stakes, aiGameState.Stakes);
            Assert.Equal(defaultGameState.CurrentHand, aiGameState.CurrentHand);
            Assert.Equal(defaultGameState.Players.Count, aiGameState.Players.Count);
        }    }

    /// <summary>
    /// Helper class for testing with DevMode enabled
    /// </summary>
    internal class DevModeTestHelper : EndpointTestBase
    {
        public DevModeTestHelper() : base(GetFastTestConfigWithDevMode())
        {
        }
    }
}
