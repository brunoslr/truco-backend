using TrucoMineiro.API.Models;

namespace TrucoMineiro.API.Domain.Interfaces
{
    /// <summary>
    /// Interface for managing game state lifecycle and timeouts
    /// </summary>
    public interface IGameStateManager
    {
        /// <summary>
        /// Creates a new game
        /// </summary>
        /// <param name="playerName">Optional player name for the human player</param>
        /// <returns>The created game state</returns>
        Task<GameState> CreateGameAsync(string? playerName = null);

        /// <summary>
        /// Retrieves a game by ID and updates its last activity
        /// </summary>
        /// <param name="gameId">The unique identifier of the game</param>
        /// <returns>The game state if found and active, null otherwise</returns>
        Task<GameState?> GetActiveGameAsync(string gameId);

        /// <summary>
        /// Saves game state changes
        /// </summary>
        /// <param name="game">The game state to save</param>
        /// <returns>True if successful, false otherwise</returns>
        Task<bool> SaveGameAsync(GameState game);

        /// <summary>
        /// Removes a completed or expired game
        /// </summary>
        /// <param name="gameId">The unique identifier of the game</param>
        /// <returns>True if successfully removed, false otherwise</returns>
        Task<bool> RemoveGameAsync(string gameId);

        /// <summary>
        /// Cleanup expired games based on timeout
        /// </summary>
        /// <returns>Number of games cleaned up</returns>
        Task<int> CleanupExpiredGamesAsync();

        /// <summary>
        /// Cleanup completed games
        /// </summary>
        /// <returns>Number of games cleaned up</returns>
        Task<int> CleanupCompletedGamesAsync();

        /// <summary>
        /// Checks if a game is expired based on last activity
        /// </summary>
        /// <param name="game">The game to check</param>
        /// <returns>True if expired, false otherwise</returns>
        bool IsGameExpired(GameState game);

        /// <summary>
        /// Checks if a game is completed (winner reached 12 points)
        /// </summary>
        /// <param name="game">The game to check</param>
        /// <returns>True if completed, false otherwise</returns>
        bool IsGameCompleted(GameState game);

        /// <summary>
        /// Gets the IDs of expired games
        /// </summary>
        /// <returns>List of expired game IDs</returns>
        Task<List<string>> GetExpiredGameIdsAsync();
    }
}
