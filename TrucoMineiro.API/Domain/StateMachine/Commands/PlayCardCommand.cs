using TrucoMineiro.API.Constants;
using TrucoMineiro.API.Domain.Models;

namespace TrucoMineiro.API.Domain.StateMachine.Commands
{
    /// <summary>
    /// Command to play a card during a player's turn
    /// </summary>
    public class PlayCardCommand : GameCommandBase
    {
        public override string CommandType => TrucoConstants.Commands.PlayCard;
        
        /// <summary>
        /// The index of the card in the player's hand to play (0-based)
        /// </summary>
        public int CardIndex { get; set; }
        
        /// <summary>
        /// The actual card being played (for validation)
        /// </summary>
        public Card? Card { get; set; }

        public PlayCardCommand() : base()
        {
        }

        public PlayCardCommand(string gameId, int playerSeat, int cardIndex) 
            : base(gameId, playerSeat)
        {
            CardIndex = cardIndex;
        }

        public PlayCardCommand(string gameId, int playerSeat, int cardIndex, Card card) 
            : base(gameId, playerSeat)
        {
            CardIndex = cardIndex;
            Card = card;
        }
    }
}
