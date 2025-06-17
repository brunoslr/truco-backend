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
        bool CanSurrenderTruco(GameState game, int playerSeat);        /// <summary>
        /// Checks if this is the last hand of the game (any team is one victory away)
        /// </summary>
        /// <param name="game">The current game state</param>
        /// <returns>True if this is the last hand, false otherwise</returns>
        bool IsLastHand(GameState game);

        /// <summary>
        /// Checks if exactly one team is at the last hand
        /// </summary>
        /// <param name="game">The current game state</param>
        /// <returns>True if exactly one team is one victory away</returns>
        bool IsOneTeamAtLastHand(GameState game);

        /// <summary>
        /// Checks if both teams are at the last hand
        /// </summary>
        /// <param name="game">The current game state</param>
        /// <returns>True if both teams are one victory away</returns>
        bool AreBothTeamsAtLastHand(GameState game);

        /// <summary>
        /// Gets the team that is at the last hand
        /// </summary>
        /// <param name="game">The current game state</param>
        /// <returns>The team that is one victory away, or null if none or both teams are</returns>
        Team? GetTeamAtLastHand(GameState game);

        /// <summary>
        /// Checks if the stakes are at maximum value
        /// </summary>
        /// <param name="game">The current game state</param>
        /// <returns>True if stakes are at maximum, false otherwise</returns>
        bool IsMaximumStakes(GameState game);

        /// <summary>
        /// Gets the next stakes value in the progression
        /// </summary>
        /// <param name="currentStakes">Current stakes value</param>
        /// <returns>Next stakes value, or -1 if at maximum</returns>
        int GetNextStakes(int currentStakes);        /// <summary>
        /// Applies last hand rule effects to the game
        /// </summary>
        /// <param name="game">The current game state</param>
        void ApplyLastHandRule(GameState game);

        /// <summary>
        /// Calculates the points at stake for the current hand
        /// </summary>
        /// <param name="game">The current game state</param>
        /// <returns>Points at stake</returns>
        int CalculateStakes(GameState game);

        /// <summary>
        /// Calculates points awarded for winning a hand
        /// </summary>        /// <param name="game">The current game state</param>
        /// <returns>Points to award to the winning team</returns>
        int CalculateHandPoints(GameState game);
    }
}
