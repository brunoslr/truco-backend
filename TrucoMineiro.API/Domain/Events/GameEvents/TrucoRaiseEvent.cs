using TrucoMineiro.API.Domain.Models;

namespace TrucoMineiro.API.Domain.Events.GameEvents
{
    /// <summary>
    /// Event raised when a player calls Truco or raises the stakes.
    /// Since you can only raise after opponent's truco/raise, these are unified.
    /// </summary>
    public class TrucoRaiseEvent : GameEventBase
    {
        public Player Player { get; set; } = null!;
        public int CurrentStakes { get; set; }
        public int NewStakes { get; set; }
        public bool IsInitialTruco { get; set; }
        public GameState GameState { get; set; } = null!;

        public TrucoRaiseEvent(
            Guid gameId, 
            Guid playerId, 
            Player player,
            int currentStakes,
            int newStakes,
            bool isInitialTruco,
            GameState gameState) 
            : base(gameId, playerId)
        {
            Player = player;
            CurrentStakes = currentStakes;
            NewStakes = newStakes;
            IsInitialTruco = isInitialTruco;
            GameState = gameState;
        }

        public TrucoRaiseEvent() : base()
        {
        }
    }
}
