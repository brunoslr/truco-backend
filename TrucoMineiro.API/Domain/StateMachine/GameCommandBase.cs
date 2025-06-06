using System;

namespace TrucoMineiro.API.Domain.StateMachine
{
    /// <summary>
    /// Base class for all game commands, providing common properties and functionality
    /// </summary>
    public abstract class GameCommandBase : IGameCommand
    {
        /// <summary>
        /// The unique identifier of the game this command targets
        /// </summary>
        public string GameId { get; set; } = string.Empty;
        
        /// <summary>
        /// The seat number of the player executing the command
        /// </summary>
        public int PlayerSeat { get; set; }
        
        /// <summary>
        /// When the command was created
        /// </summary>
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
        
        /// <summary>
        /// The type of command for logging and debugging
        /// </summary>
        public abstract string CommandType { get; }
        
        /// <summary>
        /// Unique identifier for this command instance
        /// </summary>
        public string CommandId { get; set; } = Guid.NewGuid().ToString();

        protected GameCommandBase()
        {
        }

        protected GameCommandBase(string gameId, int playerSeat)
        {
            GameId = gameId;
            PlayerSeat = playerSeat;
        }
    }
}
