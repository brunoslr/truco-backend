using Microsoft.AspNetCore.Mvc.Testing;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using TrucoMineiro.API;
using TrucoMineiro.API.DTOs;
using TrucoMineiro.Tests.Integration;
using Xunit;

namespace TrucoMineiro.Tests.Integration
{
    /// <summary>
    /// Integration tests for AI auto-play functionality using real event-driven architecture
    /// Tests the complete flow: Human plays card → Events published → AI players auto-play
    /// 
    /// MIGRATION NOTES:
    /// - Migrated from PlayCardEndpointTests.cs which used mocked event publishers
    /// - Now uses real HTTP endpoints and event-driven architecture 
    /// - Tests asynchronous event processing with proper timing considerations
    /// - Validates that AutoAiPlay feature flag correctly enables/disables AI auto-play
    /// </summary>
    public class AIAutoPlayIntegrationTests : IClassFixture<TestWebApplicationFactory>
    {
        private readonly TestWebApplicationFactory _factory;
        private readonly JsonSerializerOptions _jsonOptions = new() { PropertyNameCaseInsensitive = true };

        public AIAutoPlayIntegrationTests(TestWebApplicationFactory factory)
        {
            _factory = factory;
        }

        [Fact]
        public async Task PlayCard_ShouldTriggerAIAutoPlay_WhenAutoAiPlayEnabled()
        {
            // Arrange - Use the real factory (AutoAiPlay is enabled by default in appsettings)
            using var client = _factory.CreateClient();
            
            // Start a new game
            var gameState = await StartGameAsync(client);
            Assert.NotNull(gameState);
            
            // Verify initial state via game state endpoint
            var gameStateResponse = await client.GetAsync($"/api/game/{gameState.GameId}");
            gameStateResponse.EnsureSuccessStatusCode();
            
            var gameStateJson = await gameStateResponse.Content.ReadAsStringAsync();
            var currentGameState = JsonSerializer.Deserialize<GameStateDto>(gameStateJson, _jsonOptions);
            Assert.NotNull(currentGameState);
            
            // Initially no cards should be played
            Assert.Empty(currentGameState.PlayedCards);

            // Act - Human player plays a card
            var playCardRequest = new PlayCardRequestDto
            {
                GameId = gameState.GameId,
                PlayerSeat = 0, // Human player seat
                CardIndex = 0, // Play first card
                IsFold = false
            };

            var playCardJson = JsonSerializer.Serialize(playCardRequest, _jsonOptions);
            var playCardContent = new StringContent(playCardJson, Encoding.UTF8, "application/json");
            
            var playCardResponse = await client.PostAsync("/api/game/play-card", playCardContent);
            playCardResponse.EnsureSuccessStatusCode();

            // Wait for event-driven AI processing to complete
            await Task.Delay(1000);

            // Assert - Get updated game state and verify AI auto-play occurred
            var updatedGameStateResponse = await client.GetAsync($"/api/game/{gameState.GameId}");
            updatedGameStateResponse.EnsureSuccessStatusCode();
            
            var updatedGameStateJson = await updatedGameStateResponse.Content.ReadAsStringAsync();
            var updatedGameState = JsonSerializer.Deserialize<GameStateDto>(updatedGameStateJson, _jsonOptions);
            
            Assert.NotNull(updatedGameState);
            Assert.NotNull(updatedGameState.PlayedCards);
            
            // Verify that multiple cards were played (human + AI players)
            // With AutoAiPlay enabled, AI players should automatically play after human
            var playedCardsCount = updatedGameState.PlayedCards.Count;
            Assert.True(playedCardsCount > 1, 
                $"Expected more than 1 card to be played (human + AI auto-play), but got {playedCardsCount}");
            
            // Verify the human player's card was played
            var humanPlayedCard = updatedGameState.PlayedCards.FirstOrDefault(pc => pc.PlayerSeat == 0);
            Assert.NotNull(humanPlayedCard);
        }        [Fact]
        public async Task CompleteRound_ShouldHandleFullAIAutoPlayFlow_WhenEnabled()
        {
            // Arrange - Create factory with immediate AI delays for testing
            var testConfig = new Dictionary<string, string?>
            {
                {"GameSettings:AIPlayDelayMs", "0"},  // Immediate AI play for tests
                {"GameSettings:NewHandDelayMs", "0"}  // Immediate hand transitions for tests
            };
            
            using var factory = TestWebApplicationFactory.WithConfig(testConfig);
            using var client = factory.CreateClient();
            
            // Start a new game
            var gameState = await StartGameAsync(client);
            Assert.NotNull(gameState);

            // Act - Human player plays a card to trigger complete round
            var playCardRequest = new PlayCardRequestDto
            {
                GameId = gameState.GameId,
                PlayerSeat = 0, // Human player seat
                CardIndex = 0, // Play first card
                IsFold = false
            };

            var playCardJson = JsonSerializer.Serialize(playCardRequest, _jsonOptions);
            var playCardContent = new StringContent(playCardJson, Encoding.UTF8, "application/json");
            
            var playCardResponse = await client.PostAsync("/api/game/play-card", playCardContent);
            playCardResponse.EnsureSuccessStatusCode();            
            // Wait for complete event-driven round processing
            await Task.Delay(2000); // Allow time for all 3 AI players to auto-play

            // Assert - Verify complete round was played
            var gameStateResponse = await client.GetAsync($"/api/game/{gameState.GameId}");
            gameStateResponse.EnsureSuccessStatusCode();
            
            var gameStateJson = await gameStateResponse.Content.ReadAsStringAsync();
            var updatedGameState = JsonSerializer.Deserialize<GameStateDto>(gameStateJson, _jsonOptions);
            
            Assert.NotNull(updatedGameState);
            Assert.NotNull(updatedGameState.PlayedCards);            
            // DEBUG: Log actual state for diagnosis
            Console.WriteLine($"\n=== FINAL STATE ANALYSIS ===");
            Console.WriteLine($"PlayedCards count: {updatedGameState.PlayedCards.Count}");
            
            for (int i = 0; i < updatedGameState.PlayedCards.Count; i++)
            {
                var pc = updatedGameState.PlayedCards[i];
                var isFold = pc.Card?.Value == "FOLD" && pc.Card?.Suit == "FOLD";
                Console.WriteLine($"Seat {pc.PlayerSeat}: {pc.Card?.Value} of {pc.Card?.Suit} (IsFold: {isFold})");
            }
            
            Console.WriteLine($"\nPlayer states:");
            for (int i = 0; i < updatedGameState.Players.Count; i++)
            {
                var player = updatedGameState.Players[i];
                Console.WriteLine($"  Seat {player.Seat}: {player.Name} (IsActive: {player.IsActive}, Cards in hand: {player.Hand.Count})");
            }
            
            Console.WriteLine($"Current Hand: {updatedGameState.CurrentHand}");
            
            // Verify that all 4 players played their cards (1 human + 3 AI)
            // This validates the complete event-driven AI auto-play flow
            var playedCardsCount = updatedGameState.PlayedCards.Count;
            Assert.Equal(4, playedCardsCount);
            
            // Verify each player seat (0-3) has played a card
            for (int seat = 0; seat < 4; seat++)
            {
                var playerCard = updatedGameState.PlayedCards.FirstOrDefault(pc => pc.PlayerSeat == seat);
                Assert.NotNull(playerCard);
            }
        }

        /// <summary>
        /// Helper method to start a game and return the initial game state
        /// </summary>
        private async Task<StartGameResponse> StartGameAsync(HttpClient client)
        {
            var startGameRequest = new StartGameRequest { PlayerName = "TestHuman" };
            var startGameJson = JsonSerializer.Serialize(startGameRequest, _jsonOptions);
            var startGameContent = new StringContent(startGameJson, Encoding.UTF8, "application/json");

            var startResponse = await client.PostAsync("/api/game/start", startGameContent);
            startResponse.EnsureSuccessStatusCode();

            var startGameResponseJson = await startResponse.Content.ReadAsStringAsync();
            var startGameResponse = JsonSerializer.Deserialize<StartGameResponse>(startGameResponseJson, _jsonOptions);
            
            Assert.NotNull(startGameResponse);
            Assert.NotEmpty(startGameResponse.GameId);
            
            return startGameResponse;
        }
    }
}
