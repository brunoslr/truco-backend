using TrucoMineiro.API.Domain.Models;

namespace TrucoMineiro.API.Domain.Events.GameEvents
{
    /// <summary>
    /// Event raised when a game is started.
    /// </summary>
    public class GameStartedEvent : GameEventBase
    {
        public GameState GameState { get; set; } = null!;
        public List<Player> Players { get; set; } = new();
        public Player? StartedBy { get; set; }
        public Dictionary<string, object> GameConfiguration { get; set; } = new();

        public GameStartedEvent(
            Guid gameId, 
            GameState gameState, 
            List<Player>? players, 
            Player? startedBy = null,
            Dictionary<string, object>? gameConfiguration = null) 
            : base(gameId, GetPlayerGuid(startedBy))
        {
            GameState = gameState;
            Players = players ?? new List<Player>();
            StartedBy = startedBy;
            GameConfiguration = gameConfiguration ?? new Dictionary<string, object>();
        }

        public GameStartedEvent() : base()
        {
        }

        private static Guid? GetPlayerGuid(Player? player)
        {
            return player != null && Guid.TryParse(player.Id, out var playerId) ? playerId : null;
        }
    }
}
