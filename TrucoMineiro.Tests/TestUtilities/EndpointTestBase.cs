using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using TrucoMineiro.API.DTOs;
using TrucoMineiro.Tests.Integration;

namespace TrucoMineiro.Tests.TestUtilities
{    /// <summary>
    /// Base class for integration tests requiring HTTP operations against the TrucoMineiro API.
    /// 
    /// CONFIGURATION STRATEGY:
    /// - All test configuration is code-based for better visibility and maintainability
    /// - No external config files (appsettings.Test.json) to avoid duplication and drift
    /// - Default configuration optimized for fast test execution (zero delays)
    /// - Override configuration through constructor for specific test scenarios
    /// 
    /// USAGE PATTERNS:
    /// 1. Default fast testing: inherit from EndpointTestBase()
    /// 2. Custom configuration: inherit from EndpointTestBase(configOverrides)
    /// 3. DevMode testing: use GetFastTestConfigWithDevMode()
    /// 4. No AutoAI testing: use GetConfigWithoutAutoAiPlay()
    /// 5. Timing testing: use GetRealisticTimingConfig()
    /// 
    /// BEST PRACTICES:
    /// - Use this base class for all tests requiring HTTP API operations
    /// - Override configuration only when testing specific timing/behavior scenarios
    /// - Keep test-specific configuration close to the test code for clarity
    /// - Use predefined config methods for common scenarios to avoid duplication
    /// - Always dispose properly - base class handles factory and client disposal
    /// 
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
        }        /// <summary>
        /// Presses a button (truco actions) using the ButtonPress endpoint
        /// </summary>
        public async Task<GameStateDto> PressButtonAsync(ButtonPressRequest request)
        {
            var json = JsonSerializer.Serialize(request, _jsonOptions);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _client.PostAsync("/api/game/press-button", content);
            
            if (!response.IsSuccessStatusCode)
            {
                // Read the error message from the response body and include it in the exception
                var errorContent = await response.Content.ReadAsStringAsync();
                throw new HttpRequestException($"Response status code does not indicate success: {(int)response.StatusCode} ({response.ReasonPhrase}). {errorContent}");
            }

            var responseJson = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<GameStateDto>(responseJson, _jsonOptions)!;
        }

        /// <summary>
        /// Presses a button (truco actions) using the ButtonPress endpoint with error handling
        /// Returns the error message if the request fails, null if successful
        /// </summary>
        public async Task<string?> PressButtonWithErrorAsync(ButtonPressRequest request)
        {
            var json = JsonSerializer.Serialize(request, _jsonOptions);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _client.PostAsync("/api/game/press-button", content);
            
            if (response.IsSuccessStatusCode)
            {
                return null; // Success, no error
            }
            
            // Read the error message from the response body
            var errorContent = await response.Content.ReadAsStringAsync();
            return errorContent;
        }        /// <summary>
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
        /// Creates a ButtonPressRequest with common defaults
        /// </summary>
        protected ButtonPressRequest CreateButtonPressRequest(string gameId, int playerSeat, string action)
        {
            return new ButtonPressRequest
            {
                GameId = gameId,
                PlayerSeat = playerSeat,
                Action = action
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
        }        /// <summary>
        /// Standard fast test configuration - zero delays, AutoAI enabled, DevMode disabled
        /// Use for: Most integration tests requiring speed
        /// </summary>
        protected static Dictionary<string, string?> GetFastTestConfig()
        {
            return new Dictionary<string, string?>
            {
                {"GameSettings:AIMinPlayDelayMs", "0"},    // Immediate AI play for tests
                {"GameSettings:AIMaxPlayDelayMs", "0"},    // Immediate AI play for tests
                {"GameSettings:HandResolutionDelayMs", "0"},   // Immediate hand resolution transitions for tests
                {"GameSettings:RoundResolutionDelayMs", "0"},   // Immediate round resolution transitions for tests
                {"GameSettings:InitialDealerSeat", "3"}, // Consistent dealer for predictable tests
                {"FeatureFlags:AutoAiPlay", "true"},    // Enable AI auto-play
                {"FeatureFlags:DevMode", "false"}       // Default to normal mode
            };
        }

        /// <summary>
        /// DevMode configuration - zero delays, AutoAI enabled, DevMode enabled  
        /// Use for: Tests needing to inspect all player cards
        /// </summary>
        protected static Dictionary<string, string?> GetFastTestConfigWithDevMode()
        {
            var config = GetFastTestConfig();
            config["FeatureFlags:DevMode"] = "true";
            return config;
        }

        /// <summary>
        /// Manual control configuration - zero delays, AutoAI disabled
        /// Use for: Tests needing to control AI timing manually
        /// </summary>
        protected static Dictionary<string, string?> GetConfigWithoutAutoAiPlay()
        {
            var config = GetFastTestConfig();
            config["FeatureFlags:AutoAiPlay"] = "false"; 
            return config;
        }

        /// <summary>
        /// Realistic timing configuration - production-like delays for timing tests
        /// Use for: AI timing behavior validation tests
        /// </summary>
        protected static Dictionary<string, string?> GetRealisticTimingConfig()
        {
            return new Dictionary<string, string?>
            {
                {"GameSettings:AIMinPlayDelayMs", "100"},   // Realistic minimum AI delay
                {"GameSettings:AIMaxPlayDelayMs", "300"},   // Realistic maximum AI delay
                {"GameSettings:HandResolutionDelayMs", "100"},   // Realistic hand resolution transitions for tests
                {"GameSettings:RoundResolutionDelayMs", "100"},   // Realistic round resolution transitions for tests
                {"GameSettings:InitialDealerSeat", "3"},  // Consistent dealer for predictable tests
                {"FeatureFlags:AutoAiPlay", "true"},     // Enable AI auto-play
                {"FeatureFlags:DevMode", "false"}        // Default to normal mode
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
