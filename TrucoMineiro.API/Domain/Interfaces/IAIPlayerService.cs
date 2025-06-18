using TrucoMineiro.API.Domain.Models;

namespace TrucoMineiro.API.Domain.Interfaces
{
    /// <summary>
    /// Interface for AI player behavior and card selection
    /// </summary>
    public interface IAIPlayerService
    {
        /// <summary>
        /// Determines the best card for an AI player to play
        /// </summary>
        /// <param name="player">The AI player</param>
        /// <param name="game">The current game state</param>
        /// <returns>The index of the card to play (0-based)</returns>
        int SelectCardToPlay(Player player, GameState game);

        /// <summary>
        /// Determines if an AI player should call Truco
        /// </summary>
        /// <param name="player">The AI player</param>
        /// <param name="game">The current game state</param>
        /// <returns>True if AI should call Truco, false otherwise</returns>
        bool ShouldCallTruco(Player player, GameState game);

        /// <summary>
        /// Determines if an AI player should raise stakes
        /// </summary>
        /// <param name="player">The AI player</param>
        /// <param name="game">The current game state</param>
        /// <returns>True if AI should raise, false otherwise</returns>
        bool ShouldRaise(Player player, GameState game);        /// <summary>
        /// Determines if an AI player should fold
        /// </summary>
        /// <param name="player">The AI player</param>
        /// <param name="game">The current game state</param>
        /// <returns>True if AI should fold, false otherwise</returns>
        bool ShouldFold(Player player, GameState game);

        /// <summary>
        /// Determines how an AI player should respond to a truco call
        /// </summary>
        /// <param name="player">The AI player responding</param>
        /// <param name="game">The current game state</param>
        /// <param name="callType">The type of truco call ("Truco", "Seis", "Doze")</param>
        /// <param name="newPotentialStakes">The stakes if accepted</param>
        /// <returns>The AI's decision: Accept, Surrender, or Raise</returns>
        TrucoDecision DecideTrucoResponse(Player player, GameState game, string callType, int newPotentialStakes);

        /// <summary>
        /// Checks if a player is an AI player
        /// </summary>
        /// <param name="player">The player to check</param>
        /// <returns>True if AI player, false otherwise</returns>
        bool IsAIPlayer(Player player);
    }
}
