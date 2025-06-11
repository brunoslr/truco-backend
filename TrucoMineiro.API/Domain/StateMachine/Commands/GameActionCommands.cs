using TrucoMineiro.API.Domain.Models;

namespace TrucoMineiro.API.Domain.StateMachine.Commands
{    /// <summary>
    /// Command to surrender/give up the current hand
    /// </summary>
    public class SurrenderHandCommand : GameCommandBase
    {
        public override string CommandType => "SurrenderHand";
        
        /// <summary>
        /// Optional reason for surrendering
        /// </summary>
        public string? Reason { get; set; }

        public SurrenderHandCommand() : base()
        {
        }

        public SurrenderHandCommand(string gameId, int playerSeat, string? reason = null) 
            : base(gameId, playerSeat)
        {
            Reason = reason;
        }
    }

    /// <summary>
    /// Command to call Truco (raise the stakes)
    /// </summary>
    public class CallTrucoCommand : GameCommandBase
    {
        public override string CommandType => "CallTruco";

        public CallTrucoCommand() : base()
        {
        }

        public CallTrucoCommand(string gameId, int playerSeat) 
            : base(gameId, playerSeat)
        {
        }
    }

    /// <summary>
    /// Command to respond to a Truco call
    /// </summary>
    public class RespondToTrucoCommand : GameCommandBase
    {
        public override string CommandType => "RespondToTruco";
        
        /// <summary>
        /// The response to the Truco call
        /// </summary>
        public TrucoResponse Response { get; set; }

        /// <summary>
        /// Whether to accept the Truco call (true = accept, false = reject)
        /// </summary>
        public bool Accept { get; set; }

        public RespondToTrucoCommand() : base()
        {
        }

        public RespondToTrucoCommand(string gameId, int playerSeat, TrucoResponse response) 
            : base(gameId, playerSeat)
        {
            Response = response;
        }
    }
}
