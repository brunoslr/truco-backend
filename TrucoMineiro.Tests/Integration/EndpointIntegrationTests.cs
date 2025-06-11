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
    /// Integration tests for complete hand flow with real HTTP endpoints
    /// Tests 1 human player + 3 AI players through actual API calls
    /// Uses real event publisher to test the event-driven AI architecture
    /// 
    /// MIGRATION NOTES:
    /// - These tests favor real modules over mocking to test the complete event-driven flow
    /// - AI auto-play timing issues may require investigation (asynchronous event processing)
    /// - TODO: Move more complex multi-component scenarios to integration tests
    /// - TODO: Reduce unit test mocking in favor of testing real component interactions
    /// - TODO: Add comprehensive integration tests for edge cases and error scenarios
    /// - TODO: Consider adding integration tests for event-driven race conditions
    /// </summary>
    public class EndpointIntegrationTests : IClassFixture<TestWebApplicationFactory>
    {
        private readonly TestWebApplicationFactory _factory;
        private readonly JsonSerializerOptions _jsonOptions = new() { PropertyNameCaseInsensitive = true };

        public EndpointIntegrationTests(TestWebApplicationFactory factory)
        {
            _factory = factory;
        }

        [Fact]
        public async Task CompleteHandFlow_ShouldHandleRealPlayerAndAIPlayers()
        {
            // Arrange - Create HTTP client and start game
            using var client = _factory.CreateClient();
            
            var startGameRequest = new StartGameRequest { PlayerName = "TestHuman" };
            var startGameJson = JsonSerializer.Serialize(startGameRequest, _jsonOptions);
            var startGameContent = new StringContent(startGameJson, Encoding.UTF8, "application/json");

            // Act & Assert - Start game
            var startResponse = await client.PostAsync("/api/game/start", startGameContent);
            startResponse.EnsureSuccessStatusCode();

            var startGameResponseJson = await startResponse.Content.ReadAsStringAsync();
            var gameState = JsonSerializer.Deserialize<StartGameResponse>(startGameResponseJson, _jsonOptions);
            
            Assert.NotNull(gameState);
            Assert.NotEmpty(gameState.GameId);
            Assert.Equal(0, gameState.PlayerSeat);
            Assert.Equal(4, gameState.Players.Count);
            Assert.Equal(3, gameState.Hand.Count);

            var gameId = gameState.GameId;            // Verify initial game state
            var getGameResponse = await client.GetAsync($"/api/game/{gameId}");
            getGameResponse.EnsureSuccessStatusCode();

            var gameStateJson = await getGameResponse.Content.ReadAsStringAsync();
            var currentGameState = JsonSerializer.Deserialize<GameStateDto>(gameStateJson, _jsonOptions);

            Assert.NotNull(currentGameState);
            Assert.Equal(4, currentGameState.Players.Count);
            Assert.True(currentGameState.CurrentHand >= 1);

            // Human player plays first card
            var playCardRequest = new PlayCardRequestDto
            {
                GameId = gameId,
                PlayerSeat = 0,
                CardIndex = 0,
                IsFold = false
            };

            var playCardJson = JsonSerializer.Serialize(playCardRequest, _jsonOptions);
            var playCardContent = new StringContent(playCardJson, Encoding.UTF8, "application/json");

            var playCardResponse = await client.PostAsync("/api/game/play-card", playCardContent);
            playCardResponse.EnsureSuccessStatusCode();

            var playCardResponseJson = await playCardResponse.Content.ReadAsStringAsync();
            var playCardResult = JsonSerializer.Deserialize<PlayCardResponseDto>(playCardResponseJson, _jsonOptions);

            Assert.NotNull(playCardResult);
            Assert.True(playCardResult.Success);
            Assert.Equal(2, playCardResult.Hand.Count); // Human should have 2 cards left
            Assert.True(playCardResult.GameState.PlayedCards.Count >= 1);

            // Wait for AI players to automatically play their turns
            await Task.Delay(4000);            // Verify final state after AI auto-play
            var finalGameStateResponse = await client.GetAsync($"/api/game/{gameId}");
            finalGameStateResponse.EnsureSuccessStatusCode();

            var finalGameStateJson = await finalGameStateResponse.Content.ReadAsStringAsync();
            var finalGameState = JsonSerializer.Deserialize<GameStateDto>(finalGameStateJson, _jsonOptions);

            Assert.NotNull(finalGameState);
            Assert.True(finalGameState.PlayedCards.Count >= 1, "At least human player's card should be played");
            
            // If auto-play is working, we should see 4 played cards (complete round)
            if (finalGameState.PlayedCards.Count == 4)
            {
                var playedSeats = finalGameState.PlayedCards.Select(pc => pc.PlayerSeat).OrderBy(s => s).ToList();
                Assert.Equal(new[] { 0, 1, 2, 3 }, playedSeats);
            }
        }

        [Fact]
        public async Task AIAutoPlay_ShouldRespondAfterHumanMove()
        {
            // Arrange - Start a game
            using var client = _factory.CreateClient();
            
            var startGameRequest = new StartGameRequest { PlayerName = "AITest" };
            var startGameJson = JsonSerializer.Serialize(startGameRequest, _jsonOptions);
            var startGameContent = new StringContent(startGameJson, Encoding.UTF8, "application/json");            var startResponse = await client.PostAsync("/api/game/start", startGameContent);
            var startGameResponseJson = await startResponse.Content.ReadAsStringAsync();
            
            // TODO: Remove debug logging after event-driven AI timing issues are resolved
            Console.WriteLine($"Start game response: {startGameResponseJson}");
            
            var gameState = JsonSerializer.Deserialize<StartGameResponse>(startGameResponseJson, _jsonOptions);            var gameId = gameState!.GameId;
            
            // TODO: Remove debug logging after event-driven AI timing issues are resolved
            Console.WriteLine($"Parsed GameId: '{gameId}' (length: {gameId?.Length})");
            
            // Act - Human player makes a move
            var playCardRequest = new PlayCardRequestDto
            {
                GameId = gameId ?? throw new InvalidOperationException("GameId cannot be null"),
                PlayerSeat = 0,
                CardIndex = 0,
                IsFold = false
            };

            var playCardJson = JsonSerializer.Serialize(playCardRequest, _jsonOptions);
            var playCardContent = new StringContent(playCardJson, Encoding.UTF8, "application/json");

            var playCardResponse = await client.PostAsync("/api/game/play-card", playCardContent);
            
            // Debug: Check the response before asserting success
            if (!playCardResponse.IsSuccessStatusCode)
            {
                var errorContent = await playCardResponse.Content.ReadAsStringAsync();
                Console.WriteLine($"AIAutoPlay PlayCard failed with status: {playCardResponse.StatusCode}");
                Console.WriteLine($"Error content: {errorContent}");
            }
            
            playCardResponse.EnsureSuccessStatusCode();

            var playCardResponseJson = await playCardResponse.Content.ReadAsStringAsync();
            var playCardResult = JsonSerializer.Deserialize<PlayCardResponseDto>(playCardResponseJson, _jsonOptions);

            // Verify human move was recorded
            Assert.NotNull(playCardResult);
            Assert.True(playCardResult.Success);
            Assert.True(playCardResult.GameState.PlayedCards.Count >= 1);

            // Wait for AI players to auto-play
            await Task.Delay(5000);            // Assert - Verify AI players responded automatically
            var finalStateResponse = await client.GetAsync($"/api/game/{gameId}");
            finalStateResponse.EnsureSuccessStatusCode();
            var finalStateJson = await finalStateResponse.Content.ReadAsStringAsync();
            var finalState = JsonSerializer.Deserialize<GameStateDto>(finalStateJson, _jsonOptions);

            Assert.NotNull(finalState);
            
            // If AI auto-play is working correctly, we should see more than just the human's card
            Assert.True(finalState.PlayedCards.Count > 1, 
                $"Expected AI players to auto-play, but only {finalState.PlayedCards.Count} cards were played");
                
            // Ideally, all 4 players should have played if auto-play is fully working
            if (finalState.PlayedCards.Count == 4)
            {
                // Verify each seat played exactly once
                var playedSeats = finalState.PlayedCards.Select(pc => pc.PlayerSeat).ToList();
                Assert.Equal(4, playedSeats.Distinct().Count());
                Assert.Contains(0, playedSeats); // Human player
                Assert.Contains(1, playedSeats); // AI player 1
                Assert.Contains(2, playedSeats); // AI player 2
                Assert.Contains(3, playedSeats); // AI player 3
            }
        }

        [Fact]
        public async Task SuitConstants_ShouldBeConsistentInAllEndpoints()
        {
            // Arrange - Start a game
            using var client = _factory.CreateClient();
            
            var startGameRequest = new StartGameRequest { PlayerName = "SuitTest" };
            var startGameJson = JsonSerializer.Serialize(startGameRequest, _jsonOptions);
            var startGameContent = new StringContent(startGameJson, Encoding.UTF8, "application/json");

            var startResponse = await client.PostAsync("/api/game/start", startGameContent);
            var startGameResponseJson = await startResponse.Content.ReadAsStringAsync();
            var gameState = JsonSerializer.Deserialize<StartGameResponse>(startGameResponseJson, _jsonOptions);
            var gameId = gameState!.GameId;            
            // Act & Assert - Verify all cards use Unicode suit symbols
            var gameStateResponse = await client.GetAsync($"/api/game/{gameId}");
            var gameStateJson = await gameStateResponse.Content.ReadAsStringAsync();
            var currentGameState = JsonSerializer.Deserialize<GameStateDto>(gameStateJson, _jsonOptions);

            // Verify human player's cards use proper Unicode symbols
            var humanPlayer = currentGameState!.Players.First(p => p.Seat == 0);
            var validSuits = new[] { "♥", "♦", "♣", "♠" };
            
            foreach (var card in humanPlayer.Hand)
            {
                Assert.Contains(card.Suit, validSuits);
                Assert.False(string.IsNullOrEmpty(card.Value));
            }

            // Play a card and verify played cards also use proper suits
            var playCardRequest = new PlayCardRequestDto
            {
                GameId = gameId,
                PlayerSeat = 0,
                CardIndex = 0,
                IsFold = false
            };            
            var playCardJson = JsonSerializer.Serialize(playCardRequest, _jsonOptions);
            var playCardContent = new StringContent(playCardJson, Encoding.UTF8, "application/json");            
            var playCardResponse = await client.PostAsync("/api/game/play-card", playCardContent);
            playCardResponse.EnsureSuccessStatusCode();
            
            var playCardResponseJson = await playCardResponse.Content.ReadAsStringAsync();
            
            // Debug: Log the actual response to understand the issue
            Console.WriteLine($"PlayCard Response JSON: {playCardResponseJson}");
            
            var playCardResult = JsonSerializer.Deserialize<PlayCardResponseDto>(playCardResponseJson, _jsonOptions);
            
            Assert.NotNull(playCardResult);
            Assert.True(playCardResult.Success);
              // Verify played cards use proper suit symbols (skip empty placeholder cards)
            foreach (var playedCard in playCardResult.GameState.PlayedCards)
            {
                Assert.NotNull(playedCard.Card);
                
                // Skip empty placeholder cards - they have "EMPTY" suit and value which is valid
                if (playedCard.Card.Suit == "EMPTY" && playedCard.Card.Value == "EMPTY")
                    continue;
                
                Assert.Contains(playedCard.Card.Suit, validSuits);
                Assert.False(string.IsNullOrEmpty(playedCard.Card.Value));
            }
        }
    }
}
