using TrucoMineiro.API.Domain.Models;

namespace TrucoMineiro.API.Domain.Events.GameEvents
{
    /// <summary>
    /// Event raised when a player folds the entire hand (gives up all remaining cards).
    /// This is different from folding a single round via PlayCardRequest.IsFold.
    /// </summary>
    public class FoldHandEvent : GameEventBase
    {
        public Player Player { get; set; } = null!;
        public int HandNumber { get; set; }
        public int CurrentStakes { get; set; }
        public string WinningTeam { get; set; } = string.Empty;
        public GameState GameState { get; set; } = null!;        public FoldHandEvent(
            Guid gameId, 
            Guid playerId, 
            Player player,
            int handNumber,
            int currentStakes,
            string winningTeam,
            GameState gameState) 
            : base(gameId, playerId)
        {
            Player = player;
            HandNumber = handNumber;
            CurrentStakes = currentStakes;
            WinningTeam = winningTeam;
            GameState = gameState;
        }

        public FoldHandEvent() : base()
        {
        }
    }
}
