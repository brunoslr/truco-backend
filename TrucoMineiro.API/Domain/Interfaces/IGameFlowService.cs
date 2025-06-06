using TrucoMineiro.API.Domain.Models;

namespace TrucoMineiro.API.Domain.Interfaces
{
    /// <summary>
    /// Service responsible for managing the game flow and turn sequence
    /// </summary>
    public interface IGameFlowService
    {
        /// <summary>
        /// Executes a player's card play and manages the subsequent game flow
        /// </summary>
        /// <param name="game">The current game state</param>
        /// <param name="playerSeat">The seat of the player playing the card</param>
        /// <param name="cardIndex">The index of the card in the player's hand</param>
        /// <returns>True if the card was played successfully</returns>
        bool PlayCard(GameState game, int playerSeat, int cardIndex);
        
        /// <summary>
        /// Executes AI player turns in sequence with appropriate delays
        /// </summary>
        /// <param name="game">The current game state</param>
        /// <param name="aiPlayDelayMs">Delay between AI plays in milliseconds</param>
        /// <returns>Task representing the async operation</returns>
        Task ProcessAITurnsAsync(GameState game, int aiPlayDelayMs);
        
        /// <summary>
        /// Checks if a hand is complete and processes end-of-hand logic
        /// </summary>
        /// <param name="game">The current game state</param>
        /// <param name="newHandDelayMs">Delay before starting new hand in milliseconds</param>
        /// <returns>Task representing the async operation</returns>
        Task ProcessHandCompletionAsync(GameState game, int newHandDelayMs);
        
        /// <summary>
        /// Advances the turn to the next player
        /// </summary>
        /// <param name="game">The current game state</param>
        void AdvanceToNextPlayer(GameState game);
        
        /// <summary>
        /// Checks if all players have played their cards for the current round
        /// </summary>
        /// <param name="game">The current game state</param>
        /// <returns>True if all players have played</returns>
        bool IsRoundComplete(GameState game);
        
        /// <summary>
        /// Starts a new hand by resetting the game state
        /// </summary>
        /// <param name="game">The current game state</param>
        void StartNewHand(GameState game);
    }
}
