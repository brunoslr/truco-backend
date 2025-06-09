using TrucoMineiro.API.Domain.Models;

namespace TrucoMineiro.API.Domain.Events.GameEvents
{
    /// <summary>
    /// Event raised when a new hand is started.
    /// </summary>
    public class HandStartedEvent : GameEventBase
    {
        public int Hand { get; set; }
        public int DealerSeat { get; set; }
        public int FirstPlayerSeat { get; set; }
        public GameState GameState { get; set; } = null!;

        public HandStartedEvent(
            Guid gameId, 
            int hand, 
            int dealerSeat,
            int firstPlayerSeat,
            GameState gameState) 
            : base(gameId, null)
        {
            Hand = hand;
            DealerSeat = dealerSeat;
            FirstPlayerSeat = firstPlayerSeat;
            GameState = gameState;
        }

        public HandStartedEvent() : base()
        {
        }
    }
}
