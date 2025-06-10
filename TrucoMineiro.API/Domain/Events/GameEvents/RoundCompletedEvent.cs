using TrucoMineiro.API.Domain.Models;

namespace TrucoMineiro.API.Domain.Events.GameEvents
{
    /// <summary>
    /// Event raised when a round is completed.
    /// </summary>
    public class RoundCompletedEvent : GameEventBase
    {
        public int Round { get; set; }
        public int Hand { get; set; }
        public Player? RoundWinner { get; set; }
        public List<Card> PlayedCards { get; set; } = new();
        public Dictionary<Guid, int> ScoreChanges { get; set; } = new();
        public GameState GameState { get; set; } = null!;
        public bool IsDraw { get; set; }

        public RoundCompletedEvent(
            Guid gameId, 
            int round, 
            int hand, 
            Player? roundWinner, 
            List<Card>? playedCards, 
            Dictionary<Guid, int>? scoreChanges, 
            GameState gameState,
            bool isDraw = false) 
            : base(gameId, GetPlayerGuid(roundWinner))
        {
            Round = round;
            Hand = hand;
            RoundWinner = roundWinner;
            PlayedCards = playedCards ?? new List<Card>();
            ScoreChanges = scoreChanges ?? new Dictionary<Guid, int>();
            GameState = gameState;
            IsDraw = isDraw;
        }

        public RoundCompletedEvent() : base()
        {
        }        private static Guid? GetPlayerGuid(Player? player)
        {
            return player?.Id;  // player.Id is already a Guid
        }
    }
}
