namespace TrucoMineiro.API.Models
{
    /// <summary>
    /// Represents a player in the Truco game
    /// </summary>
    public class Player
    {
        /// <summary>
        /// Unique identifier for the player
        /// </summary>
        public string Id { get; set; } = Guid.NewGuid().ToString();

        /// <summary>
        /// Player's name (e.g., "You", "AI 1", "Partner", "AI 2")
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Player's team (e.g., "Player's Team", "Opponent Team")
        /// </summary>
        public string Team { get; set; } = string.Empty;

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

        public Player(string name, string team, int seat)
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
