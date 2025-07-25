namespace TrucoMineiro.API.Domain.Models
{    /// <summary>
    /// Represents a player in the Truco game
    /// </summary>
    public class Player
    {
        /// <summary>
        /// Unique identifier for the player
        /// </summary>
        public Guid Id { get; set; } = Guid.NewGuid();

        /// <summary>
        /// Player's name (e.g., "You", "AI 1", "Partner", "AI 2")
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Player's team (e.g., "Player's Team", "Opponent Team")
        /// </summary>
        public Team Team { get; set; }

        /// <summary>
        /// Player's current hand of cards
        /// </summary>
        public List<Card> Hand { get; set; } = new List<Card>();

        /// <summary>
        /// Whether this player is the dealer for the current hand
        /// </summary>
        public bool IsDealer { get; set; }

        /// <summary>
        /// Whether it's currently this player's turn
        /// </summary>
        public bool IsActive { get; set; }

        /// <summary>
        /// Player's position at the table (0-3)
        /// </summary>
        public int Seat { get; set; }

        /// <summary>
        /// Whether this player is controlled by AI
        /// </summary>
        public bool IsAI { get; set; }

        /// <summary>
        /// Whether this player has folded in the current hand
        /// </summary>
        public bool HasFolded { get; set; } = false;

        public Player(string name, Team team, int seat)
        {
            Name = name;
            Team = team;
            Seat = seat;
        }

        public Player() { }

        /// <summary>
        /// Add a card to the player's hand
        /// </summary>
        public void AddCard(Card card)
        {
            Hand.Add(card);
        }

        /// <summary>
        /// Remove a card from the player's hand
        /// </summary>
        public Card PlayCard(int cardIndex)
        {
            if (cardIndex < 0 || cardIndex >= Hand.Count)
            {
                throw new ArgumentOutOfRangeException(nameof(cardIndex), "Invalid card index");
            }

            var card = Hand[cardIndex];
            Hand.RemoveAt(cardIndex);
            return card;
        }

        /// <summary>
        /// Clear the player's hand
        /// </summary>
        public void ClearHand()
        {
            Hand.Clear();
        }
    }
}
