using TrucoMineiro.API.Domain.Models;

namespace TrucoMineiro.API.Domain.Events.GameEvents
{
    /// <summary>
    /// Event raised when a hand is completed (one team wins 2 out of 3 rounds).
    /// </summary>
    public class HandCompletedEvent : GameEventBase
    {
        public int Hand { get; set; }
        public string WinningTeam { get; set; } = string.Empty;
        public List<int> RoundWinners { get; set; } = new();
        public int PointsAwarded { get; set; }
        public GameState GameState { get; set; } = null!;

        public HandCompletedEvent(
            Guid gameId, 
            int hand, 
            string winningTeam,
            List<int> roundWinners,
            int pointsAwarded,
            GameState gameState) 
            : base(gameId, null)
        {
            Hand = hand;
            WinningTeam = winningTeam;
            RoundWinners = roundWinners;
            PointsAwarded = pointsAwarded;
            GameState = gameState;
        }

        public HandCompletedEvent() : base()
        {
        }
    }
}
