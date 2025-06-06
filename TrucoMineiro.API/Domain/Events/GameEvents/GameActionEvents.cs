using TrucoMineiro.API.Domain.Models;

namespace TrucoMineiro.API.Domain.Events.GameEvents
{
    /// <summary>
    /// Event published when Truco is called
    /// </summary>
    public class TrucoCalledEvent : GameEventBase
    {
        public Player CallingPlayer { get; set; } = null!;
        public int TrucoLevel { get; set; }
        public GameState GameState { get; set; } = null!;

        public TrucoCalledEvent(Guid gameId, Player callingPlayer, int trucoLevel, GameState gameState)
            : base(gameId, GetPlayerGuid(callingPlayer))
        {
            CallingPlayer = callingPlayer;
            TrucoLevel = trucoLevel;
            GameState = gameState;
        }

        public TrucoCalledEvent() : base()
        {
        }

        private static Guid? GetPlayerGuid(Player? player)
        {
            return player != null && Guid.TryParse(player.Id, out var playerId) ? playerId : null;
        }
    }

    /// <summary>
    /// Event published when Truco is accepted
    /// </summary>
    public class TrucoAcceptedEvent : GameEventBase
    {
        public Player RespondingPlayer { get; set; } = null!;
        public int TrucoLevel { get; set; }
        public GameState GameState { get; set; } = null!;

        public TrucoAcceptedEvent(Guid gameId, Player respondingPlayer, int trucoLevel, GameState gameState)
            : base(gameId, GetPlayerGuid(respondingPlayer))
        {
            RespondingPlayer = respondingPlayer;
            TrucoLevel = trucoLevel;
            GameState = gameState;
        }

        public TrucoAcceptedEvent() : base()
        {
        }

        private static Guid? GetPlayerGuid(Player? player)
        {
            return player != null && Guid.TryParse(player.Id, out var playerId) ? playerId : null;
        }
    }

    /// <summary>
    /// Event published when Truco is rejected
    /// </summary>
    public class TrucoRejectedEvent : GameEventBase
    {
        public Player RespondingPlayer { get; set; } = null!;
        public Player CallingPlayer { get; set; } = null!;
        public int PointsAwarded { get; set; }
        public GameState GameState { get; set; } = null!;

        public TrucoRejectedEvent(Guid gameId, Player respondingPlayer, Player callingPlayer, int pointsAwarded, GameState gameState)
            : base(gameId, GetPlayerGuid(respondingPlayer))
        {
            RespondingPlayer = respondingPlayer;
            CallingPlayer = callingPlayer;
            PointsAwarded = pointsAwarded;
            GameState = gameState;
        }

        public TrucoRejectedEvent() : base()
        {
        }

        private static Guid? GetPlayerGuid(Player? player)
        {
            return player != null && Guid.TryParse(player.Id, out var playerId) ? playerId : null;
        }
    }

    /// <summary>
    /// Event published when a player folds
    /// </summary>
    public class PlayerFoldedEvent : GameEventBase
    {
        public Player FoldingPlayer { get; set; } = null!;
        public string WinningTeam { get; set; } = string.Empty;
        public GameState GameState { get; set; } = null!;

        public PlayerFoldedEvent(Guid gameId, Player foldingPlayer, string winningTeam, GameState gameState)
            : base(gameId, GetPlayerGuid(foldingPlayer))
        {
            FoldingPlayer = foldingPlayer;
            WinningTeam = winningTeam;
            GameState = gameState;
        }

        public PlayerFoldedEvent() : base()
        {
        }

        private static Guid? GetPlayerGuid(Player? player)
        {
            return player != null && Guid.TryParse(player.Id, out var playerId) ? playerId : null;
        }
    }
}
