namespace TrucoMineiro.API.DTOs
{
    /// <summary>
    /// Request for the new play card endpoint
    /// </summary>
    public class PlayCardRequestDto
    {
        /// <summary>
        /// The unique identifier of the game
        /// </summary>
        /// <example>abc123</example>
        public string GameId { get; set; } = string.Empty;        /// <summary>
        /// The seat number of the player making the move (0-3)
        /// </summary>
        /// <example>0</example>
        public int PlayerSeat { get; set; }

        /// <summary>
        /// The index of the card in the player's hand to play (0-based)
        /// </summary>
        /// <example>0</example>
        public int CardIndex { get; set; }

        /// <summary>
        /// Whether this is a fold action instead of playing a card
        /// </summary>
        /// <example>false</example>
        public bool IsFold { get; set; } = false;
    }
}
