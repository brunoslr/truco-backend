using TrucoMineiro.API.Domain.Models;

namespace TrucoMineiro.API.Domain.Interfaces
{
    /// <summary>
    /// Interface for score calculation and game completion logic
    /// </summary>
    public interface IScoreCalculationService
    {
        /// <summary>
        /// Calculates points awarded for winning a hand
        /// </summary>
        /// <param name="game">The current game state</param>
        /// <returns>Points to award to the winning team</returns>
        int CalculateHandPoints(GameState game);

        /// <summary>
        /// Awards points to a team and updates the game state
        /// </summary>
        /// <param name="game">The current game state</param>
        /// <param name="team">The team to award points to</param>
        /// <param name="points">The number of points to award</param>
        void AwardPoints(GameState game, string team, int points);

        /// <summary>
        /// Checks if the game has ended (a team reached 12 points)
        /// </summary>
        /// <param name="game">The current game state</param>
        /// <returns>True if game is over, false otherwise</returns>
        bool IsGameOver(GameState game);

        /// <summary>
        /// Gets the winning team if the game is over
        /// </summary>
        /// <param name="game">The current game state</param>
        /// <returns>The winning team name, or null if game is not over</returns>
        string? GetGameWinner(GameState game);

        /// <summary>
        /// Gets the current score for a specific team
        /// </summary>
        /// <param name="game">The current game state</param>
        /// <param name="team">The team name</param>
        /// <returns>The team's current score</returns>
        int GetTeamScore(GameState game, string team);

        /// <summary>
        /// Resets scores for a new game
        /// </summary>
        /// <param name="game">The current game state</param>
        void ResetScores(GameState game);

        /// <summary>
        /// Validates if score update is valid
        /// </summary>
        /// <param name="game">The current game state</param>
        /// <param name="team">The team to update</param>
        /// <param name="points">The points to add</param>
        /// <returns>True if valid, false otherwise</returns>
        bool IsScoreUpdateValid(GameState game, string team, int points);

        /// <summary>
        /// Checks if the game is complete (a team reached 12 points)
        /// </summary>
        /// <param name="game">The current game state</param>
        /// <returns>True if game is complete, false otherwise</returns>
        bool IsGameComplete(GameState game);
    }
}
