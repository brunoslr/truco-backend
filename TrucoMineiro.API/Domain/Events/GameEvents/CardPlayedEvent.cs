using TrucoMineiro.API.Domain.Models;

namespace TrucoMineiro.API.Domain.Events.GameEvents
{
    /// <summary>
    /// Event raised when a player plays a card in the game.
    /// </summary>
    public class CardPlayedEvent : GameEventBase
    {
        public Card Card { get; set; } = null!;
        public Player Player { get; set; } = null!;
        public int Round { get; set; }
        public int Hand { get; set; }
        public bool IsAIMove { get; set; }
        public GameState GameState { get; set; } = null!;

        public CardPlayedEvent(
            Guid gameId, 
            Guid playerId, 
            Card card, 
            Player player, 
            int round, 
            int hand, 
            bool isAIMove, 
            GameState gameState) 
            : base(gameId, playerId)
        {
            Card = card;
            Player = player;
            Round = round;
            Hand = hand;
            IsAIMove = isAIMove;
            GameState = gameState;
        }

        public CardPlayedEvent() : base()
        {
        }
    }
}
