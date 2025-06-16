using TrucoMineiro.API.Domain.Models;

namespace TrucoMineiro.API.Domain.Interfaces
{    /// <summary>
    /// Interface for handling Truco calls, stakes, and special rules validation
    /// </summary>
    public interface ITrucoRulesEngine
    {        
        /// <summary>
        /// Checks if a Truco call is allowed in the current game state
        /// </summary>
        /// <param name="game">The current game state</param>
        /// <param name="playerSeat">The seat of the player wanting to call Truco</param>
        /// <returns>True if allowed, false otherwise</returns>
        bool CanCallTruco(GameState game, int playerSeat);

        /// <summary>
        /// Checks if raising stakes is allowed
        /// </summary>
        /// <param name="game">The current game state</param>
        /// <param name="playerSeat">The seat of the player wanting to raise</param>
        /// <returns>True if allowed, false otherwise</returns>
        bool CanRaise(GameState game, int playerSeat);

        /// <summary>
        /// Checks if accepting a truco call is allowed
        /// </summary>
        /// <param name="game">The current game state</param>
        /// <param name="playerSeat">The seat of the player wanting to accept</param>
        /// <returns>True if allowed, false otherwise</returns>
        bool CanAcceptTruco(GameState game, int playerSeat);

        /// <summary>
        /// Checks if surrendering to a truco call is allowed
        /// </summary>
        /// <param name="game">The current game state</param>
        /// <param name="playerSeat">The seat of the player wanting to surrender</param>
        /// <returns>True if allowed, false otherwise</returns>
        bool CanSurrenderTruco(GameState game, int playerSeat);

        /// <summary>
        /// Checks if exactly one team has 10 points
        /// </summary>
        /// <param name="game">The current game state</param>
        /// <returns>True if exactly one team has 10 points</returns>
        bool IsOneTeamAt10(GameState game);

        /// <summary>
        /// Checks if both teams have 10 points
        /// </summary>
        /// <param name="game">The current game state</param>
        /// <returns>True if both teams have 10 points</returns>
        bool AreBothTeamsAt10(GameState game);

        /// <summary>
        /// Gets the team that has 10 points
        /// </summary>
        /// <param name="game">The current game state</param>
        /// <returns>The team with 10 points, or null if none or both teams have 10</returns>
        Team? GetTeamAt10(GameState game);

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
