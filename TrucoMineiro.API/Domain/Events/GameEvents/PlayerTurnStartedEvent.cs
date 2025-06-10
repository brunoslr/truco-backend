using TrucoMineiro.API.Domain.Models;

namespace TrucoMineiro.API.Domain.Events.GameEvents
{
    /// <summary>
    /// Event raised when a player's turn starts.
    /// </summary>
    public class PlayerTurnStartedEvent : GameEventBase
    {
        public Player Player { get; set; } = null!;
        public int Round { get; set; }
        public int Hand { get; set; }
        public bool IsAITurn { get; set; }
        public GameState GameState { get; set; } = null!;
        public List<string> AvailableActions { get; set; } = new();

        public PlayerTurnStartedEvent(
            Guid gameId, 
            Player player, 
            int round, 
            int hand, 
            GameState gameState,
            List<string>? availableActions = null) 
            : base(gameId, GetPlayerGuid(player))
        {
            Player = player;
            Round = round;
            Hand = hand;
            IsAITurn = player.IsAI;
            GameState = gameState;
            AvailableActions = availableActions ?? new List<string>();
        }

        public PlayerTurnStartedEvent() : base()
        {
        }        private static Guid? GetPlayerGuid(Player player)
        {
            return player?.Id;  // player.Id is already a Guid
        }
    }
}
