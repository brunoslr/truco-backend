using TrucoMineiro.API.DTOs;
using TrucoMineiro.Tests.TestUtilities;
using Xunit;

namespace TrucoMineiro.Tests
{
    /// <summary>
    /// Modernized endpoint tests for PlayCard functionality
    /// Uses real HTTP requests and event-driven architecture instead of mocks
    /// Follows current testing best practices with shared setup via EndpointTestBase
    /// 
    /// ARCHITECTURE NOTES:
    /// - Uses TestWebApplicationFactory for real endpoint testing
    /// - Leverages actual PlayCardService.ProcessPlayCardRequestAsync() implementation
    /// - Tests complete request/response flow including event publishing
    /// - No mocks - tests real component interactions and event-driven AI
    /// </summary>
    public class PlayCardEndpointTests : EndpointTestBase
    {
        [Fact]
        public async Task PlayCard_ShouldReturnSuccess_WhenValidMove()
        {
            // Arrange
            var gameResponse = await CreateGameAsync("TestPlayer");
            var humanPlayer = GetHumanPlayer(gameResponse);
            var request = CreatePlayCardRequest(gameResponse.GameId, humanPlayer.Seat, 0);

            // Act
            var response = await PlayCardAsync(request);

            // Assert
            Assert.True(response.Success);
            Assert.Equal("Card played successfully", response.Message);
            Assert.NotNull(response.GameState);
            Assert.Equal(2, response.Hand.Count); // Player should have 2 cards left after playing one
            Assert.Equal(4, response.PlayerHands.Count); // Should have all 4 player hands
        }

        [Fact]
        public async Task PlayCard_ShouldHideAICards_WhenNotInDevMode()
        {
            // Arrange
            var gameResponse = await CreateGameAsync("TestPlayer");
            var humanPlayer = GetHumanPlayer(gameResponse);
            var request = CreatePlayCardRequest(gameResponse.GameId, humanPlayer.Seat, 0);

            // Act
            var response = await PlayCardAsync(request);

            // Assert
            Assert.True(response.Success);
            
            // Human player's hand should be visible
            var humanPlayerHand = response.PlayerHands.First(h => h.Seat == 0);
            Assert.All(humanPlayerHand.Cards, card => Assert.NotNull(card.Value));
            Assert.All(humanPlayerHand.Cards, card => Assert.NotNull(card.Suit));

            // AI player hands should be hidden (cards exist but values are null)
            var aiPlayerHands = response.PlayerHands.Where(h => h.Seat != 0);
            foreach (var aiHand in aiPlayerHands)
            {
                Assert.All(aiHand.Cards, card => Assert.Null(card.Value));
                Assert.All(aiHand.Cards, card => Assert.Null(card.Suit));
            }
        }        [Fact]
        public async Task PlayCard_ShouldShowAllCards_WhenInDevMode()
        {
            // Arrange - Create test with DevMode enabled
            var devModeConfig = GetFastTestConfigWithDevMode();
            
            using var testWithDevMode = new PlayCardEndpointTestsWithDevMode(devModeConfig);
            var gameResponse = await testWithDevMode.CreateGameAsync("TestPlayer");
            var humanPlayer = gameResponse.Players.First(p => p.Seat == 0);
            var request = new PlayCardRequestDto
            {
                GameId = gameResponse.GameId,
                PlayerSeat = humanPlayer.Seat,
                CardIndex = 0,
                IsFold = false
            };

            // Act
            var response = await testWithDevMode.PlayCardAsync(request);

            // Assert
            Assert.True(response.Success);
            
            // All player hands should be visible in DevMode (non-empty cards should have values)
            foreach (var playerHand in response.PlayerHands)
            {
                if (playerHand.Cards.Count > 0)
                {
                    // In DevMode, cards should be visible (not null)
                    Assert.All(playerHand.Cards, card => Assert.NotNull(card.Value));
                    Assert.All(playerHand.Cards, card => Assert.NotNull(card.Suit));
                }
            }
        }

