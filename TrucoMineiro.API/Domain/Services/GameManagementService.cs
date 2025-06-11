using TrucoMineiro.API.Domain.Events;
using TrucoMineiro.API.Domain.Interfaces;
using TrucoMineiro.API.Domain.Models;
using TrucoMineiro.API.DTOs;

namespace TrucoMineiro.API.Services
{
    /// <summary>
    /// Service for managing general game operations (game creation, retrieval, and configuration)
    /// </summary>
    public class GameManagementService
    {        
        private readonly IGameStateManager _gameStateManager;
        private readonly IGameRepository _gameRepository;
        private readonly bool _devMode;

        /// <summary>
        /// Constructor for GameManagementService
        /// </summary>
        /// <param name="gameStateManager">Game state management service</param>        
        /// <param name="gameRepository">Game repository</param>
        /// <param name="configuration">Application configuration</param>
        public GameManagementService(
            IGameStateManager gameStateManager,
            IGameRepository gameRepository,
            IConfiguration configuration)
        {
            _gameStateManager = gameStateManager;           
            _gameRepository = gameRepository;           
            // Read configuration from appsettings.json
            _devMode = configuration.GetValue<bool>("FeatureFlags:DevMode", false);
        }

        /// <summary>
        /// Creates a new game with a custom player name
        /// </summary>
        /// <param name="playerName">Name for the player at seat 0</param>
        /// <returns>The newly created game state</returns>
        public GameState CreateGame(string? playerName = null)
        {
            var gameTask = _gameStateManager.CreateGameAsync(playerName);
            var gameState = gameTask.GetAwaiter().GetResult();

            // Update stakes to 2 as per legacy requirements
            gameState.CurrentStake = 2;

            // Save the updated game state
            var saveTask = _gameRepository.SaveGameAsync(gameState);
            saveTask.GetAwaiter().GetResult();

            return gameState;
        }

        /// <summary>
        /// Checks if the development mode is enabled
        /// </summary>
        /// <returns>True if DevMode is enabled, false otherwise</returns>
        public bool IsDevMode()
        {
            return _devMode;
        }       
        
        /// <summary>
        /// Gets a game by ID
        /// </summary>
        /// <param name="gameId">The unique identifier of the game</param>
        /// <returns>The game state if found, null otherwise</returns>
        public GameState? GetGame(string gameId)
        {
            var gameTask = _gameRepository.GetGameAsync(gameId);
            return gameTask.GetAwaiter().GetResult();
        }
    }
}
