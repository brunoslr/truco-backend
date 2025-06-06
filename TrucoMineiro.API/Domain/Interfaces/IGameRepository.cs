using TrucoMineiro.API.Domain.Models;

namespace TrucoMineiro.API.Domain.Interfaces
{
    /// <summary>
    /// Interface for game state persistence and retrieval
    /// </summary>
    public interface IGameRepository
    {
        /// <summary>
        /// Saves or updates a game state
        /// </summary>
        /// <param name="game">The game state to save</param>
        /// <returns>True if successful, false otherwise</returns>
        Task<bool> SaveGameAsync(GameState game);

        /// <summary>
        /// Retrieves a game by its ID
        /// </summary>
        /// <param name="gameId">The unique identifier of the game</param>
        /// <returns>The game state if found, null otherwise</returns>
        Task<GameState?> GetGameAsync(string gameId);

        /// <summary>
        /// Removes a game from storage
        /// </summary>
        /// <param name="gameId">The unique identifier of the game</param>
        /// <returns>True if successfully removed, false otherwise</returns>
        Task<bool> RemoveGameAsync(string gameId);

        /// <summary>
        /// Gets all active games
        /// </summary>
        /// <returns>Collection of all active games</returns>
        Task<IEnumerable<GameState>> GetAllGamesAsync();

        /// <summary>
        /// Gets games that have been inactive for longer than the specified timeout
        /// </summary>
        /// <param name="timeoutMinutes">Timeout in minutes</param>
        /// <returns>Collection of expired games</returns>
        Task<IEnumerable<GameState>> GetExpiredGamesAsync(int timeoutMinutes);

        /// <summary>
        /// Updates the last activity timestamp for a game
        /// </summary>
        /// <param name="gameId">The unique identifier of the game</param>
        /// <returns>True if successful, false otherwise</returns>
        Task<bool> UpdateLastActivityAsync(string gameId);

        /// <summary>
        /// Deletes a game from storage
        /// </summary>
        /// <param name="gameId">The unique identifier of the game</param>
        /// <returns>True if successfully deleted, false otherwise</returns>
        Task<bool> DeleteGameAsync(string gameId);
    }
}
