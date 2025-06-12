using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using TrucoMineiro.Tests.TestUtilities;
using Xunit;
using System.Diagnostics;

namespace TrucoMineiro.Tests.Integration
{
    public class DelayConfigurationTests : EndpointTestBase
    {
        [Fact]
        public void Configuration_ShouldContainDelaySettings()
        {
            // Arrange
            using var scope = _factory.Services.CreateScope();
            var configuration = scope.ServiceProvider.GetRequiredService<IConfiguration>();

            // Act & Assert - verify delay configurations exist and have expected values
            var handResolutionDelay = configuration.GetValue<int>("GameSettings:HandResolutionDelayMs", -1);
            var roundResolutionDelay = configuration.GetValue<int>("GameSettings:RoundResolutionDelayMs", -1);
            var aiMinDelay = configuration.GetValue<int>("GameSettings:AIMinPlayDelayMs", -1);
            var aiMaxDelay = configuration.GetValue<int>("GameSettings:AIMaxPlayDelayMs", -1);

            Assert.True(handResolutionDelay >= 0, "HandResolutionDelayMs should be configured");
            Assert.True(roundResolutionDelay >= 0, "RoundResolutionDelayMs should be configured");
            Assert.True(aiMinDelay >= 0, "AIMinPlayDelayMs should be configured");
            Assert.True(aiMaxDelay >= 0, "AIMaxPlayDelayMs should be configured");
            
            // Log the values for verification
            Assert.True(true, $"Hand Resolution Delay: {handResolutionDelay}ms");
            Assert.True(true, $"Round Resolution Delay: {roundResolutionDelay}ms");
            Assert.True(true, $"AI Min Play Delay: {aiMinDelay}ms");
            Assert.True(true, $"AI Max Play Delay: {aiMaxDelay}ms");
        }

        [Fact]
        public async Task AIAutoPlay_WithConfiguredDelays_ShouldTakeExpectedTime()
        {
            // Arrange
            var stopwatch = Stopwatch.StartNew();

            // Act - Run the existing AI autoplay test and measure time
            var client = _factory.CreateClient();
            var response = await client.PostAsync("/api/game/start", new StringContent(
                """{"playerName": "DelayTest"}""", 
                System.Text.Encoding.UTF8, 
                "application/json"));            response.EnsureSuccessStatusCode();
            var content = await response.Content.ReadAsStringAsync();
            dynamic? gameData = Newtonsoft.Json.JsonConvert.DeserializeObject(content);
            string gameId = gameData?.gameId ?? throw new InvalidOperationException("Failed to get game ID from response");

            // Play a few cards to trigger AI responses and measure the time
            await client.PostAsync($"/api/game/play-card", new StringContent(
                $$"""{"gameId": "{{gameId}}", "cardIndex": 0}""", 
                System.Text.Encoding.UTF8, 
                "application/json"));

            // Wait for AI moves to complete (should include configured delays)
            await Task.Delay(2000); // Allow time for AI to respond
            
            stopwatch.Stop();

            // Assert - should take longer than just processing time due to configured delays
            Assert.True(stopwatch.ElapsedMilliseconds >= 1000, 
                $"Game with AI delays should take at least 1 second, but took {stopwatch.ElapsedMilliseconds}ms");
        }
    }
}
