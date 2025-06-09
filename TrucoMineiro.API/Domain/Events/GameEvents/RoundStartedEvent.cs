using TrucoMineiro.API.Domain.Models;

namespace TrucoMineiro.API.Domain.Events.GameEvents
{
    /// <summary>
    /// Event raised when a new round is started within a hand.
    /// </summary>
    public class RoundStartedEvent : GameEventBase
    {
        public int Round { get; set; }
        public int Hand { get; set; }
        public int FirstPlayerSeat { get; set; }
        public GameState GameState { get; set; } = null!;

        public RoundStartedEvent(
            Guid gameId, 
            int round, 
            int hand,
            int firstPlayerSeat,
            GameState gameState) 
            : base(gameId, null)
        {
            Round = round;
            Hand = hand;
            FirstPlayerSeat = firstPlayerSeat;
            GameState = gameState;
        }

        public RoundStartedEvent() : base()
        {
        }
    }
}
