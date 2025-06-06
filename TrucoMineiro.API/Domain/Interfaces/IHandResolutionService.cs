using TrucoMineiro.API.Domain.Models;

namespace TrucoMineiro.API.Domain.Interfaces
{
    /// <summary>
    /// Interface for handling card ranking and hand resolution logic
    /// </summary>
    public interface IHandResolutionService
    {
        /// <summary>
        /// Gets the strength/ranking of a card according to Truco Mineiro rules
        /// </summary>
        /// <param name="card">The card to evaluate</param>
        /// <returns>Numeric strength value (higher = stronger)</returns>
        int GetCardStrength(Card card);

        /// <summary>
        /// Determines the winner of a round based on played cards
        /// </summary>
        /// <param name="playedCards">Cards played in the round</param>
        /// <param name="players">All players in the game</param>
        /// <returns>The winning player, or null if there's a draw</returns>
        Player? DetermineRoundWinner(List<PlayedCard> playedCards, List<Player> players);

        /// <summary>
        /// Determines if there's a draw in the current round
        /// </summary>
        /// <param name="playedCards">Cards played in the round</param>
        /// <param name="players">All players in the game</param>
        /// <returns>True if there's a draw, false otherwise</returns>
        bool IsRoundDraw(List<PlayedCard> playedCards, List<Player> players);

        /// <summary>
        /// Handles draw resolution according to Truco Mineiro rules
        /// </summary>
        /// <param name="game">The current game state</param>
        /// <param name="roundNumber">The current round number (1, 2, or 3)</param>
        /// <returns>The team that wins due to draw resolution, or null if unresolved</returns>
        string? HandleDrawResolution(GameState game, int roundNumber);

        /// <summary>
        /// Determines if a hand is complete (2 out of 3 rounds won)
        /// </summary>
        /// <param name="game">The current game state</param>
        /// <returns>True if hand is complete, false otherwise</returns>
        bool IsHandComplete(GameState game);

        /// <summary>
        /// Gets the winning team of the current hand
        /// </summary>
        /// <param name="game">The current game state</param>
        /// <returns>The winning team name, or null if hand is not complete</returns>
        string? GetHandWinner(GameState game);
    }
}
