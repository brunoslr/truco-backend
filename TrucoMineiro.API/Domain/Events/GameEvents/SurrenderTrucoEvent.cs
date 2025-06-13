using TrucoMineiro.API.Domain.Models;

namespace TrucoMineiro.API.Domain.Events.GameEvents
{
    /// <summary>
    /// Event raised when a player surrenders the entire hand (gives up all remaining cards).
    /// This is different from folding a single round via PlayCardRequest.IsFold.
    /// </summary>
    public class SurrenderTrucoEvent : GameEventBase
    {       
         public Player Player { get; set; } = null!;
        public int HandNumber { get; set; }
        public int CurrentStake { get; set; }
        public Team WinningTeam { get; set; }
        public GameState GameState { get; set; } = null!;
        public SurrenderTrucoEvent(
            Guid gameId,
            Guid playerId,
            Player player,
            int handNumber,
            int currentStake,
            Team winningTeam,
            GameState gameState)
            : base(gameId, playerId)
        {
            Player = player;
            HandNumber = handNumber;
            CurrentStake = currentStake;
            WinningTeam = winningTeam;
            GameState = gameState;
        }

        public SurrenderTrucoEvent() : base()
        {
        }
    }
}
