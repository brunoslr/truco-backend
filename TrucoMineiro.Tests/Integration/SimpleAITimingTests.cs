using System.Diagnostics;
using Microsoft.AspNetCore.Mvc.Testing;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using TrucoMineiro.API.DTOs;
using TrucoMineiro.Tests.Integration;
using TrucoMineiro.Tests.TestUtilities;

namespace TrucoMineiro.Tests.Integration
{
    /// <summary>
    /// Simple integration tests to verify AI timing behavior works with new configuration
    /// </summary>
    public class SimpleAITimingTests : EndpointTestBase
    {
        [Fact]
        public async Task AI_Should_Use_Zero_Delays_In_Test_Environment()
        {
            // Arrange: Use default test configuration (zero delays)
            var stopwatch = Stopwatch.StartNew();
            
            // Act: Start game and trigger AI actions
            var gameState = await CreateGameAsync("TimingTestPlayer");
            var gameId = gameState.GameId;

            // Play a card to trigger AI responses
            var playCardRequest = new PlayCardRequestDto
            {
                GameId = gameId,
                PlayerSeat = 0,
                CardIndex = 0
            };

            await PlayCardAsync(playCardRequest);
            stopwatch.Stop();            // Assert: Total time should be very fast with zero delays
            Assert.True(stopwatch.ElapsedMilliseconds < 500, 
                $"AI actions took too long: {stopwatch.ElapsedMilliseconds}ms (expected < 500ms with zero delays)");
        }

        [Fact]
        public async Task AI_Should_Use_Custom_Delays_When_Configured()
        {
            // Arrange: Create custom configuration with measurable delays
            var customConfig = new Dictionary<string, string?>
            {
                ["GameSettings:AIMinPlayDelayMs"] = "100",
                ["GameSettings:AIMaxPlayDelayMs"] = "200",
                ["GameSettings:HandResolutionDelayMs"] = "100",
                ["GameSettings:RoundResolutionDelayMs"] = "100",
                ["FeatureFlags:AutoAiPlay"] = "true",
                ["FeatureFlags:DevMode"] = "false"
            };

            using var factory = TestWebApplicationFactory.WithConfig(customConfig);
            using var client = factory.CreateClient();

            var stopwatch = Stopwatch.StartNew();

            // Act: Start game and trigger AI actions
            var gameState = await CreateGameAsync(client, "TimingTestPlayer");
            var gameId = gameState.GameId;

            // Play a card to trigger AI responses  
            var playCardRequest = new PlayCardRequestDto
            {
                GameId = gameId,
                PlayerSeat = 0,
                CardIndex = 0
            };

            await PlayCardAsync(client, playCardRequest);
            stopwatch.Stop();            // Assert: Total time should be longer with custom delays
            Assert.True(stopwatch.ElapsedMilliseconds >= 50, 
                $"AI actions too fast: {stopwatch.ElapsedMilliseconds}ms (expected >= 50ms with custom delays)");
            
            // Should not be excessively long either (allowing for processing overhead)
            Assert.True(stopwatch.ElapsedMilliseconds < 2000, 
                $"AI actions took too long: {stopwatch.ElapsedMilliseconds}ms (expected < 2000ms)");
        }

        /// <summary>
        /// Helper method to create a game using a specific client
        /// </summary>
        private async Task<StartGameResponse> CreateGameAsync(HttpClient client, string playerName)
        {
            var startGameRequest = new StartGameRequest { PlayerName = playerName };
            var json = JsonSerializer.Serialize(startGameRequest);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await client.PostAsync("/api/game/start", content);
            response.EnsureSuccessStatusCode();

            var result = await response.Content.ReadFromJsonAsync<StartGameResponse>();
            Assert.NotNull(result);
            
            return result;
        }

        /// <summary>
        /// Helper method to play a card using a specific client
        /// </summary>
        private async Task<PlayCardResponseDto> PlayCardAsync(HttpClient client, PlayCardRequestDto request)
        {
            var response = await client.PostAsJsonAsync("/api/game/play-card", request);
            response.EnsureSuccessStatusCode();

            var result = await response.Content.ReadFromJsonAsync<PlayCardResponseDto>();
            Assert.NotNull(result);
            
            return result;
        }
    }
}
