using TrucoMineiro.API.Domain.Models;

namespace TrucoMineiro.API.Domain.Interfaces
{
    /// <summary>
    /// Service responsible for managing game state cleanup operations
    /// </summary>
    public interface IGameCleanupService
    {
        /// <summary>
        /// Clean up after a round is completed
        /// </summary>
        /// <param name="game">The current game state</param>
        void CleanupAfterRound(GameState game);

        /// <summary>
        /// Clean up after a hand is completed
        /// </summary>
        /// <param name="game">The current game state</param>
        Task CleanupAfterHandAsync(GameState game);

        /// <summary>
        /// Clean up when game is completed
        /// </summary>
        /// <param name="game">The current game state</param>
        void CleanupAfterGame(GameState game);

        /// <summary>
        /// Initialize played card slots for a new round
        /// </summary>
        /// <param name="game">The current game state</param>
        void InitializePlayedCardsForRound(GameState game);

        /// <summary>
        /// Initialize played card slots for a new hand
        /// </summary>
        /// <param name="game">The current game state</param>
        void InitializePlayedCardsForHand(GameState game);
    }
}
