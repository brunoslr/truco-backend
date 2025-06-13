using TrucoMineiro.API.Constants;

namespace TrucoMineiro.API.Domain.StateMachine.Commands
{
    /// <summary>
    /// Command to start a new game
    /// </summary>
    public class StartGameCommand : GameCommandBase
    {
        public override string CommandType => TrucoConstants.Commands.StartGame;
        
        /// <summary>
        /// Optional custom name for the human player
        /// </summary>
        public string? PlayerName { get; set; }

        public StartGameCommand() : base()
        {
        }

        public StartGameCommand(string gameId, string? playerName = null) 
            : base(gameId, 0) // Player seat 0 is the human player
        {
            PlayerName = playerName;
        }
    }
}
