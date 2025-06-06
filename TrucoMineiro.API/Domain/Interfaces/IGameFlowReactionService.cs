using TrucoMineiro.API.Domain.Models;

namespace TrucoMineiro.API.Domain.Interfaces
{
    /// <summary>
    /// Service responsible for reacting to card play actions and managing subsequent game flow
    /// </summary>
    public interface IGameFlowReactionService
    {
        /// <summary>
        /// Processes all reactions after a card is played
        /// </summary>
        /// <param name="game">The current game state</param>
        /// <param name="autoAiPlay">Whether AI should automatically play</param>
        /// <param name="aiPlayDelayMs">Delay for AI plays in milliseconds</param>
        /// <param name="newHandDelayMs">Delay for new hand start in milliseconds</param>
        /// <returns>Task representing the async operation</returns>
        Task ProcessCardPlayReactionsAsync(GameState game, bool autoAiPlay, int aiPlayDelayMs, int newHandDelayMs);
        
        /// <summary>
        /// Checks if round is complete and determines winner
        /// </summary>
        /// <param name="game">The current game state</param>
        void ProcessRoundCompletion(GameState game);
        
        /// <summary>
        /// Processes AI turns if enabled
        /// </summary>
        /// <param name="game">The current game state</param>
        /// <param name="aiPlayDelayMs">Delay for AI plays in milliseconds</param>
        /// <returns>Task representing the async operation</returns>
        Task ProcessAITurnsAsync(GameState game, int aiPlayDelayMs);
        
        /// <summary>
        /// Checks if hand is complete and starts new hand if needed
        /// </summary>
        /// <param name="game">The current game state</param>
        /// <param name="newHandDelayMs">Delay before starting new hand in milliseconds</param>
        /// <returns>Task representing the async operation</returns>
        Task ProcessHandCompletionAsync(GameState game, int newHandDelayMs);
    }
}
