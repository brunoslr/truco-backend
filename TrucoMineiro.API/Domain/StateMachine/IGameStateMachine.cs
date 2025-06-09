using System.Threading.Tasks;
using TrucoMineiro.API.Domain.Models;

namespace TrucoMineiro.API.Domain.StateMachine
{    /// <summary>
    /// Result of processing a game command
    /// </summary>
    public class CommandResult
    {
        /// <summary>
        /// Whether the command was successfully processed
        /// </summary>
        public bool IsSuccess { get; set; }
        
        /// <summary>
        /// Error message if the command failed
        /// </summary>
        public string? ErrorMessage { get; set; }
        
        /// <summary>
        /// Updated game state after command processing
        /// </summary>
        public GameState? GameState { get; set; }

        public static CommandResult Success(string? message = null) =>
            new() { IsSuccess = true, ErrorMessage = message };

        public static CommandResult Failure(string errorMessage) =>
            new() { IsSuccess = false, ErrorMessage = errorMessage };
            
        public static CommandResult Success(GameState gameState) =>
            new() { IsSuccess = true, GameState = gameState };
    }    /// <summary>
    /// Interface for the game state machine that processes commands and manages game flow
    /// </summary>
    public interface IGameStateMachine
    {
        /// <summary>
        /// Process a game command and return the result
        /// </summary>
        /// <param name="command">The command to process</param>
        /// <returns>The result of processing the command</returns>
        Task<CommandResult> ProcessCommandAsync(IGameCommand command);
        
        /// <summary>
        /// Get the current state of a game
        /// </summary>
        /// <param name="gameId">The unique identifier of the game</param>
        /// <returns>The current game state, or null if not found</returns>
        Task<GameState?> GetGameStateAsync(Guid gameId);
        
        /// <summary>
        /// Check if a command is valid for the current game state
        /// </summary>
        /// <param name="command">The command to validate</param>
        /// <returns>CommandResult indicating if the command is valid</returns>
        Task<CommandResult> ValidateCommandAsync(IGameCommand command);
    }
}
