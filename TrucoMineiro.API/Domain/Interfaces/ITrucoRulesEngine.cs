using TrucoMineiro.API.Models;

namespace TrucoMineiro.API.Domain.Interfaces
{
    /// <summary>
    /// Interface for handling Truco calls, stakes, and special rules
    /// </summary>
    public interface ITrucoRulesEngine
    {
        /// <summary>
        /// Validates and processes a Truco call
        /// </summary>
        /// <param name="game">The current game state</param>
        /// <param name="playerId">The player making the Truco call</param>
        /// <returns>True if the call is valid and processed, false otherwise</returns>
        bool ProcessTrucoCall(GameState game, string playerId);

        /// <summary>
        /// Validates and processes a raise (after Truco)
        /// </summary>
        /// <param name="game">The current game state</param>
        /// <param name="playerId">The player raising the stakes</param>
        /// <returns>True if the raise is valid and processed, false otherwise</returns>
        bool ProcessRaise(GameState game, string playerId);

        /// <summary>
        /// Validates and processes a fold
        /// </summary>
        /// <param name="game">The current game state</param>
        /// <param name="playerId">The player folding</param>
        /// <returns>True if the fold is valid and processed, false otherwise</returns>
        bool ProcessFold(GameState game, string playerId);

        /// <summary>
        /// Checks if a Truco call is allowed in the current game state
        /// </summary>
        /// <param name="game">The current game state</param>
        /// <param name="playerId">The player wanting to call Truco</param>
        /// <returns>True if allowed, false otherwise</returns>
        bool CanCallTruco(GameState game, string playerId);

        /// <summary>
        /// Checks if raising stakes is allowed
        /// </summary>
        /// <param name="game">The current game state</param>
        /// <param name="playerId">The player wanting to raise</param>
        /// <returns>True if allowed, false otherwise</returns>
        bool CanRaise(GameState game, string playerId);

        /// <summary>
        /// Checks if "Mão de 10" special rule applies
        /// </summary>
        /// <param name="game">The current game state</param>
        /// <returns>True if "Mão de 10" rule is active, false otherwise</returns>
        bool IsMaoDe10Active(GameState game);

        /// <summary>
        /// Applies "Mão de 10" rule effects to the game
        /// </summary>
        /// <param name="game">The current game state</param>
        void ApplyMaoDe10Rule(GameState game);

        /// <summary>
        /// Calculates the points at stake for the current hand
        /// </summary>
        /// <param name="game">The current game state</param>
        /// <returns>Points at stake</returns>
        int CalculateStakes(GameState game);

        /// <summary>
        /// Calculates points awarded for winning a hand
        /// </summary>
        /// <param name="game">The current game state</param>
        /// <returns>Points to award to the winning team</returns>
        int CalculateHandPoints(GameState game);
    }
}
