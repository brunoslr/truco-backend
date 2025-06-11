using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using TrucoMineiro.API.Domain.Interfaces;
using TrucoMineiro.API.Domain.Models;

namespace TrucoMineiro.API.Domain.Services
{
    /// <summary>
    /// Service responsible for managing game state lifecycle and timeouts
    /// Handles creation, persistence, cleanup, and basic game state operations
    /// </summary>
    public class GameStateManager : IGameStateManager
    {
        private readonly IGameRepository _gameRepository;
        private readonly IConfiguration _configuration;
        private readonly ILogger<GameStateManager> _logger;

        public GameStateManager(
            IGameRepository gameRepository,
            IConfiguration configuration,
            ILogger<GameStateManager> logger)
        {
            _gameRepository = gameRepository;
            _configuration = configuration;
            _logger = logger;
        }

        /// <summary>
        /// Creates a new game
        /// </summary>
        public async Task<GameState> CreateGameAsync(string? playerName = null)
        {
            _logger.LogInformation("Creating new game for player: {PlayerName}", playerName ?? "Anonymous");
            
            var gameState = new GameState();
            gameState.InitializeGame(playerName ?? "Anonymous");
            
            await _gameRepository.SaveGameAsync(gameState);
            
            _logger.LogInformation("Game created with ID: {GameId}", gameState.GameId);
            return gameState;
        }

        /// <summary>
        /// Retrieves a game by ID and updates its last activity
        /// </summary>
        public async Task<GameState?> GetActiveGameAsync(string gameId)
        {
            var game = await _gameRepository.GetGameAsync(gameId);
            
            if (game == null)
            {
                _logger.LogWarning("Game {GameId} not found", gameId);
                return null;
            }

            if (IsGameExpired(game))
            {
                _logger.LogInformation("Game {GameId} has expired", gameId);
                return null;
            }

            // Update last activity
            game.LastActivity = DateTime.UtcNow;
            await _gameRepository.SaveGameAsync(game);
            
            return game;
        }

        /// <summary>
        /// Saves game state changes
        /// </summary>
        public async Task<bool> SaveGameAsync(GameState game)
        {
            try
            {
                game.LastActivity = DateTime.UtcNow;
                return await _gameRepository.SaveGameAsync(game);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to save game {GameId}", game.GameId);
                return false;
            }
        }

        /// <summary>
        /// Removes a completed or expired game
        /// </summary>
        public async Task<bool> RemoveGameAsync(string gameId)
        {
            try
            {
                return await _gameRepository.DeleteGameAsync(gameId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to remove game {GameId}", gameId);
                return false;
            }
        }

        /// <summary>
        /// Cleanup expired games based on timeout
        /// </summary>
        public async Task<int> CleanupExpiredGamesAsync()
        {
            var expiredGameIds = await GetExpiredGameIdsAsync();
            int cleanedUp = 0;

            foreach (var gameId in expiredGameIds)
            {
                if (await RemoveGameAsync(gameId))
                {
                    cleanedUp++;
                }
            }

            if (cleanedUp > 0)
            {
                _logger.LogInformation("Cleaned up {Count} expired games", cleanedUp);
            }

            return cleanedUp;
        }

        /// <summary>
        /// Cleanup completed games
        /// </summary>
        public async Task<int> CleanupCompletedGamesAsync()
        {
            var allGames = await _gameRepository.GetAllGamesAsync();
            var completedGames = allGames.Where(g => IsGameCompleted(g)).ToList();
            int cleanedUp = 0;

            foreach (var game in completedGames)
            {
                if (await RemoveGameAsync(game.GameId))
                {
                    cleanedUp++;
                }
            }

            if (cleanedUp > 0)
            {
                _logger.LogInformation("Cleaned up {Count} completed games", cleanedUp);
            }

            return cleanedUp;
        }

        /// <summary>
        /// Checks if a game is expired based on last activity
        /// </summary>
        public bool IsGameExpired(GameState game)
        {
            var timeoutMinutes = _configuration.GetValue<int>("GameSettings:InactivityTimeoutMinutes", 30);
            var timeoutThreshold = DateTime.UtcNow.AddMinutes(-timeoutMinutes);
            return game.LastActivity < timeoutThreshold;
        }

        /// <summary>
        /// Checks if a game is completed (winner reached 12 points)
        /// </summary>
        public bool IsGameCompleted(GameState game)
        {
            return game.Status == GameStatus.Completed || 
                   game.Team1Score >= 12 || 
                   game.Team2Score >= 12;
        }

        /// <summary>
        /// Gets the IDs of expired games
        /// </summary>
        public async Task<List<string>> GetExpiredGameIdsAsync()
        {
            var allGames = await _gameRepository.GetAllGamesAsync();
            return allGames.Where(g => IsGameExpired(g)).Select(g => g.GameId).ToList();
        }

        /// <summary>
        /// Advances the turn to the next player
        /// </summary>
        public void AdvanceToNextPlayer(GameState game)
        {
            var currentPlayer = game.Players.FirstOrDefault(p => p.IsActive);
            if (currentPlayer != null)
            {
                currentPlayer.IsActive = false;
                var nextPlayerIndex = (currentPlayer.Seat + 1) % game.Players.Count;
                game.Players[nextPlayerIndex].IsActive = true;
                game.CurrentPlayerIndex = nextPlayerIndex;
            }
        }

        /// <summary>
        /// Checks if all players have played their cards for the current round
        /// </summary>
        public bool IsRoundComplete(GameState game)
        {
            return game.PlayedCards.Count == 4 && 
                   game.PlayedCards.All(pc => pc.Card != null && !pc.Card.IsEmpty);
        }

        /// <summary>
        /// Starts a new hand by resetting the game state
        /// </summary>
        public void StartNewHand(GameState game)
        {
            _logger.LogInformation("Starting new hand for game {GameId} - Hand {HandNumber}", 
                game.GameId, game.CurrentHand + 1);

            // Clear played cards
            foreach (var pc in game.PlayedCards)
            {
                pc.Card = Card.CreateEmptyCard();
            }

            // Reset round
            game.CurrentRound = 1;
            game.CurrentHand++;

            // Deal new cards to all players
            game.DealCards();

            // Reset stakes
            game.Stakes = 1;

            _logger.LogInformation("New hand started for game {GameId} - Hand {HandNumber}", 
                game.GameId, game.CurrentHand);
        }

        /// <summary>
        /// Checks if the development mode is enabled
        /// </summary>
        public bool IsDevMode()
        {
            return _configuration.GetValue<bool>("FeatureFlags:DevMode", false);
        }
    }
}