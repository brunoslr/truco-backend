using TrucoMineiro.API.Constants;
using TrucoMineiro.API.Domain.Models;

namespace TrucoMineiro.API.Domain.StateMachine.Commands
{
    /// <summary>
    /// Command to surrender/give up the current hand
    /// </summary>
    public class SurrenderHandCommand : GameCommandBase
    {
        public override string CommandType => TrucoConstants.Commands.SurrenderHand;
        
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
    /// Command to call Truco or raise (Seis/Doze)
    /// </summary>
    public class CallTrucoOrRaiseCommand : GameCommandBase
    {
        public override string CommandType => TrucoConstants.Commands.CallTrucoOrRaise;

        public CallTrucoOrRaiseCommand() : base()
        {
        }

        public CallTrucoOrRaiseCommand(string gameId, int playerSeat) 
            : base(gameId, playerSeat)
        {
        }
    }

    /// <summary>
    /// Command to accept a Truco call or raise
    /// </summary>
    public class AcceptTrucoCommand : GameCommandBase
    {
        public override string CommandType => TrucoConstants.Commands.AcceptTruco;

        public AcceptTrucoCommand() : base()
        {
        }

        public AcceptTrucoCommand(string gameId, int playerSeat) 
            : base(gameId, playerSeat)
        {
        }
    }

    /// <summary>
    /// Command to surrender to a Truco call
    /// </summary>
    public class SurrenderTrucoCommand : GameCommandBase
    {
        public override string CommandType => TrucoConstants.Commands.SurrenderTruco;        public SurrenderTrucoCommand() : base()
        {
        }

        public SurrenderTrucoCommand(string gameId, int playerSeat) 
            : base(gameId, playerSeat)
        {
        }
    }
}