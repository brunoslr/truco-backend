namespace TrucoMineiro.API.DTOs
{
    /// <summary>
    /// Represents a player in the Truco game
    /// </summary>
    public class PlayerDto
    {
        /// <summary>
        /// Unique identifier for the player
        /// </summary>
        public string PlayerId { get; set; } = string.Empty;

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
        public List<CardDto> Hand { get; set; } = new List<CardDto>();

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
        /// Seat number of the first player for the current hand
        /// </summary>
        public int FirstPlayerSeat { get; set; }
    }
}
