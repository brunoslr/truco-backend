using TrucoMineiro.API.Constants;

namespace TrucoMineiro.API.Domain.Models
{
    /// <summary>
    /// Represents the state of a single round within a hand
    /// </summary>
    public class RoundState
    {
        /// <summary>
        /// The round number (1, 2, or 3)
        /// </summary>
        public int RoundNumber { get; set; }
        
        /// <summary>
        /// Cards played by each player in this round
        /// Key: PlayerSeat, Value: Card (or null if fold/not played)
        /// </summary>
        public Dictionary<int, Card?> PlayedCards { get; set; } = new();
        
        /// <summary>
        /// Number of players who have played their cards in this round
        /// </summary>
        public int PlayersPlayed => PlayedCards.Count(kvp => kvp.Value != null);
        
        /// <summary>
        /// Total number of active players in the game
        /// </summary>
        public int TotalPlayers { get; set; } = TrucoConstants.Game.MaxPlayers;
        
        /// <summary>
        /// Whether this round is complete (all players have played)
        /// </summary>
        public bool IsComplete => PlayersPlayed == TotalPlayers;
        
        /// <summary>
        /// The player seat who won this round (null if not determined or draw)
        /// </summary>
        public int? WinnerSeat { get; set; }
        
        /// <summary>
        /// Whether this round ended in a draw
        /// </summary>
        public bool IsDraw { get; set; }
        
        /// <summary>
        /// Timestamp when the round started
        /// </summary>
        public DateTime StartedAt { get; set; } = DateTime.UtcNow;
        
        /// <summary>
        /// Timestamp when the round was completed
        /// </summary>
        public DateTime? CompletedAt { get; set; }

        public RoundState(int roundNumber, int totalPlayers = TrucoConstants.Game.MaxPlayers)
        {
            RoundNumber = roundNumber;
            TotalPlayers = totalPlayers;
            
            // Initialize empty slots for all players
            for (int seat = 0; seat < totalPlayers; seat++)
            {
                PlayedCards[seat] = null;
            }
        }

        /// <summary>
        /// Play a card for a specific player seat
        /// </summary>
        /// <param name="playerSeat">The seat of the player playing the card</param>
        /// <param name="card">The card being played (null for fold)</param>
        public void PlayCard(int playerSeat, Card? card)
        {
            PlayedCards[playerSeat] = card;
            
            if (IsComplete && CompletedAt == null)
            {
                CompletedAt = DateTime.UtcNow;
            }
        }        /// <summary>
        /// Get all non-null cards played in this round
        /// </summary>
        public List<PlayedCard> GetPlayedCards()
        {
            return PlayedCards
                .Where(kvp => kvp.Value != null && !kvp.Value.IsFold)
                .Select(kvp => new PlayedCard(kvp.Key, kvp.Value!))
                .ToList();
        }

        /// <summary>
        /// Check if a specific player has played in this round
        /// </summary>
        public bool HasPlayerPlayed(int playerSeat)
        {
            return PlayedCards.ContainsKey(playerSeat) && PlayedCards[playerSeat] != null;
        }
    }
}
