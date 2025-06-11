using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using TrucoMineiro.API.DTOs;
using TrucoMineiro.Tests.Integration;

namespace TrucoMineiro.Tests.TestUtilities
{
    /// <summary>
    /// Base class for endpoint tests providing shared setup and utilities
    /// Uses real event-driven architecture instead of mocks for authentic testing
    /// </summary>
    public abstract class EndpointTestBase : IDisposable
    {
        protected readonly TestWebApplicationFactory _factory;
        protected readonly HttpClient _client;
        protected readonly JsonSerializerOptions _jsonOptions;        protected EndpointTestBase()
        {
            // Use fast test configuration by default to optimize test execution
            var fastConfig = GetFastTestConfig();
            _factory = TestWebApplicationFactory.WithConfig(fastConfig);
            _client = _factory.CreateClient();
            _jsonOptions = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
        }

        protected EndpointTestBase(Dictionary<string, string?> configOverrides)
        {
            _factory = TestWebApplicationFactory.WithConfig(configOverrides);
            _client = _factory.CreateClient();
            _jsonOptions = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
        }        /// <summary>
        /// Creates a new game and returns the game state
        /// </summary>
        public async Task<StartGameResponse> CreateGameAsync(string playerName = "TestPlayer")
        {
            var startGameRequest = new StartGameRequest { PlayerName = playerName };
            var json = JsonSerializer.Serialize(startGameRequest, _jsonOptions);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _client.PostAsync("/api/game/start", content);
            response.EnsureSuccessStatusCode();

            var responseJson = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<StartGameResponse>(responseJson, _jsonOptions)!;
        }        /// <summary>
        /// Plays a card using the PlayCard endpoint
        /// </summary>
        public async Task<PlayCardResponseDto> PlayCardAsync(PlayCardRequestDto request)
        {
            var json = JsonSerializer.Serialize(request, _jsonOptions);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _client.PostAsync("/api/game/play-card", content);
            var responseJson = await response.Content.ReadAsStringAsync();
            
            return JsonSerializer.Deserialize<PlayCardResponseDto>(responseJson, _jsonOptions)!;
        }        /// <summary>
        /// Gets the current game state
        /// </summary>
        public async Task<GameStateDto> GetGameStateAsync(string gameId, int? playerSeat = null)
        {
            var url = $"/api/game/{gameId}";
            if (playerSeat.HasValue)
            {
                url += $"?playerSeat={playerSeat}";
            }

            var response = await _client.GetAsync(url);
            response.EnsureSuccessStatusCode();

            var responseJson = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<GameStateDto>(responseJson, _jsonOptions)!;
        }

        /// <summary>
        /// Creates a PlayCardRequestDto with common defaults
        /// </summary>
        protected PlayCardRequestDto CreatePlayCardRequest(string gameId, int playerSeat = 0, int cardIndex = 0, bool isFold = false)
        {
            return new PlayCardRequestDto
            {
                GameId = gameId,
                PlayerSeat = playerSeat,
                CardIndex = cardIndex,
                IsFold = isFold
            };
        }

        /// <summary>
        /// Finds the human player (seat 0) from a game response
        /// </summary>
        protected PlayerInfoDto GetHumanPlayer(StartGameResponse gameResponse)
        {
            return gameResponse.Players.First(p => p.Seat == 0);
        }

        /// <summary>
        /// Finds the player at a specific seat from a game response
        /// </summary>
        protected PlayerInfoDto GetPlayerAtSeat(StartGameResponse gameResponse, int seat)
        {
            return gameResponse.Players.First(p => p.Seat == seat);
        }

        /// <summary>
        /// Runs a test with specific configuration overrides
        /// Note: This method creates a temporary test instance - use constructor with configOverrides instead for better performance
        /// </summary>
        /// <param name="configOverrides">Configuration settings to override</param>
        /// <param name="testAction">Test action to execute with the configured test base</param>
        protected async Task TestWithConfigAsync(
            Dictionary<string, string?> configOverrides,
            Func<Task> testAction)
        {
            // Create temporary factory and client with the specified configuration
            using var factory = TestWebApplicationFactory.WithConfig(configOverrides);
            using var client = factory.CreateClient();
            var jsonOptions = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };

            // Temporarily replace the current factory and client
            var originalFactory = _factory;
            var originalClient = _client;

            // Use reflection to set the fields temporarily (this is a testing utility)
            var factoryField = typeof(EndpointTestBase).GetField("_factory", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var clientField = typeof(EndpointTestBase).GetField("_client", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

            try
            {
                factoryField?.SetValue(this, factory);
                clientField?.SetValue(this, client);

                await testAction();
            }
            finally
            {
                // Restore original values
                factoryField?.SetValue(this, originalFactory);
                clientField?.SetValue(this, originalClient);
            }
        }

        /// <summary>
        /// Gets default fast test configuration with optimized delays for testing
        /// Most tests should use this configuration unless specifically testing delay behavior
        /// </summary>
        protected static Dictionary<string, string?> GetFastTestConfig()
        {
            return new Dictionary<string, string?>
            {
                {"GameSettings:AIPlayDelayMs", "0"},    // Immediate AI play for tests
                {"GameSettings:NewHandDelayMs", "0"},   // Immediate hand transitions for tests
                {"FeatureFlags:AutoAiPlay", "true"},    // Enable AI auto-play
                {"FeatureFlags:DevMode", "false"}       // Default to normal mode
            };
        }        /// <summary>
        /// Gets default fast test configuration with DevMode enabled
        /// Useful for tests that need to see all cards
        /// </summary>
        protected static Dictionary<string, string?> GetFastTestConfigWithDevMode()
        {
            var config = GetFastTestConfig();
            config["FeatureFlags:DevMode"] = "true";
            return config;
        }

        /// <summary>
        /// Gets test configuration with AI auto-play disabled
        /// Useful for tests that need to verify intermediate game states before AI takes over
        /// </summary>
        protected static Dictionary<string, string?> GetConfigWithoutAutoAiPlay()
        {
            return new Dictionary<string, string?>
            {
                {"GameSettings:AIPlayDelayMs", "0"},    // Immediate AI play when triggered
                {"GameSettings:NewHandDelayMs", "0"},   // Immediate hand transitions for tests
                {"FeatureFlags:AutoAiPlay", "false"},   // Disable AI auto-play
                {"FeatureFlags:DevMode", "false"}       // Default to normal mode
            };
        }

        public virtual void Dispose()
        {
            _client?.Dispose();
            _factory?.Dispose();
        }
    }

    /// <summary>
    /// Internal test endpoint base for configuration testing
    /// </summary>
    internal class TestEndpointBase : EndpointTestBase
    {
        public TestEndpointBase(Dictionary<string, string?> configOverrides) : base(configOverrides)
        {
        }
    }
}
