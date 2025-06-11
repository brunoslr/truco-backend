using TrucoMineiro.API.Domain.Events.GameEvents;
using TrucoMineiro.API.Domain.Models;

namespace TrucoMineiro.API.Domain.Interfaces
{
    /// <summary>
    /// Service responsible for game flow orchestration and event generation
    /// </summary>
    public interface IGameFlowService
    {        /// <summary>
        /// Processes game flow after a card is played and returns events to publish
        /// </summary>
        /// <param name="game">The current game state</param>
        /// <returns>List of events to publish</returns>
        List<object> ProcessCardPlayedFlow(GameState game);

        /// <summary>
        /// Processes game flow after a truco/raise is called and returns events to publish
        /// </summary>
        /// <param name="game">The current game state</param>
        /// <param name="callingPlayer">The player who called truco/raise</param>
        /// <param name="newStakes">The new stakes value</param>
        /// <returns>List of events to publish</returns>
        List<object> ProcessTrucoRaiseFlow(GameState game, Player callingPlayer, int newStakes);

        /// <summary>
        /// Processes game flow after a fold occurs and returns events to publish
        /// </summary>
        /// <param name="game">The current game state</param>
        /// <param name="foldingPlayer">The player who folded</param>
        /// <param name="winningTeam">The team that wins due to fold</param>
        /// <param name="currentStakes">The current stakes value</param>
        /// <returns>List of events to publish</returns>
        List<object> ProcessFoldFlow(GameState game, Player foldingPlayer, string winningTeam, int currentStakes);
    }
}