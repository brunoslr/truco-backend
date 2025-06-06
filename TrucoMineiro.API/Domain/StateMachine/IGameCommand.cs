using System;

namespace TrucoMineiro.API.Domain.StateMachine
{
    /// <summary>
    /// Interface for all game commands that can be processed by the state machine
    /// </summary>
    public interface IGameCommand
    {
        /// <summary>
        /// The unique identifier of the game this command targets
        /// </summary>
        string GameId { get; }
        
        /// <summary>
        /// The seat number of the player executing the command
        /// </summary>
        int PlayerSeat { get; }
        
        /// <summary>
        /// When the command was created
        /// </summary>
        DateTime Timestamp { get; }
        
        /// <summary>
        /// The type of command for logging and debugging
        /// </summary>
        string CommandType { get; }
        
        /// <summary>
        /// Unique identifier for this command instance
        /// </summary>
        string CommandId { get; }
    }
}
