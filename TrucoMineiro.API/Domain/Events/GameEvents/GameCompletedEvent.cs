using TrucoMineiro.API.Domain.Models;

namespace TrucoMineiro.API.Domain.Events.GameEvents
{
    /// <summary>
    /// Event raised when a game is completed.
    /// </summary>
    public class GameCompletedEvent : GameEventBase
    {
        public Player? Winner { get; set; }
        public Dictionary<Guid, int> FinalScores { get; set; } = new();
        public GameState FinalGameState { get; set; } = null!;
        public TimeSpan GameDuration { get; set; }
        public Dictionary<string, object> GameStatistics { get; set; } = new();
        public string CompletionReason { get; set; } = "normal";

        public GameCompletedEvent(
            Guid gameId, 
            Player? winner, 
            Dictionary<Guid, int>? finalScores, 
            GameState finalGameState, 
            TimeSpan gameDuration,
            string completionReason = "normal",
            Dictionary<string, object>? gameStatistics = null) 
            : base(gameId, GetPlayerGuid(winner))
        {
            Winner = winner;
            FinalScores = finalScores ?? new Dictionary<Guid, int>();
            FinalGameState = finalGameState;
            GameDuration = gameDuration;
            CompletionReason = completionReason;
            GameStatistics = gameStatistics ?? new Dictionary<string, object>();
        }

        public GameCompletedEvent() : base()
        {
        }        private static Guid? GetPlayerGuid(Player? player)
        {
            return player?.Id;  // player.Id is already a Guid
        }
    }
}
