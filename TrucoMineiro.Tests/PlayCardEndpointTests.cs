using TrucoMineiro.API.DTOs;
using TrucoMineiro.Tests.TestUtilities;
using Xunit;

namespace TrucoMineiro.Tests
{
    /// <summary>
    /// Modernized endpoint tests for PlayCard functionality with simplified response format
    /// Uses real HTTP requests and event-driven architecture instead of mocks
    /// Follows current testing best practices with shared setup via EndpointTestBase
    /// 
    /// ARCHITECTURE NOTES:
    /// - Uses TestWebApplicationFactory for real endpoint testing
    /// - Leverages actual PlayCardService.ProcessPlayCardRequestAsync() implementation
    /// - Tests complete request/response flow including event publishing
    /// - No mocks - tests real component interactions and event-driven AI
    /// - PlayCard returns simplified status-only response; clients poll GetGame for state updates
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

            // Assert - Check simplified response
            Assert.True(response.Success);
            Assert.Equal("Card played successfully", response.Message);
            Assert.Null(response.Error);
            
            // Verify game state by polling GetGame endpoint (following the new pattern)
            var gameState = await GetGameStateAsync(gameResponse.GameId, humanPlayer.Seat);
            Assert.NotNull(gameState);
            
            // Player should have 2 cards left after playing one
            var humanPlayerInState = gameState.Players.First(p => p.Seat == humanPlayer.Seat);
            Assert.Equal(2, humanPlayerInState.Hand.Count);
            
            // Should have all 4 players in the game
            Assert.Equal(4, gameState.Players.Count);
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
            Assert.Null(response.Message);
            Assert.Equal("Game not found", response.Error);
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
            Assert.Null(response.Message);
            Assert.Contains("Invalid card index", response.Error);
        }

        [Fact]
        public async Task PlayCard_ShouldReturnError_WhenInvalidPlayerSeat()
        {
            // Arrange
            var gameResponse = await CreateGameAsync("TestPlayer");
            var request = CreatePlayCardRequest(gameResponse.GameId, 99, 0); // Invalid player seat

            // Act
            var response = await PlayCardAsync(request);

            // Assert
            Assert.False(response.Success);
            Assert.Null(response.Message);
            // The error message should indicate invalid request parameters since 99 is out of range (0-3)
            Assert.Contains("Invalid request parameters", response.Error);
        }

        /// <summary>
        /// Helper class for fold tests that need to disable AI auto-play
        /// to verify fold cards before round cleanup occurs
        /// </summary>
        private class FoldTestHelper : EndpointTestBase
        {
            public FoldTestHelper() : base(GetConfigWithoutAutoAiPlay())
            {
            }

            public new async Task<StartGameResponse> CreateGameAsync(string playerName) => await base.CreateGameAsync(playerName);
            public new async Task<PlayCardResponseDto> PlayCardAsync(PlayCardRequestDto request) => await base.PlayCardAsync(request);
            public new async Task<GameStateDto> GetGameStateAsync(string gameId, int? playerSeat = null) => await base.GetGameStateAsync(gameId, playerSeat);
            public new PlayerInfoDto GetHumanPlayer(StartGameResponse gameResponse) => base.GetHumanPlayer(gameResponse);
            public new PlayCardRequestDto CreatePlayCardRequest(string gameId, int playerSeat, int cardIndex, bool isFold = false) => base.CreatePlayCardRequest(gameId, playerSeat, cardIndex, isFold);
        }

        /// <summary>
        /// Helper class for AutoAiPlay testing - disables AI auto-play
        /// </summary>
        private class AutoAiPlayTestHelper : EndpointTestBase
        {
            public AutoAiPlayTestHelper() : base(GetConfigWithoutAutoAiPlay())
            {
            }

            public new async Task<StartGameResponse> CreateGameAsync(string playerName) => await base.CreateGameAsync(playerName);
            public new async Task<PlayCardResponseDto> PlayCardAsync(PlayCardRequestDto request) => await base.PlayCardAsync(request);
            public new async Task<GameStateDto> GetGameStateAsync(string gameId, int? playerSeat = null) => await base.GetGameStateAsync(gameId, playerSeat);
            public new PlayerInfoDto GetHumanPlayer(StartGameResponse gameResponse) => base.GetHumanPlayer(gameResponse);
            public new PlayCardRequestDto CreatePlayCardRequest(string gameId, int playerSeat, int cardIndex, bool isFold = false) => base.CreatePlayCardRequest(gameId, playerSeat, cardIndex, isFold);
        }

        /// <summary>
        /// Helper class for DevMode testing - enables DevMode
        /// </summary>
        private class DevModeTestHelper : EndpointTestBase
        {
            public DevModeTestHelper() : base(GetFastTestConfigWithDevMode())
            {
            }

            public new async Task<StartGameResponse> CreateGameAsync(string playerName) => await base.CreateGameAsync(playerName);
            public new async Task<PlayCardResponseDto> PlayCardAsync(PlayCardRequestDto request) => await base.PlayCardAsync(request);
            public new async Task<GameStateDto> GetGameStateAsync(string gameId, int? playerSeat = null) => await base.GetGameStateAsync(gameId, playerSeat);
            public new PlayerInfoDto GetHumanPlayer(StartGameResponse gameResponse) => base.GetHumanPlayer(gameResponse);
            public new PlayCardRequestDto CreatePlayCardRequest(string gameId, int playerSeat, int cardIndex, bool isFold = false) => base.CreatePlayCardRequest(gameId, playerSeat, cardIndex, isFold);        }
    }
}