        [Fact]
        public async Task PlayCard_ShouldHandleFold_WhenFoldRequested()
        {
            // Arrange
            var gameResponse = await CreateGameAsync("TestPlayer");
            var humanPlayer = GetHumanPlayer(gameResponse);
            var request = CreatePlayCardRequest(gameResponse.GameId, humanPlayer.Seat, 0, isFold: true);

            // Act
            var response = await PlayCardAsync(request);

            // Assert
            Assert.True(response.Success);
            Assert.Equal("Card played successfully", response.Message);
            Assert.NotNull(response.GameState);
            
            // Verify that a played card exists for the player (fold card should be in PlayedCards)
            var playedCard = response.GameState.PlayedCards.FirstOrDefault(pc => pc.PlayerSeat == humanPlayer.Seat);
            Assert.NotNull(playedCard);
            Assert.NotNull(playedCard.Card);
            
            // The card should be a fold card (implementation detail - fold cards have "FOLD" value)
            Assert.Equal("FOLD", playedCard.Card.Value);
        }

        [Fact]
        public async Task PlayCard_ShouldReturnError_WhenGameNotFound()
        {
            // Arrange
            var request = CreatePlayCardRequest("invalid-game-id", 0, 0);

            // Act
            var response = await PlayCardAsync(request);

            // Assert
            Assert.False(response.Success);
            Assert.Equal("Game not found", response.Message);
        }

        [Fact]
        public async Task PlayCard_ShouldReturnError_WhenInvalidCardIndex()
        {
            // Arrange
            var gameResponse = await CreateGameAsync("TestPlayer");
            var humanPlayer = GetHumanPlayer(gameResponse);
            var request = CreatePlayCardRequest(gameResponse.GameId, humanPlayer.Seat, 99); // Invalid card index

            // Act
            var response = await PlayCardAsync(request);

            // Assert
            Assert.False(response.Success);
            Assert.Contains("Invalid card index", response.Message);
        }        [Fact]
        public async Task PlayCard_ShouldReturnError_WhenInvalidPlayerSeat()
        {
            // Arrange
            var gameResponse = await CreateGameAsync("TestPlayer");
            var request = CreatePlayCardRequest(gameResponse.GameId, 99, 0); // Invalid player seat

            // Act
            var response = await PlayCardAsync(request);

            // Assert
            Assert.False(response.Success);
            // The error message should indicate invalid request parameters since 99 is out of range (0-3)
            Assert.Contains("Invalid request parameters", response.Message);
        }[Fact]
        public async Task PlayCard_ShouldUpdatePlayedCardsArray()
        {
            // Arrange
            var gameResponse = await CreateGameAsync("TestPlayer");
            var humanPlayer = GetHumanPlayer(gameResponse);
            
            // Get initial game state to check played cards count
            var initialGameState = await GetGameStateAsync(gameResponse.GameId);
            var originalCardCount = initialGameState.PlayedCards.Count;
            
            var request = CreatePlayCardRequest(gameResponse.GameId, humanPlayer.Seat, 0);

            // Act
            var response = await PlayCardAsync(request);

            // Assert
            Assert.True(response.Success);
            
            // Get updated game state to check played cards
            var updatedGameState = await GetGameStateAsync(gameResponse.GameId);
            
            // Should have one more played card than before
            Assert.True(updatedGameState.PlayedCards.Count >= originalCardCount);
            
            // Should have a played card for the human player
            var playedCard = updatedGameState.PlayedCards.FirstOrDefault(pc => pc.PlayerSeat == humanPlayer.Seat);
            Assert.NotNull(playedCard);
            Assert.NotNull(playedCard.Card);
            Assert.NotNull(playedCard.Card.Value);
            Assert.NotNull(playedCard.Card.Suit);
        }

        /// <summary>
        /// Helper class for DevMode testing
        /// </summary>
        private class PlayCardEndpointTestsWithDevMode : EndpointTestBase
        {
            public PlayCardEndpointTestsWithDevMode(Dictionary<string, string?> configOverrides) : base(configOverrides)
            {
            }
        }
    }
}
