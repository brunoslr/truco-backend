using TrucoMineiro.API.Domain.Models;

namespace TrucoMineiro.API.Domain.Events.GameEvents
{
    /// <summary>
    /// Event published when Truco is called or raised (Seis/Doze)
    /// </summary>
    public class TrucoOrRaiseCalledEvent : GameEventBase
    {
        public Player CallingPlayer { get; set; } = null!;
        public int CallerTeam { get; set; }
        public string CallType { get; set; } = string.Empty; // "Truco", "Seis", "Doze"
        public int PreviousStakes { get; set; }
        public int NewPotentialStakes { get; set; }
        public GameState GameState { get; set; } = null!;

        public TrucoOrRaiseCalledEvent(Guid gameId, Player callingPlayer, int callerTeam, string callType, int previousStakes, int newPotentialStakes, GameState gameState)
            : base(gameId, GetPlayerGuid(callingPlayer))
        {
            CallingPlayer = callingPlayer;
            CallerTeam = callerTeam;
            CallType = callType;
            PreviousStakes = previousStakes;
            NewPotentialStakes = newPotentialStakes;
            GameState = gameState;
        }

        public TrucoOrRaiseCalledEvent() : base()
        {
        }

        private static Guid? GetPlayerGuid(Player? player)
        {
            return player?.Id;
        }
    }    
    
    /// <summary>
    /// Event published when Truco is accepted
    /// </summary>
    public class TrucoAcceptedEvent : GameEventBase
    {
        public Player AcceptingPlayer { get; set; } = null!;
        public int AcceptingTeam { get; set; }
        public int ConfirmedStakes { get; set; }
        public int? CanRaiseTeam { get; set; }
        public GameState GameState { get; set; } = null!;

        public TrucoAcceptedEvent(Guid gameId, Player acceptingPlayer, int acceptingTeam, int confirmedStakes, int? canRaiseTeam, GameState gameState)
            : base(gameId, GetPlayerGuid(acceptingPlayer))
        {
            AcceptingPlayer = acceptingPlayer;
            AcceptingTeam = acceptingTeam;
            ConfirmedStakes = confirmedStakes;
            CanRaiseTeam = canRaiseTeam;
            GameState = gameState;
        }

        public TrucoAcceptedEvent() : base()
        {
        }

        private static Guid? GetPlayerGuid(Player? player)
        {
            return player?.Id;
        }
    }    
    
    /// <summary>
    /// Event published when a team surrenders to a Truco call
    /// </summary>
    public class TrucoSurrenderedEvent : GameEventBase
    {
        public Player SurrenderingPlayer { get; set; } = null!;
        public int SurrenderingTeam { get; set; }
        public int PointsAwarded { get; set; }
        public GameState GameState { get; set; } = null!;

        public TrucoSurrenderedEvent(Guid gameId, Player surrenderingPlayer, int surrenderingTeam, int pointsAwarded, GameState gameState)
            : base(gameId, GetPlayerGuid(surrenderingPlayer))
        {
            SurrenderingPlayer = surrenderingPlayer;
            SurrenderingTeam = surrenderingTeam;
            PointsAwarded = pointsAwarded;
            GameState = gameState;
        }

        public TrucoSurrenderedEvent() : base()
        {
        }

        private static Guid? GetPlayerGuid(Player? player)
        {
            return player?.Id;
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
            return player?.Id;  // player.Id is already a Guid
        }
    }
}
